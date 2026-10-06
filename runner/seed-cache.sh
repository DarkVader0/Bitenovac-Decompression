#!/usr/bin/env bash
#
# Fills main's artifact store by running the whole pipeline — plan, Debug and Release build and
# test, promote — over a committed revision of this checkout, so the first pull request after
# setup already hits the cache.
#
# Usage:
#   sudo bash runner/seed-cache.sh
#   sudo bash runner/seed-cache.sh --ref origin/master --user ci-runner
#
# Options:
#   --ref REF     Revision to seed from. Defaults to HEAD. Uncommitted changes are never seeded.
#   --user NAME   Account the jobs run as; it must own main's volume. Defaults to ci-runner.
#
# Seed from the revision the runner image was built from: every hash includes the installed
# RemoteBuildTool, and a revision whose runner/in-container.sh predates it cannot drive it.

set -euo pipefail

readonly REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

log()  { printf '\n\033[1m==> %s\033[0m\n' "$*"; }
fail() { printf '\n\033[31merror:\033[0m %s\n' "$*" >&2; exit 1; }

ref="HEAD"
run_as="ci-runner"

while [[ $# -gt 0 ]]; do
    case "$1" in
        --ref)     ref="${2:-}";    shift 2 ;;
        --user)    run_as="${2:-}"; shift 2 ;;
        -h|--help) sed -n '2,/^$/p' "${BASH_SOURCE[0]}" | sed 's/^# \?//'; exit 0 ;;
        *)         fail "Unknown argument: $1" ;;
    esac
done

[[ "${EUID}" -eq 0 ]] || fail "Run this with sudo. The copy it seeds from has to belong to ${run_as}."
id "${run_as}" > /dev/null 2>&1 || fail "There is no account named ${run_as}. Run runner/setup-runner.sh first."

workspace="$(mktemp -d /tmp/bitenovac-seed-XXXXXX)"
readonly PR_VOLUME="bitenovac-pr-seed-$$"

cleanup() {
    docker volume rm -f "${PR_VOLUME}" > /dev/null 2>&1 || true
    rm -rf "${workspace}"
}
trap cleanup EXIT

revision="$(git -c safe.directory="${REPO_ROOT}" -C "${REPO_ROOT}" rev-parse --short "${ref}")"
log "Copying ${ref} (${revision}) into ${workspace}"
git -c safe.directory="${REPO_ROOT}" -C "${REPO_ROOT}" archive "${ref}" | tar -C "${workspace}" -xf -
chown -R "${run_as}:${run_as}" "${workspace}"
cd "${workspace}"

# GITHUB_ACTIONS turns on ContinuousIntegrationBuild, which every project's hash includes: without
# it the seeded entries would never match a pull request's.
run() {
    sudo -u "${run_as}" env \
        GITHUB_ACTIONS=true \
        REMOTEBUILDTOOL_PR_VOLUME="${PR_VOLUME}" \
        REMOTEBUILDTOOL_MAX_CPU="${REMOTEBUILDTOOL_MAX_CPU:-}" \
        REMOTEBUILDTOOL_CPUS="${REMOTEBUILDTOOL_CPUS:-}" \
        REMOTEBUILDTOOL_MEMORY="${REMOTEBUILDTOOL_MEMORY:-}" \
        bash "${workspace}/runner/in-container.sh" "$@"
}

log "Plan";            run plan
log "Debug build";     run build Debug
log "Debug test";      run test Debug
log "Release build";   run build Release
log "Release test";    run test Release
log "Promote";         run promote
