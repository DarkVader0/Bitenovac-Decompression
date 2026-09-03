#!/usr/bin/env bash
#
# Container entry point, reached only from docker/ci-local.sh. On the build server
# runner/in-container.sh bypasses it: the workspace is already mounted read-write there, so there
# is nothing to copy and each step invokes the published tool directly.
#
# Usage (as arguments to docker/ci-local.sh, or to 'docker run <image>'):
#   <none>            Run the whole pull request pipeline: plan, then Debug and Release build+test
#   ci <args...>      Run one Bitenovac.CloudBuild command with these arguments and nothing else
#   shell             Open an interactive bash in the prepared working copy
#   exec <args...>    Run an arbitrary command in the prepared working copy
#
# The working copy
# ----------------
# The repository is bind-mounted read-only at /host and copied to /repo before anything runs.
# The tool writes bin/, obj/ and artifacts/, and a Linux container writing those into a Windows
# working tree leaves output the next local build cannot use.
#
# The copy carries the uncommitted working tree rather than HEAD, and .git with it, because
# 'plan' hashes what is actually on disk, uncommitted changes included — the same thing CI would
# see if this were pushed.

set -euo pipefail

readonly HOST_DIR="/host"
readonly REPO_DIR="/repo"
readonly OUT_DIR="/out"
readonly MAIN_DIR="/mnt/main"
readonly PR_DIR="/mnt/pr"

log()  { printf '\n\033[1;36m==> %s\033[0m\n' "$*"; }
fail() { printf '\n\033[31merror:\033[0m %s\n' "$*" >&2; exit 1; }

prepare_working_copy() {
    [[ -d "${HOST_DIR}" ]] || fail "Nothing is mounted at ${HOST_DIR}. Run this through docker/ci-local.sh."

    log "Copying the working tree into ${REPO_DIR}"

    mkdir -p "${REPO_DIR}"

    tar -C "${HOST_DIR}" -cf - \
        --exclude='./artifacts' \
        --exclude='./TestResults' \
        --exclude='./BenchmarkDotNet.Artifacts' \
        --exclude='./.idea' \
        --exclude='./.vs' \
        --exclude='*/bin' \
        --exclude='*/obj' \
        . | tar -C "${REPO_DIR}" -xf -

    git config --global --add safe.directory "${REPO_DIR}"

    cd "${REPO_DIR}"

    printf '  %s\n' "$(git rev-parse --short HEAD 2>/dev/null || echo 'no HEAD') $(git status --porcelain 2>/dev/null | wc -l) uncommitted path(s)"
}

publish_tool() {
    log "Publishing the CloudBuild tool"
    dotnet tool restore > /dev/null
    dotnet publish src/tools/Bitenovac.CloudBuild -c Release -o /tool --nologo > /dev/null
}

export_artifacts() {
    [[ -d "${OUT_DIR}" ]] || return 0
    [[ -d "${REPO_DIR}/artifacts" ]] || return 0

    rm -rf "${OUT_DIR:?}"/*
    cp -a "${REPO_DIR}/artifacts/." "${OUT_DIR}/" 2>/dev/null || true
    printf '\nArtifacts copied to the host at artifacts/local-ci.\n'
}

run_pipeline() {
    mkdir -p "${REPO_DIR}/artifacts"
    trap export_artifacts EXIT

    export CLOUDBUILD_REPO_ROOT="${REPO_DIR}"
    export CLOUDBUILD_MAIN_STORE="${MAIN_DIR}"
    export CLOUDBUILD_PR_STORE="${PR_DIR}"

    publish_tool

    log "Plan"
    # Guarded rather than checked after the fact: 'set -e' would abort the script at a failing
    # plan before any status check below could report why.
    if ! /tool/Bitenovac.CloudBuild plan; then
        printf '\nPR: \033[31mred\033[0m -- plan failed.\n'
        return 1
    fi

    local failed=0
    local configuration
    for configuration in Debug Release; do
        log "${configuration} build"
        /tool/Bitenovac.CloudBuild build "${configuration}" || failed=1

        log "${configuration} test"
        /tool/Bitenovac.CloudBuild test "${configuration}" || failed=1
    done

    if [[ "${failed}" -ne 0 ]]; then
        printf '\nPR: \033[31mred\033[0m -- at least one stage did not succeed.\n'
        return 1
    fi

    printf '\nPR: \033[32mgreen\033[0m -- every stage succeeded.\n'
}

# ------------------------------------------------------------------------------------- main ----

prepare_working_copy

case "${1:-pipeline}" in
    pipeline)
        run_pipeline
        ;;
    ci)
        shift
        trap export_artifacts EXIT
        export CLOUDBUILD_REPO_ROOT="${REPO_DIR}" CLOUDBUILD_MAIN_STORE="${MAIN_DIR}" CLOUDBUILD_PR_STORE="${PR_DIR}"
        publish_tool
        /tool/Bitenovac.CloudBuild "$@"
        ;;
    shell) shift; exec bash "$@" ;;
    exec)  shift; trap export_artifacts EXIT; "$@" ;;
    *)     fail "Unknown command '${1}'. Expected one of: pipeline, ci, shell, exec." ;;
esac
