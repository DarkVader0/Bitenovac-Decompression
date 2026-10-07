#!/usr/bin/env bash
#
# Runs one Bitenovac.RemoteBuildTool command in a fresh container, discarded when the command returns.
#
# Usage, from a workflow step with the repository checked out:
#   bash runner/in-container.sh plan
#   bash runner/in-container.sh test Debug
#
# The workspace is mounted read-write: RemoteBuildTool writes obj/ and bin/ there for real projects to
# use. Volumes carry what does not belong in the workspace or does not survive the container:
# NuGet packages, main's artifact store, this run's own artifact store, and logs. The container
# runs as the calling user, so the next checkout can delete what it leaves behind.
#
# RemoteBuildTool itself is not built from this checkout. It is the release installed into the image (see
# runner/build-image.sh), so a pull request cannot change the RemoteBuildTool that judges it.

set -euo pipefail

readonly REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
readonly NUGET_VOLUME="${REMOTEBUILDTOOL_NUGET_VOLUME:-bitenovac-runner-nuget}"
readonly MAIN_VOLUME="${REMOTEBUILDTOOL_MAIN_VOLUME:-bitenovac-main}"
readonly LOGS_VOLUME="${REMOTEBUILDTOOL_LOGS_VOLUME:-bitenovac-logs}"
readonly OFFICIAL_VOLUME="${REMOTEBUILDTOOL_OFFICIAL_VOLUME:-bitenovac-official}"
# Keyed on the run id, not the PR number, so a re-run of the same PR gets a clean volume instead
# of reading stale state a cancelled attempt left behind.
readonly PR_VOLUME="${REMOTEBUILDTOOL_PR_VOLUME:-bitenovac-pr-${GITHUB_RUN_ID:-local}}"

cd "${REPO_ROOT}"

fail() { printf '\nerror: %s\n' "$*" >&2; exit 1; }

[[ $# -gt 0 ]] || fail "No command given. Pass a Bitenovac.RemoteBuildTool command, for example: test Debug"

sdk_version="$(sed -n 's/.*"version"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' global.json | head -n 1)"
[[ -n "${sdk_version}" ]] || fail "Could not read the SDK version from global.json."

readonly IMAGE="bitenovac-remotebuildtool-runner:${sdk_version}"

docker image inspect "${IMAGE}" > /dev/null 2>&1 \
    || fail "The image ${IMAGE} is missing. Run runner/build-image.sh on this machine; it builds the image from global.json."

# main is read-only everywhere except 'promote': a PR run must not be able to write to it no
# matter what RemoteBuildTool does, and the only thing that ever calls 'promote' is a Cache refresh
# run that already passed every other stage — see .github/workflows/cache-refresh.yml.
main_mount="${MAIN_VOLUME}:/mnt/main:ro"
[[ "${1}" == "promote" ]] && main_mount="${MAIN_VOLUME}:/mnt/main"

run_args=(
    --rm
    --init
    --user "$(id -u):$(id -g)"
    --volume "${REPO_ROOT}:/repo"
    --volume "${NUGET_VOLUME}:/cache"
    --volume "${main_mount}"
    --volume "${PR_VOLUME}:/mnt/pr"
    --volume "${LOGS_VOLUME}:/mnt/logs"
    --workdir /repo
    # Running as a non-root user leaves no writable home, so both are pointed somewhere writable
    # instead of relying on one. NUGET_PACKAGES is what puts the cache on the mounted volume.
    --env NUGET_PACKAGES=/cache/nuget
    # NuGet arbitrates concurrent extraction with lock files under the temp directory, not under
    # the packages folder. Agents share the volume but not /tmp, so leaving this unset lets two
    # containers extract the same package into one directory with nothing between them, and the
    # loser's rename target disappears mid-restore.
    --env TMPDIR=/cache/tmp
    --env DOTNET_CLI_HOME=/tmp
    --env DOTNET_NOLOGO=true
    --env DOTNET_CLI_TELEMETRY_OPTOUT=true
    --env TESTINGPLATFORM_TELEMETRY_OPTOUT=1
    --env REMOTEBUILDTOOL_REPO_ROOT=/repo
    --env REMOTEBUILDTOOL_MAIN_STORE=/mnt/main
    --env REMOTEBUILDTOOL_PR_STORE=/mnt/pr
    --env REMOTEBUILDTOOL_DROP_ROOT=/mnt/official
)

# Only 'release' writes the drops, so no other command can touch them.
[[ "${1}" == "release" ]] && run_args+=(--volume "${OFFICIAL_VOLUME}:/mnt/official")

# The agent's share of the machine, from its .env (see runner/setup-runner.sh). Swap is capped to
# the same value, so a job over its memory is killed rather than paging the host.
[[ -n "${REMOTEBUILDTOOL_CPUS:-}" ]] && run_args+=(--cpus "${REMOTEBUILDTOOL_CPUS}")
[[ -n "${REMOTEBUILDTOOL_MEMORY:-}" ]] && run_args+=(--memory "${REMOTEBUILDTOOL_MEMORY}" --memory-swap "${REMOTEBUILDTOOL_MEMORY}")

# Passed through when set.
for variable in GITHUB_ACTIONS GITHUB_RUN_ID REMOTEBUILDTOOL_MAX_CPU REMOTEBUILDTOOL_CACHELESS REMOTEBUILDTOOL_COVERAGE_HTML; do
    [[ -n "${!variable:-}" ]] && run_args+=(--env "${variable}=${!variable}")
done

exec docker run "${run_args[@]}" --entrypoint bash "${IMAGE}" -c '
    set -euo pipefail
    mkdir -p "${TMPDIR}"

    # reportgenerator, which only the coverage merge in "test" runs.
    [[ "$1" == "test" ]] && dotnet tool restore > /dev/null

    exec /opt/remotebuildtool/remotebuildtool "$@"
' ci "$@"
