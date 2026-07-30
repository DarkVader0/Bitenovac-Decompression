#!/usr/bin/env bash
#
# Continuous integration entry point. The workflow files stay thin and call into here, so
# every step a pull request runs can also be run locally with the same command.
#
# Usage:
#   BASE_SHA=<ref> build/ci.sh affected            List the projects a change reaches
#   BASE_SHA=<ref> build/ci.sh restore             Restore those projects
#   BASE_SHA=<ref> build/ci.sh build CONFIG        Build those projects
#   BASE_SHA=<ref> build/ci.sh test CONFIG         Run their tests and collect coverage
#   BASE_SHA=<ref> build/ci.sh coverage-gate CONFIG  Require full coverage of changed libraries
#                  build/ci.sh graph               Print the project dependency graph
#
# CONFIG is 'Debug' or 'Release'. BASE_SHA is the commit to diff against; when it is unset or
# unusable every project is treated as affected, which is the safe default.
#
# Work is selected from the project reference graph rather than from directory names. A
# project is affected when it changed, or when it depends -- directly or transitively -- on
# something that changed. Nothing upstream of the change is retested: editing Core cannot
# alter Units, so Units' tests do not run, though Units is still compiled because Core needs
# it to build.
#
# The API is excluded from library changes by the same rule, with no special case: it consumes
# the algorithms as published, versioned NuGet packages rather than by project reference, so
# it is not a dependent of anything under src/libraries. 'verify-isolation' keeps that true.
#
# Run the coverage gate against Debug. Release IL is optimised and inlined, so the branch
# points coverlet records stop mapping cleanly onto the source and fully covered code reports
# less than 100% branch coverage.

set -euo pipefail

readonly REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
readonly TARGET_FRAMEWORK="net10.0"
# Relative to REPO_ROOT, which this script cds into: absolute MSYS-style paths are not
# understood by the Windows .NET tools, and relative paths work identically on Linux.
readonly ARTIFACTS_DIR="artifacts"
# Per-project thresholds come from MSBuild (see the coverage policy in Directory.Build.props).

# Changing any of these changes how everything compiles, so they mark every project affected.
readonly SHARED_PATHS=(
    "Directory.Build.props"
    "Directory.Build.targets"
    "Directory.Packages.props"
    "global.json"
    "nuget.config"
    ".editorconfig"
    ".config/dotnet-tools.json"
)

cd "${REPO_ROOT}"

log()  { printf '\n\033[1m==> %s\033[0m\n' "$*"; }
fail() { printf '::error::%s\n' "$*" >&2; exit 1; }

declare -a PROJECTS=()
declare -A DEPENDENTS=()
declare -a AFFECTED=()

# ------------------------------------------------------------------------------ graph ----

# Scans the whole repository rather than a fixed list of roots, so a project added anywhere
# is picked up without editing this script.
discover_projects() {
    mapfile -t PROJECTS < <(
        find . -name '*.csproj' \
            -not -path '*/bin/*' \
            -not -path '*/obj/*' \
            -not -path './artifacts/*' \
            2>/dev/null | sed 's|^\./||' | sort
    )
    [[ ${#PROJECTS[@]} -gt 0 ]] || fail "No projects found in the repository."
}

# Emits each ProjectReference of a project as a repository-relative path, so references
# written with '..' segments and Windows separators compare equal to discovered paths.
project_references() {
    local project="$1"
    local directory
    directory="$(dirname "${project}")"

    local include
    while IFS= read -r include; do
        [[ -z "${include}" ]] && continue
        include="${include//\\//}"
        realpath -m --relative-to="${REPO_ROOT}" "${directory}/${include}"
    done < <(
        grep -oE '<ProjectReference[[:space:]]+Include="[^"]+"' "${project}" 2>/dev/null \
            | sed -E 's/.*Include="([^"]+)".*/\1/'
    )
}

build_graph() {
    discover_projects

    local project reference
    for project in "${PROJECTS[@]}"; do
        while IFS= read -r reference; do
            [[ -z "${reference}" ]] && continue
            DEPENDENTS["${reference}"]="${DEPENDENTS[${reference}]:-}${project} "
        done < <(project_references "${project}")
    done
}

cmd_graph() {
    build_graph
    log "Project dependency graph (X <- Y means Y must rebuild when X changes)"
    local project
    for project in "${PROJECTS[@]}"; do
        printf '%s\n' "${project}"
        local dependent
        for dependent in ${DEPENDENTS[${project}]:-}; do
            printf '    <- %s\n' "${dependent}"
        done
    done
}

# --------------------------------------------------------------------------- affected ----

# Returns the project owning a file: the nearest ancestor directory holding a .csproj.
owning_project() {
    local directory
    directory="$(dirname "$1")"

    while [[ "${directory}" != "." && "${directory}" != "/" && -n "${directory}" ]]; do
        shopt -s nullglob
        local candidates=("${directory}"/*.csproj)
        shopt -u nullglob
        if [[ ${#candidates[@]} -gt 0 ]]; then
            printf '%s\n' "${candidates[0]}"
            return 0
        fi
        directory="$(dirname "${directory}")"
    done

    return 1
}

is_shared_path() {
    local file="$1" shared
    for shared in "${SHARED_PATHS[@]}"; do
        [[ "${file}" == "${shared}" ]] && return 0
    done
    # The pipeline itself decides how everything is built and tested.
    [[ "${file}" == build/* || "${file}" == .github/workflows/* ]] && return 0
    return 1
}

# Walks the reverse edges of the graph from the changed projects, so a project is included
# when it changed or when anything it depends on changed.
compute_affected() {
    build_graph

    local -a seeds=()
    local everything=false
    local files=""

    if [[ -n "${CHANGED_FILES:-}" ]]; then
        # Dry run: answer "what would CI do if I touched these files?" without needing a
        # commit to diff against. CHANGED_FILES is a newline-separated list of paths.
        files="${CHANGED_FILES}"
    elif [[ -n "${BASE_SHA:-}" ]] && git rev-parse --verify --quiet "${BASE_SHA}^{commit}" >/dev/null 2>&1; then
        # Three dots: diff against the merge base, so commits that landed on the target branch
        # after the pull request opened do not look like changes.
        files="$(git diff --name-only "${BASE_SHA}...HEAD")"
    else
        echo "No usable BASE_SHA ('${BASE_SHA:-}'); treating every project as affected." >&2
        everything=true
    fi

    if [[ "${everything}" == false ]]; then
        if [[ -z "${files}" ]]; then
            echo "No files changed."
        else
            echo "Changed files:"
            local file
            while IFS= read -r file; do
                [[ -z "${file}" ]] && continue

                if is_shared_path "${file}"; then
                    printf '  %s  (shared: affects every project)\n' "${file}"
                    everything=true
                    continue
                fi

                local owner=""
                owner="$(owning_project "${file}" || true)"
                if [[ -n "${owner}" ]]; then
                    printf '  %s  -> %s\n' "${file}" "${owner}"
                    seeds+=("${owner}")
                else
                    printf '  %s  (no project; ignored)\n' "${file}"
                fi
            done <<< "${files}"
        fi
    fi

    if [[ "${everything}" == true ]]; then
        AFFECTED=("${PROJECTS[@]}")
        return
    fi

    local -A seen=()
    local -a queue=("${seeds[@]:-}")
    while [[ ${#queue[@]} -gt 0 ]]; do
        local current="${queue[0]}"
        queue=("${queue[@]:1}")
        [[ -z "${current}" ]] && continue
        [[ -n "${seen[${current}]:-}" ]] && continue
        seen["${current}"]=1

        local dependent
        for dependent in ${DEPENDENTS[${current}]:-}; do
            queue+=("${dependent}")
        done
    done

    if [[ ${#seen[@]} -eq 0 ]]; then
        AFFECTED=()
        return
    fi

    mapfile -t AFFECTED < <(printf '%s\n' "${!seen[@]}" | sort)
}

affected_test_projects() {
    local project
    for project in "${AFFECTED[@]:-}"; do
        [[ -z "${project}" ]] && continue
        [[ "$(basename "${project}" .csproj)" == *.Tests ]] && printf '%s\n' "${project}"
    done
}

# Reads a project's coverage policy from MSBuild rather than from the raw XML, so the
# Directory.Build.props defaults and any per-project override resolve exactly as the compiler
# sees them. Emits: assembly<TAB>excluded<TAB>minimumLine<TAB>minimumBranch
read_coverage_policy() {
    local project="$1"

    # Asking for several properties at once prints one "name": "value" pair per line, which is
    # why a sed extraction is enough and the gate needs nothing beyond the .NET SDK.
    local evaluated
    evaluated="$(dotnet msbuild "${project}" -nologo \
        -getProperty:AssemblyName \
        -getProperty:ExcludeFromCoverage \
        -getProperty:MinimumLineCoverage \
        -getProperty:MinimumBranchCoverage 2>/dev/null)"

    local assembly excluded minimum_line minimum_branch
    assembly="$(sed -n 's/.*"AssemblyName": "\(.*\)".*/\1/p' <<< "${evaluated}")"
    excluded="$(sed -n 's/.*"ExcludeFromCoverage": "\(.*\)".*/\1/p' <<< "${evaluated}")"
    minimum_line="$(sed -n 's/.*"MinimumLineCoverage": "\(.*\)".*/\1/p' <<< "${evaluated}")"
    minimum_branch="$(sed -n 's/.*"MinimumBranchCoverage": "\(.*\)".*/\1/p' <<< "${evaluated}")"

    printf '%s\t%s\t%s\t%s\n' \
        "${assembly}" "${excluded:-false}" "${minimum_line:-100}" "${minimum_branch:-100}"
}

cmd_affected() {
    compute_affected

    echo
    if [[ ${#AFFECTED[@]} -eq 0 ]]; then
        echo "Affected projects: none"
    else
        echo "Affected projects (${#AFFECTED[@]}):"
        printf '  %s\n' "${AFFECTED[@]}"
    fi

    local tests
    tests="$(affected_test_projects || true)"
    if [[ -n "${tests}" ]]; then
        echo "Test projects to run:"
        printf '%s\n' "${tests}" | sed 's/^/  /'
    else
        echo "Test projects to run: none"
    fi

    local any=false
    [[ ${#AFFECTED[@]} -gt 0 ]] && any=true

    if [[ -n "${GITHUB_OUTPUT:-}" ]]; then
        echo "any=${any}" >> "${GITHUB_OUTPUT}"
    fi
    echo "any=${any}"
}

# ------------------------------------------------------------------- verify-isolation ----

# Skipping the API on a library change is only sound while the API depends on published
# package versions. A project reference would make the API a dependent of the libraries, and
# quietly turn every library pull request into an API build too.
cmd_verify_isolation() {
    log "Verifying the API does not project-reference the libraries"

    discover_projects

    local offenders=""
    local project reference
    for project in "${PROJECTS[@]}"; do
        [[ "${project}" == src/apps/* || "${project}" == tests/apps/* ]] || continue
        while IFS= read -r reference; do
            [[ "${reference}" == src/libraries/* ]] && offenders+="  ${project} -> ${reference}"$'\n'
        done < <(project_references "${project}")
    done

    if [[ -n "${offenders}" ]]; then
        printf '%s\n' "${offenders}" >&2
        fail "App projects project-reference the libraries. The API is meant to consume them as versioned packages; a project reference makes every library change rebuild and retest the API."
    fi

    echo "OK: no app project references src/libraries."
}

# ------------------------------------------------------------------ restore/build/test ----

require_configuration() {
    case "${1:-}" in
        Debug|Release) ;;
        *) fail "Configuration must be 'Debug' or 'Release', got '${1:-}'." ;;
    esac
}

cmd_restore() {
    compute_affected
    [[ ${#AFFECTED[@]} -gt 0 ]] || { echo "Nothing affected; skipping restore."; return 0; }

    log "Restoring ${#AFFECTED[@]} affected project(s)"
    # --locked-mode makes a stale packages.lock.json fail the build instead of being silently
    # rewritten, which the repository guidelines require.
    local project
    for project in "${AFFECTED[@]}"; do
        dotnet restore "${project}" --locked-mode
    done
}

cmd_build() {
    local configuration="${1:-}"
    require_configuration "${configuration}"

    compute_affected
    [[ ${#AFFECTED[@]} -gt 0 ]] || { echo "Nothing affected; skipping build."; return 0; }

    log "Building ${#AFFECTED[@]} affected project(s) (${configuration})"
    # Upstream dependencies are compiled by MSBuild as inputs even though they are not in the
    # affected set; only their tests are skipped.
    local project
    for project in "${AFFECTED[@]}"; do
        dotnet build "${project}" --configuration "${configuration}" --no-restore
    done
}

cmd_test() {
    local configuration="${1:-}"
    require_configuration "${configuration}"

    compute_affected

    local -a test_projects=()
    mapfile -t test_projects < <(affected_test_projects || true)
    if [[ ${#test_projects[@]} -eq 0 || -z "${test_projects[0]:-}" ]]; then
        echo "No affected test projects; nothing to run."
        return 0
    fi

    local coverage_dir="${ARTIFACTS_DIR}/coverage/${configuration}"
    rm -rf "${coverage_dir}"
    mkdir -p "${coverage_dir}"

    log "Testing ${#test_projects[@]} affected test project(s) (${configuration})"

    local failed="" empty=""
    local project
    for project in "${test_projects[@]}"; do
        local name
        name="$(basename "${project}" .csproj)"
        local assembly="$(dirname "${project}")/bin/${configuration}/${TARGET_FRAMEWORK}/${name}.dll"
        [[ -f "${assembly}" ]] || fail "Test assembly not found: ${assembly}. Run 'build' first."

        echo "--- ${name}"
        # The test projects run on Microsoft.Testing.Platform. 'dotnet test' does not forward
        # extension arguments to the test app, so --coverlet reaches nothing and no coverage is
        # produced; executing the assembly directly is what actually collects it.
        local status=0
        dotnet exec "${assembly}" \
            --coverlet \
            --coverlet-output-format cobertura \
            --coverlet-file-prefix "${name}" \
            --results-directory "${coverage_dir}" || status=$?

        case "${status}" in
            0) ;;
            # Microsoft.Testing.Platform returns 8 when a run discovers no tests (its
            # minimum-expected-tests policy) and 5 when a filter selects none. Neither is a
            # failing assertion, so name them separately instead of reporting a phantom failure.
            5|8) empty+="  ${name}"$'\n' ;;
            *)   failed+="  ${name} (exit ${status})"$'\n' ;;
        esac
    done

    [[ -n "${empty}" ]]  && printf 'Test projects containing no tests:\n%s\n' "${empty}" >&2
    [[ -n "${failed}" ]] && printf 'Test projects with failing tests:\n%s\n' "${failed}" >&2

    if [[ -n "${failed}" || -n "${empty}" ]]; then
        fail "Tests did not pass (${configuration})."
    fi

    echo "All affected tests passed (${configuration})."
}

# ---------------------------------------------------------------------- coverage-gate ----

cmd_coverage_gate() {
    local configuration="${1:-}"
    require_configuration "${configuration}"

    compute_affected

    log "Reading the coverage policy of each affected project"

    # Each entry is assembly<TAB>minimumLine<TAB>minimumBranch for a project the gate judges.
    local -a gated=()
    local project
    for project in "${AFFECTED[@]:-}"; do
        [[ -z "${project}" ]] && continue

        local policy assembly excluded minimum_line minimum_branch
        policy="$(read_coverage_policy "${project}")"
        IFS=$'\t' read -r assembly excluded minimum_line minimum_branch <<< "${policy}"

        if [[ -z "${assembly}" ]]; then
            printf '  ?    %-52s could not read its coverage policy\n' "$(basename "${project}" .csproj)"
            fail "Failed to evaluate the coverage policy of ${project}."
        fi

        if [[ "${excluded,,}" == "true" ]]; then
            printf '  skip %-52s ExcludeFromCoverage=true\n' "${assembly}"
            continue
        fi

        printf '  gate %-52s line >= %s%%  branch >= %s%%\n' "${assembly}" "${minimum_line}" "${minimum_branch}"
        gated+=("${assembly}"$'\t'"${minimum_line}"$'\t'"${minimum_branch}")
    done

    if [[ ${#gated[@]} -eq 0 ]]; then
        echo "No affected project is under the coverage gate; nothing to check."
        return 0
    fi

    # A project with no test project at all would otherwise fail further down with a confusing
    # complaint about missing coverage files. Name the real problem instead: a project held to
    # a coverage bar has to be tested by something.
    local -a test_projects=()
    mapfile -t test_projects < <(affected_test_projects || true)
    if [[ ${#test_projects[@]} -eq 0 || -z "${test_projects[0]:-}" ]]; then
        printf '::error::Nothing tests these changed projects: %s\n' \
            "$(printf '%s ' "${gated[@]%%$'\t'*}")" >&2
        fail "A project under the coverage gate must be tested by something. Add a test project ending in '.Tests' with a ProjectReference to it, or set <ExcludeFromCoverage>true</ExcludeFromCoverage> in the project."
    fi

    local coverage_dir="${ARTIFACTS_DIR}/coverage/${configuration}"
    local report_dir="${ARTIFACTS_DIR}/coverage-report/${configuration}"

    shopt -s nullglob
    local reports=("${coverage_dir}"/*cobertura*.xml)
    shopt -u nullglob
    [[ ${#reports[@]} -gt 0 ]] || fail "No coverage reports under ${coverage_dir}. Did 'test' run?"

    rm -rf "${report_dir}"

    # ReportGenerator merges the per-project reports. Merging matters: each test project only
    # exercises part of a library, and coverlet writes the source path differently depending on
    # which project produced the report, so a merge keyed on file path double counts every line.
    local generator_output
    if ! generator_output="$(dotnet reportgenerator \
        "-reports:${coverage_dir}/*cobertura*.xml" \
        "-targetdir:${report_dir}" \
        "-reporttypes:Cobertura;TextSummary;Html" 2>&1)"; then
        # Its output is captured to keep the log readable, so it has to be replayed on failure
        # rather than discarded, or the gate dies with no explanation at all.
        printf '%s\n' "${generator_output}" >&2
        fail "ReportGenerator failed to merge the coverage reports."
    fi

    # The merged Cobertura report carries one <package> element per assembly with its rates as
    # attributes on a single line, which is what the gate reads below.
    local merged="${report_dir}/Cobertura.xml"
    [[ -f "${merged}" ]] || fail "ReportGenerator produced no merged report at ${merged}."

    local exit_code=0
    local entry
    for entry in "${gated[@]}"; do
        local assembly minimum_line minimum_branch
        IFS=$'\t' read -r assembly minimum_line minimum_branch <<< "${entry}"

        local element line branch
        element="$(grep -oE "<package name=\"${assembly}\" line-rate=\"[^\"]*\" branch-rate=\"[^\"]*\"" \
            "${merged}" | head -1 || true)"

        line=""
        branch=""
        if [[ -n "${element}" ]]; then
            # Cobertura states rates as fractions; the policy is expressed in percent.
            line="$(awk -v r="$(sed -n 's/.*line-rate="\([^"]*\)".*/\1/p' <<< "${element}")" \
                'BEGIN { printf "%.2f", r * 100 }')"
            branch="$(awk -v r="$(sed -n 's/.*branch-rate="\([^"]*\)".*/\1/p' <<< "${element}")" \
                'BEGIN { printf "%.2f", r * 100 }')"
        fi

        if [[ -z "${line}" ]]; then
            printf '::error::%s is under the coverage gate but no test exercised it, so it produced no coverage data.\n' "${assembly}" >&2
            exit_code=1
            continue
        fi

        if awk "BEGIN { exit !(${line} >= ${minimum_line} && ${branch} >= ${minimum_branch}) }"; then
            printf '  OK   %-52s line %6s%%  branch %6s%%\n' "${assembly}" "${line}" "${branch}"
        else
            printf '::error::%s is below its coverage gate: line %s%% (min %s%%), branch %s%% (min %s%%)\n' \
                "${assembly}" "${line}" "${minimum_line}" "${branch}" "${minimum_branch}" >&2
            exit_code=1
        fi
    done

    if [[ ${exit_code} -ne 0 ]]; then
        echo "Open ${report_dir}/index.html to see which lines are uncovered." >&2
        exit "${exit_code}"
    fi

    echo "Every gated project meets its coverage policy."
}

# ------------------------------------------------------------------------------- main ----

case "${1:-}" in
    affected)         shift; cmd_affected "$@" ;;
    graph)            shift; cmd_graph "$@" ;;
    verify-isolation) shift; cmd_verify_isolation "$@" ;;
    restore)          shift; cmd_restore "$@" ;;
    build)            shift; cmd_build "$@" ;;
    test)             shift; cmd_test "$@" ;;
    coverage-gate)    shift; cmd_coverage_gate "$@" ;;
    *)
        sed -n '2,30p' "${BASH_SOURCE[0]}" | sed 's/^# \?//'
        exit 1
        ;;
esac
