#!/usr/bin/env bash
#
# Runs the pull request pipeline locally, in the same image the self-hosted runner builds in, on
# Linux and without pushing anything.
#
# Usage:
#   docker/ci-local.sh                          The whole pipeline, everything treated as affected
#   docker/ci-local.sh --base master            The whole pipeline for a pull request into master
#   docker/ci-local.sh --changed src/libraries/Bitenovac.DecompressionAlgorithms.Core/Foo.cs
#                                               What CI would do if those files had changed
#   docker/ci-local.sh ci plan                  One build/ci.sh command and nothing else
#   docker/ci-local.sh ci graph
#   docker/ci-local.sh shell                    A prompt inside the runner, on a copy of the tree
#
# Options:
#   -b, --base REF        Diff against this commit, as BASE_SHA does in CI. Resolved on the host,
#                         so branch names and HEAD~1 work; the container sees a commit id.
#   -c, --changed LIST    A comma or newline separated list of paths to treat as changed, for
#                         answering "what would CI run if I touched this?" without a commit.
#   -s, --shards N        Pin the shard count instead of deriving it from the project count.
#       --shard N         Run only this shard.
#       --rebuild         Rebuild the runner image even if it is already present.
#       --no-cache        Do not reuse the NuGet package cache between runs.
#
# The image is built from docker/ci-runner.Dockerfile with the SDK version read from global.json,
# and is rebuilt when that version or a docker/ file changes.
#
# The run's output lands on the host under artifacts/local-ci: the coverage report, the plan, and
# the markdown that would have been the job summary. The repository is mounted read-only.

set -euo pipefail

readonly REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
readonly DOCKER_DIR="${REPO_ROOT}/docker"
readonly OUT_DIR="${REPO_ROOT}/artifacts/local-ci"
readonly NUGET_VOLUME="bitenovac-ci-nuget"

cd "${REPO_ROOT}"

log()  { printf '\n\033[1m==> %s\033[0m\n' "$*"; }
fail() { printf '\n\033[31merror:\033[0m %s\n' "$*" >&2; exit 1; }

# ------------------------------------------------------------------------------- arguments ----

base_ref=""
changed=""
shards=""
shard=""
rebuild=false
use_cache=true
command_args=()

while [[ $# -gt 0 ]]; do
    case "$1" in
        -b|--base)    base_ref="${2:-}"; shift 2 ;;
        -c|--changed) changed="${2:-}";  shift 2 ;;
        -s|--shards)  shards="${2:-}";   shift 2 ;;
        --shard)      shard="${2:-}";    shift 2 ;;
        --rebuild)    rebuild=true;      shift ;;
        --no-cache)   use_cache=false;   shift ;;
        -h|--help)    sed -n '2,29p' "${BASH_SOURCE[0]}" | sed 's/^# \?//'; exit 0 ;;
        --)           shift; command_args+=("$@"); break ;;
        # Option parsing stops at the command; everything after it belongs to the command.
        # Otherwise 'ci-local.sh exec bash -c ...' loses the -c to --changed.
        *)            command_args+=("$@"); break ;;
    esac
done

command -v docker > /dev/null 2>&1 || fail "docker is not on PATH. Run ./init.sh to see what is missing."
docker info > /dev/null 2>&1 || fail "The Docker daemon is not reachable. Start Docker and try again."

[[ "$(docker info --format '{{.OSType}}' 2>/dev/null)" == "linux" ]] \
    || fail "The runner image is a Linux image, but Docker is in Windows container mode. Switch it to Linux containers."

# --------------------------------------------------------------------------------- the image ----

sdk_version="$(sed -n 's/.*"version"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' global.json | head -n 1)"
[[ -n "${sdk_version}" ]] || fail "Could not read the SDK version from global.json."

readonly IMAGE="bitenovac-ci-runner:${sdk_version}"

if [[ "${rebuild}" == true ]] || ! docker image inspect "${IMAGE}" > /dev/null 2>&1; then
    log "Building ${IMAGE} (.NET SDK ${sdk_version})"
    docker build \
        --file "${DOCKER_DIR}/ci-runner.Dockerfile" \
        --build-arg "DOTNET_SDK_VERSION=${sdk_version}" \
        --tag "${IMAGE}" \
        "${DOCKER_DIR}"
else
    # Mostly a cache hit. Re-running after editing the entrypoint must not run the old one.
    log "Refreshing ${IMAGE}"
    docker build --quiet \
        --file "${DOCKER_DIR}/ci-runner.Dockerfile" \
        --build-arg "DOTNET_SDK_VERSION=${sdk_version}" \
        --tag "${IMAGE}" \
        "${DOCKER_DIR}" > /dev/null
fi

# ----------------------------------------------------------------------------------- inputs ----

base_sha=""
if [[ -n "${base_ref}" ]]; then
    # Resolved on the host so 'master', 'HEAD~1' and remote branches work; the container's copy
    # of .git has no remotes configured.
    base_sha="$(git rev-parse --verify "${base_ref}^{commit}" 2>/dev/null)" \
        || fail "Could not resolve '${base_ref}' to a commit."
    log "Diffing against ${base_ref} (${base_sha:0:12})"
elif [[ -n "${changed}" ]]; then
    log "Treating these as the changed files:"
    printf '  %s\n' ${changed//,/ }
else
    log "No --base and no --changed: every project is treated as affected, which is what CI does when it cannot work out a merge base."
fi

mkdir -p "${OUT_DIR}"

# ------------------------------------------------------------------------------------ mounts ----

# Under Git Bash a POSIX path reaches the Docker CLI as a Windows path only after cygpath, and
# MSYS_NO_PATHCONV stops MSYS mangling the container-side half of the -v argument.
host_repo="${REPO_ROOT}"
host_out="${OUT_DIR}"
if command -v cygpath > /dev/null 2>&1; then
    host_repo="$(cygpath -w "${REPO_ROOT}")"
    host_out="$(cygpath -w "${OUT_DIR}")"
    export MSYS_NO_PATHCONV=1
fi

run_args=(
    --rm
    --init
    --volume "${host_repo}:/host:ro"
    --volume "${host_out}:/out"
    --workdir /repo
)

# A named volume rather than a bind mount of ~/.nuget, so a Linux container does not write into
# the host's package folder.
if [[ "${use_cache}" == true ]]; then
    run_args+=(--volume "${NUGET_VOLUME}:/root/.nuget/packages")
fi

[[ -n "${base_sha}" ]] && run_args+=(--env "BASE_SHA=${base_sha}")
[[ -n "${changed}"  ]] && run_args+=(--env "CHANGED_FILES=$(printf '%s' "${changed}" | tr ',' '\n')")
[[ -n "${shards}"   ]] && run_args+=(--env "CI_SHARDS=${shards}")
[[ -n "${shard}"    ]] && run_args+=(--env "CI_SHARD=${shard}")

# Interactive only when attached to a terminal, so this stays usable from a script or a hook.
if [[ -t 0 && -t 1 ]]; then
    run_args+=(--interactive --tty)
fi

log "Running the pipeline in ${IMAGE}"

status=0
docker run "${run_args[@]}" "${IMAGE}" "${command_args[@]}" || status=$?

if [[ "${status}" -ne 0 ]]; then
    printf '\n\033[31mThe local pipeline failed (exit %s).\033[0m Its output is under artifacts/local-ci.\n' "${status}" >&2
fi

exit "${status}"
