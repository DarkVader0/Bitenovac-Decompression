#!/usr/bin/env bash
#
# Container entry point, reached only from docker/ci-local.sh. On the build server
# runner/in-container.sh bypasses it: the workspace is already mounted read-write there, so there
# is nothing to copy and each step invokes build/ci.sh directly.
#
# Usage (as arguments to docker/ci-local.sh, or to 'docker run <image>'):
#   <none>            Run the whole pull request pipeline: plan, verify, then every shard of the
#                     Debug and Release stages
#   ci <args...>      Run build/ci.sh with these arguments and nothing else
#   shell             Open an interactive bash in the prepared working copy
#   exec <args...>    Run an arbitrary command in the prepared working copy
#
# The working copy
# ----------------
# The repository is bind-mounted read-only at /host and copied to /repo before anything runs.
# The build writes bin/, obj/ and artifacts/, and a Linux container writing those into a Windows
# working tree leaves output the next local build cannot use.
#
# The copy carries the uncommitted working tree rather than HEAD, and .git with it, because
# 'plan' diffs against BASE_SHA.

set -euo pipefail

readonly HOST_DIR="/host"
readonly REPO_DIR="/repo"
readonly OUT_DIR="/out"

log()  { printf '\n\033[1;36m==> %s\033[0m\n' "$*"; }
fail() { printf '\n\033[31merror:\033[0m %s\n' "$*" >&2; exit 1; }

prepare_working_copy() {
    [[ -d "${HOST_DIR}" ]] || fail "Nothing is mounted at ${HOST_DIR}. Run this through docker/ci-local.sh."

    log "Copying the working tree into ${REPO_DIR}"

    mkdir -p "${REPO_DIR}"

    # Build output is excluded rather than copied and deleted. A Windows obj/ carries absolute
    # paths and a NuGet asset file describing a different RID, so restoring on top of it fails
    # for reasons unrelated to the change being tested.
    tar -C "${HOST_DIR}" -cf - \
        --exclude='./artifacts' \
        --exclude='./TestResults' \
        --exclude='./BenchmarkDotNet.Artifacts' \
        --exclude='./.idea' \
        --exclude='./.vs' \
        --exclude='*/bin' \
        --exclude='*/obj' \
        . | tar -C "${REPO_DIR}" -xf -

    # The copy is owned by root and came from another machine, which git refuses to read.
    git config --global --add safe.directory "${REPO_DIR}"

    cd "${REPO_DIR}"

    printf '  %s\n' "$(git rev-parse --short HEAD 2>/dev/null || echo 'no HEAD') $(git status --porcelain 2>/dev/null | wc -l) uncommitted path(s)"
}

# Copies what the run produced back out, when docker/ci-local.sh mounted somewhere to put it.
# Registered as an EXIT trap so a failed run still leaves its coverage report.
export_artifacts() {
    [[ -d "${OUT_DIR}" ]] || return 0
    [[ -d "${REPO_DIR}/artifacts" ]] || return 0

    rm -rf "${OUT_DIR:?}"/*
    cp -a "${REPO_DIR}/artifacts/." "${OUT_DIR}/" 2>/dev/null || true
    printf '\nArtifacts copied to the host at artifacts/local-ci.\n'
}

# --------------------------------------------------------------------------------- pipeline ----

# Shards run one after another rather than on parallel runners, since a single container has one
# set of cores. A failing shard does not stop the rest.

readonly SUMMARY_FILE="${REPO_DIR}/artifacts/ci-summary.md"

# GitHub's vocabulary, which is what build/ci.sh summarise renders: a phase after a failed one
# reports 'skipped', distinguishing "the build broke" from "the tests ran and failed".
declare -A stage_result=()

run_phase() {
    local stage="$1" shard="$2" phase="$3"
    shift 3

    local key="${stage}/${shard}"

    if [[ "${stage_result[${key}]:-ok}" != "ok" ]]; then
        phase_outcomes+=("${phase}=skipped")
        return 0
    fi

    log "${stage} shard ${shard}: ${phase}"
    if CI_SHARD="${shard}" "$@"; then
        phase_outcomes+=("${phase}=success")
    else
        phase_outcomes+=("${phase}=failure")
        stage_result["${key}"]="failed"
    fi
}

run_stage_shard() {
    local stage="$1" shard="$2"
    local key="${stage}/${shard}"
    stage_result["${key}"]="ok"

    phase_outcomes=()

    run_phase "${stage}" "${shard}" restore bash build/ci.sh restore "${stage}"
    run_phase "${stage}" "${shard}" build   bash build/ci.sh build   "${stage}"
    run_phase "${stage}" "${shard}" test    bash build/ci.sh test    "${stage}"

    # Debug only, matching the workflow.
    if [[ "${stage}" == "Debug" ]]; then
        run_phase "${stage}" "${shard}" coverage-gate bash build/ci.sh coverage-gate Debug
    fi

    CI_STAGE="${stage}" CI_SHARD="${shard}" bash build/ci.sh summarise "${phase_outcomes[@]}" || true
}

run_pipeline() {
    mkdir -p "${REPO_DIR}/artifacts"
    trap export_artifacts EXIT

    # build/ci.sh writes to these when set, which is how the plan hands its outputs to the later
    # jobs and how every shard reports into one summary page.
    export GITHUB_OUTPUT="${REPO_DIR}/artifacts/ci-outputs.txt"
    export GITHUB_STEP_SUMMARY="${SUMMARY_FILE}"
    : > "${GITHUB_OUTPUT}"
    : > "${GITHUB_STEP_SUMMARY}"

    log "Plan"
    bash build/ci.sh plan

    local any shards
    any="$(sed -n 's/^any=//p' "${GITHUB_OUTPUT}" | tail -n 1)"
    shards="$(sed -n 's/^shards=//p' "${GITHUB_OUTPUT}" | tail -n 1)"

    if [[ "${any}" != "true" ]]; then
        log "Nothing is affected; the Debug and Release stages would be skipped."
        printf '\nPR: \033[32mgreen\033[0m (no affected projects)\n'
        return 0
    fi

    log "Verify build assumptions"
    local verify_result="success"
    bash build/ci.sh verify || verify_result="failure"

    log "Restoring local tools"
    dotnet tool restore

    local indices
    indices="$(tr -d '[]' <<< "${shards}" | tr ',' ' ')"

    local stage shard
    for stage in Debug Release; do
        for shard in ${indices}; do
            run_stage_shard "${stage}" "${shard}"
        done
    done

    # ------------------------------------------------------------------------------ board ----

    printf '\n\033[1m%s\033[0m\n' "Results"
    printf '  %-10s %s\n' "verify" "${verify_result}"

    local failed=0
    [[ "${verify_result}" == "success" ]] || failed=1

    local key
    for stage in Debug Release; do
        for shard in ${indices}; do
            key="${stage}/${shard}"
            printf '  %-10s shard %-3s %s\n' "${stage}" "${shard}" "${stage_result[${key}]}"
            [[ "${stage_result[${key}]}" == "ok" ]] || failed=1
        done
    done

    if [[ -s "${SUMMARY_FILE}" ]]; then
        printf '\n\033[1m%s\033[0m\n' "Job summary (artifacts/local-ci/ci-summary.md)"
        sed 's/^/  /' "${SUMMARY_FILE}"
    fi

    if [[ "${failed}" -ne 0 ]]; then
        printf '\nPR: \033[31mred\033[0m -- at least one required job did not succeed.\n'
        return 1
    fi

    printf '\nPR: \033[32mgreen\033[0m -- all required jobs succeeded or were skipped.\n'
}

# ------------------------------------------------------------------------------------- main ----

prepare_working_copy

case "${1:-pipeline}" in
    pipeline) run_pipeline ;;
    ci)       shift; trap export_artifacts EXIT; bash build/ci.sh "$@" ;;
    shell)    shift; exec bash "$@" ;;
    exec)     shift; trap export_artifacts EXIT; "$@" ;;
    *)        fail "Unknown command '${1}'. Expected one of: pipeline, ci, shell, exec." ;;
esac
