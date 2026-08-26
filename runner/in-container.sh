#!/usr/bin/env bash
#
# Runs one build/ci.sh command in a fresh container, discarded when the command returns.
#
# Usage, from a workflow step with the repository checked out:
#   bash runner/in-container.sh plan
#   bash runner/in-container.sh test Debug
#
# The workspace is mounted read-write: the build writes artifacts/ there and later steps read it.
# Packages go to a named volume instead, so they outlive the container. The container runs as the
# calling user, so the next checkout can delete what it leaves behind.

set -euo pipefail

readonly REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
readonly NUGET_VOLUME="${CI_NUGET_VOLUME:-bitenovac-runner-nuget}"

cd "${REPO_ROOT}"

fail() { printf '\nerror: %s\n' "$*" >&2; exit 1; }

[[ $# -gt 0 ]] || fail "No command given. Pass a build/ci.sh command, for example: test Debug"

sdk_version="$(sed -n 's/.*"version"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' global.json | head -n 1)"
[[ -n "${sdk_version}" ]] || fail "Could not read the SDK version from global.json."

readonly IMAGE="bitenovac-ci-runner:${sdk_version}"

docker image inspect "${IMAGE}" > /dev/null 2>&1 \
    || fail "The image ${IMAGE} is missing. Re-run runner/setup-runner.sh; it builds the image from global.json."

run_args=(
    --rm
    --init
    --user "$(id -u):$(id -g)"
    --volume "${REPO_ROOT}:/repo"
    --volume "${NUGET_VOLUME}:/cache"
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
)

# Passed through when set.
for variable in BASE_SHA CHANGED_FILES CI_SHARD CI_SHARDS CI_STAGE CI_COVERAGE_HTML CI_MAX_CPU \
                GITHUB_ACTIONS GITHUB_OUTPUT GITHUB_STEP_SUMMARY; do
    [[ -n "${!variable:-}" ]] && run_args+=(--env "${variable}=${!variable}")
done

# GITHUB_OUTPUT and GITHUB_STEP_SUMMARY are files under RUNNER_TEMP, outside the workspace.
# Without this mount those writes go nowhere.
if [[ -n "${RUNNER_TEMP:-}" && -d "${RUNNER_TEMP}" ]]; then
    run_args+=(--volume "${RUNNER_TEMP}:${RUNNER_TEMP}")
fi

# The image entrypoint expects a read-only mount to copy from; the workspace is already in place,
# so it is bypassed. Tools are restored here because this is where they run: a second or two
# against a warm volume, and nothing is installed on the host.
exec docker run "${run_args[@]}" --entrypoint bash "${IMAGE}" \
    -c 'mkdir -p "${TMPDIR}" && dotnet tool restore > /dev/null && exec bash build/ci.sh "$@"' ci "$@"
