#!/usr/bin/env bash
#
# Runs one Bitenovac.CloudBuild command in a fresh container, discarded when the command returns.
#
# Usage, from a workflow step with the repository checked out:
#   bash runner/in-container.sh plan
#   bash runner/in-container.sh test Debug
#
# The workspace is mounted read-write: the tool writes obj/ and bin/ there for real projects to
# use. Three more volumes carry what does not belong in the workspace or does not survive the
# container: NuGet packages, main's artifact store, this run's own artifact store, and logs.
# The container runs as the calling user, so the next checkout can delete what it leaves behind.
#
# Bootstrap
# ---------
# The tool is published to a scratch directory OUTSIDE the checkout before it runs, rather than
# invoked with `dotnet run --project` in place. src/tools/Bitenovac.CloudBuild and
# src/tools/Bitenovac.CloudBuild.Core are projects in this same repository, so a run whose plan selects
# them (any run, once the tool itself has changed — see ToolVersion in the tool's own source, and
# note this is also why a change here invalidates the whole cache) tries to materialise or
# recompile its own currently-loaded assemblies. On the file locking every mainstream OS applies
# to a loaded assembly, that fails outright; a self-contained publish first means the running
# process's files and the checkout's build output never alias.

set -euo pipefail

readonly REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
readonly NUGET_VOLUME="${CLOUDBUILD_NUGET_VOLUME:-bitenovac-runner-nuget}"
readonly MAIN_VOLUME="${CLOUDBUILD_MAIN_VOLUME:-bitenovac-main}"
readonly LOGS_VOLUME="${CLOUDBUILD_LOGS_VOLUME:-bitenovac-logs}"
# Keyed on the run id, not the PR number, so a re-run of the same PR gets a clean volume instead
# of reading stale state a cancelled attempt left behind.
readonly PR_VOLUME="${CLOUDBUILD_PR_VOLUME:-bitenovac-pr-${GITHUB_RUN_ID:-local}}"
readonly TOOL_PUBLISH_VOLUME="${CLOUDBUILD_TOOL_VOLUME:-bitenovac-tool-${GITHUB_RUN_ID:-local}}"

cd "${REPO_ROOT}"

fail() { printf '\nerror: %s\n' "$*" >&2; exit 1; }

[[ $# -gt 0 ]] || fail "No command given. Pass a Bitenovac.CloudBuild command, for example: test Debug"

sdk_version="$(sed -n 's/.*"version"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' global.json | head -n 1)"
[[ -n "${sdk_version}" ]] || fail "Could not read the SDK version from global.json."

readonly IMAGE="bitenovac-cloudbuild-runner:${sdk_version}"

docker image inspect "${IMAGE}" > /dev/null 2>&1 \
    || fail "The image ${IMAGE} is missing. Re-run runner/setup-runner.sh; it builds the image from global.json."

# main is read-only everywhere except 'promote': a PR run must not be able to write to it no
# matter what the tool does, and the only thing that ever calls 'promote' is a merge_group run
# that already passed every other stage — see .github/workflows/pr.yml.
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
    --volume "${TOOL_PUBLISH_VOLUME}:/tool"
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
    --env CLOUDBUILD_REPO_ROOT=/repo
    --env CLOUDBUILD_MAIN_STORE=/mnt/main
    --env CLOUDBUILD_PR_STORE=/mnt/pr
)

# Passed through when set.
for variable in GITHUB_ACTIONS GITHUB_RUN_ID CLOUDBUILD_MAX_CPU CLOUDBUILD_CACHELESS CLOUDBUILD_COVERAGE_HTML; do
    [[ -n "${!variable:-}" ]] && run_args+=(--env "${variable}=${!variable}")
done

exec docker run "${run_args[@]}" --entrypoint bash "${IMAGE}" -c '
    set -euo pipefail
    mkdir -p "${TMPDIR}"
    dotnet tool restore > /dev/null

    # The tool is published once per container into the volume every stage of this run shares,
    # not rebuilt per stage: same cost as one dotnet run, paid once instead of four times, and
    # it is what keeps the published binary outside the checkout the tool itself operates on.
    if [[ ! -x /tool/Bitenovac.CloudBuild ]]; then
        dotnet publish src/tools/Bitenovac.CloudBuild -c Release -o /tool --nologo > /dev/null
    fi

    exec /tool/Bitenovac.CloudBuild "$@"
' ci "$@"
