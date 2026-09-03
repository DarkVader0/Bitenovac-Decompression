#!/usr/bin/env bash
#
# Runs the pull request pipeline locally, in the same image the self-hosted runner builds in, on
# Linux and without pushing anything.
#
# Usage:
#   docker/ci-local.sh                          Plan, then build and test both configurations
#   docker/ci-local.sh --cacheless              Ignore the local main cache; rebuild everything
#   docker/ci-local.sh ci plan                  One Bitenovac.CloudBuild command and nothing else
#   docker/ci-local.sh ci graph
#   docker/ci-local.sh shell                    A prompt inside the runner, on a copy of the tree
#
# There is no --base or --changed here any more: the old affected-set model answered "what would
# CI do if X changed?" by diffing against a commit. The cache answers the same question directly
# — change the file on disk and run this; the tool hashes what it finds. What each project's
# fullHash is doing is visible with 'docker/ci-local.sh ci graph'.
#
# Options:
#       --cacheless        Ignore the local main cache entirely; every project is a miss. Never
#                          promotes — this run's cache stays local to it.
#       --keep-artifacts   Do not delete this run's own artifact store on exit.
#       --rebuild          Rebuild the runner image even if it is already present.
#       --no-cache         Do not reuse the NuGet package cache between runs.
#
# The image is built from docker/ci-runner.Dockerfile with the SDK version read from global.json,
# and is rebuilt when that version or a docker/ file changes.
#
# main here is a Docker volume local to this machine, not the shared one CI promotes into. It
# persists between local runs so repeated local iteration stays warm, and 'docker volume rm
# bitenovac-local-main' resets it. The run's output lands on the host under artifacts/local-ci:
# the coverage report and the merged plan. The repository is mounted read-only.

set -euo pipefail

readonly REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
readonly DOCKER_DIR="${REPO_ROOT}/docker"
readonly OUT_DIR="${REPO_ROOT}/artifacts/local-ci"
readonly NUGET_VOLUME="bitenovac-cloudbuild-nuget"
readonly MAIN_VOLUME="bitenovac-local-main"
readonly PR_VOLUME="bitenovac-local-pr-$$"

cd "${REPO_ROOT}"

log()  { printf '\n\033[1m==> %s\033[0m\n' "$*"; }
fail() { printf '\n\033[31merror:\033[0m %s\n' "$*" >&2; exit 1; }

# ------------------------------------------------------------------------------- arguments ----

cacheless=false
keep_artifacts=false
rebuild=false
use_cache=true
command_args=()

while [[ $# -gt 0 ]]; do
    case "$1" in
        --cacheless)      cacheless=true;      shift ;;
        --keep-artifacts) keep_artifacts=true; shift ;;
        --rebuild)        rebuild=true;        shift ;;
        --no-cache)       use_cache=false;     shift ;;
        -h|--help)        sed -n '2,32p' "${BASH_SOURCE[0]}" | sed 's/^# \?//'; exit 0 ;;
        --)               shift; command_args+=("$@"); break ;;
        # Option parsing stops at the command; everything after it belongs to the command.
        *)                command_args+=("$@"); break ;;
    esac
done

command -v docker > /dev/null 2>&1 || fail "docker is not on PATH. Run ./init.sh to see what is missing."
docker info > /dev/null 2>&1 || fail "The Docker daemon is not reachable. Start Docker and try again."

[[ "$(docker info --format '{{.OSType}}' 2>/dev/null)" == "linux" ]] \
    || fail "The runner image is a Linux image, but Docker is in Windows container mode. Switch it to Linux containers."

# --------------------------------------------------------------------------------- the image ----

sdk_version="$(sed -n 's/.*"version"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' global.json | head -n 1)"
[[ -n "${sdk_version}" ]] || fail "Could not read the SDK version from global.json."

readonly IMAGE="bitenovac-cloudbuild-runner:${sdk_version}"

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
    --volume "${MAIN_VOLUME}:/mnt/main"
    --volume "${PR_VOLUME}:/mnt/pr"
    --workdir /repo
)

# A named volume rather than a bind mount of ~/.nuget, so a Linux container does not write into
# the host's package folder.
if [[ "${use_cache}" == true ]]; then
    run_args+=(--volume "${NUGET_VOLUME}:/root/.nuget/packages")
fi

[[ "${cacheless}" == true ]] && run_args+=(--env "CLOUDBUILD_CACHELESS=true")

# Interactive only when attached to a terminal, so this stays usable from a script or a hook.
if [[ -t 0 && -t 1 ]]; then
    run_args+=(--interactive --tty)
fi

cleanup_volume() {
    [[ "${keep_artifacts}" == true ]] && { log "--keep-artifacts: leaving ${PR_VOLUME} in place."; return 0; }
    docker volume rm "${PR_VOLUME}" > /dev/null 2>&1 || true
}
trap cleanup_volume EXIT

log "Running the pipeline in ${IMAGE}"

status=0
docker run "${run_args[@]}" "${IMAGE}" "${command_args[@]}" || status=$?

if [[ "${status}" -ne 0 ]]; then
    printf '\n\033[31mThe local pipeline failed (exit %s).\033[0m Its output is under artifacts/local-ci.\n' "${status}" >&2
fi

exit "${status}"
