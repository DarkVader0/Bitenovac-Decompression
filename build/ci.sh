#!/usr/bin/env bash
#
# Continuous integration entry point. Every CI step is one of these commands, so any of them can
# be run locally.
#
# Usage:
#   BASE_SHA=<ref> build/ci.sh plan                  Compute the graph, the affected set, the shards
#                  build/ci.sh graph                 Print the project dependency graph
#                  build/ci.sh verify                Check the invariants the plan relies on
#                  build/ci.sh restore CONFIG        Restore the selected projects
#                  build/ci.sh build CONFIG          Build the selected projects
#                  build/ci.sh test CONFIG           Run their tests and collect coverage
#                  build/ci.sh coverage-gate CONFIG  Require full coverage of the selected projects
#                  build/ci.sh summarise PHASE=RESULT...  Report a shard's outcome to the job summary
#
# CONFIG is 'Debug' or 'Release'.
#
# 'plan' writes everything the later commands need into artifacts/ci; nothing recomputes it. In
# CI that directory travels between jobs as an artifact, keeping the shard partition identical
# in every stage.
#
# Selection
# ---------
# BASE_SHA is the commit to diff against. When unset or unusable, every project is treated as
# affected. CHANGED_FILES overrides it with a newline separated list, for answering "what would
# CI do if I touched these?" without a commit.
#
# Work is selected from the project reference graph, not from directory names. A project is
# affected when it changed, or when it depends directly or transitively on something that
# changed. Nothing upstream of a change is retested: editing Core cannot alter Units, so Units'
# tests do not run, though Units is still compiled as an input to Core.
#
# A shared build file affects the subtree it sits in. Directory.Build.props at the root marks
# everything; one under tests/ marks only the tests. Without this scoping, any change to a
# shared file rebuilds the entire repository.
#
# build/, .github/workflows/ and the scripts in runner/ are the exception and mark every project:
# they govern how everything is compiled, tested and measured, so no subtree stands in for the
# whole. runner/in-container.sh in particular sits between the workflow and every build, where a
# wrong mount or a dropped environment variable would otherwise merge green having tested nothing.
#
# The API falls out of library changes by the same rule, with no special case: it consumes the
# algorithms as versioned NuGet packages rather than by project reference, so it is not a
# dependent of anything under src/libraries. 'verify' keeps that true.
#
# Sharding
# --------
# CI_SHARD selects one shard of the plan; unset means every affected project. Shards are
# self-contained by construction (see build/shards.awk), so a shard restores, builds, tests and
# gates coverage without reading from any other shard.
#
# Coverage
# --------
# The coverage gate runs against Debug. Release IL is optimised and inlined, so coverlet's branch
# points stop mapping onto the source and fully covered code reports below 100% branch coverage.

set -euo pipefail

readonly REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

# Under Git Bash the .NET tools are native Windows programs and cannot read an MSYS path such as
# /d/Projekti. Paths handed to MSBuild are either relative to REPO_ROOT or rooted at this.
if command -v cygpath > /dev/null 2>&1; then
    readonly REPO_ROOT_NATIVE="$(cygpath -w "${REPO_ROOT}")\\"
else
    readonly REPO_ROOT_NATIVE="${REPO_ROOT}/"
fi

readonly ARTIFACTS_DIR="artifacts"
readonly PLAN_DIR="${ARTIFACTS_DIR}/ci"
readonly BUILD_DIR="build"
readonly RUNNER_DIR="runner"

readonly PROJECTS_FILE="${PLAN_DIR}/projects.txt"
readonly EDGES_FILE="${PLAN_DIR}/edges.tsv"
readonly SEEDS_FILE="${PLAN_DIR}/seeds.txt"
readonly AFFECTED_FILE="${PLAN_DIR}/affected.txt"
readonly INFO_FILE="${PLAN_DIR}/info.tsv"
readonly SHARDS_FILE="${PLAN_DIR}/shards.json"

# Files whose contents change how everything beneath them compiles. A change to one marks every
# project in its own directory and below, and nothing outside it.
readonly SCOPING_FILES=(
    "Directory.Build.props"
    "Directory.Build.targets"
    "Directory.Packages.props"
    ".editorconfig"
    "global.json"
    "nuget.config"
    "NuGet.config"
    "NuGet.Config"
)

# Roughly how many projects one runner should own. Lower means more shards and more parallelism,
# at the cost of a runner start-up and a restore each. Override with CI_SHARDS to pin a count.
readonly PROJECTS_PER_SHARD=8
readonly MAX_SHARDS=16

cd "${REPO_ROOT}"

log()  { printf '\n\033[1m==> %s\033[0m\n' "$*"; }
warn() { printf '::warning::%s\n' "$*" >&2; }
fail() { printf '::error::%s\n' "$*" >&2; exit 1; }

# Appends markdown to the run's job summary, collecting what each of the two stages times N
# shards found onto one page. A no-op outside CI.
summary() {
    [[ -n "${GITHUB_STEP_SUMMARY:-}" ]] || return 0
    printf '%s\n' "$*" >> "${GITHUB_STEP_SUMMARY}"
}

require_configuration() {
    case "${1:-}" in
        Debug|Release) ;;
        *) fail "Configuration must be 'Debug' or 'Release', got '${1:-}'." ;;
    esac
}

# ------------------------------------------------------------------------------ graph ----

# Scans the whole repository rather than a fixed list of roots, so a project added anywhere is
# picked up without editing this script. Every MSBuild language is included: the graph, the
# shards and the coverage gate use project references and output paths, neither C#-specific.
discover_projects() {
    find . \
        \( -name '*.csproj' -o -name '*.fsproj' -o -name '*.vbproj' \) \
        -not -path '*/bin/*' \
        -not -path '*/obj/*' \
        -not -path "./${ARTIFACTS_DIR}/*" \
        2>/dev/null \
        | sed 's|^\./||' \
        | LC_ALL=C sort
}

build_graph() {
    mkdir -p "${PLAN_DIR}"

    discover_projects > "${PROJECTS_FILE}"
    [[ -s "${PROJECTS_FILE}" ]] || fail "No projects found in the repository."

    # One awk process for the whole repository rather than a grep and a realpath per reference.
    # xargs keeps the argument list within the platform limit at any project count.
    xargs -a "${PROJECTS_FILE}" -d '\n' awk -f "${BUILD_DIR}/edges.awk" \
        | LC_ALL=C sort -u > "${EDGES_FILE}"
}

cmd_graph() {
    build_graph

    log "Project dependency graph (X <- Y means Y must rebuild when X changes)"
    local project
    while IFS= read -r project; do
        printf '%s\n' "${project}"
        awk -F'\t' -v target="${project}" '$2 == target { printf "    <- %s\n", $1 }' "${EDGES_FILE}"
    done < "${PROJECTS_FILE}"
}

# --------------------------------------------------------------------------- affected ----

# Returns the directory a shared build file governs, or fails when the file is not one.
scoping_directory() {
    local file="$1"
    local base="${file##*/}"
    local directory="${file%/*}"
    [[ "${directory}" == "${file}" ]] && directory="."

    local candidate
    for candidate in "${SCOPING_FILES[@]}"; do
        if [[ "${base}" == "${candidate}" ]]; then
            printf '%s\n' "${directory}"
            return 0
        fi
    done

    # A tool manifest governs the directory holding its .config folder.
    if [[ "${base}" == "dotnet-tools.json" && "${directory##*/}" == ".config" ]]; then
        local parent="${directory%/*}"
        [[ "${parent}" == "${directory}" ]] && parent="."
        printf '%s\n' "${parent}"
        return 0
    fi

    return 1
}

projects_under() {
    local scope="$1"
    if [[ "${scope}" == "." ]]; then
        cat "${PROJECTS_FILE}"
    else
        awk -v prefix="${scope}/" 'index($0, prefix) == 1' "${PROJECTS_FILE}"
    fi
}

# Returns every project that could own a file: those in the nearest ancestor directory holding
# any project file. A file in a directory with two projects belongs to both, so all of them are
# returned rather than one being picked.
owning_projects() {
    local directory
    directory="$(dirname "$1")"

    while [[ "${directory}" != "." && "${directory}" != "/" && -n "${directory}" ]]; do
        local found
        found="$(awk -v prefix="${directory}/" '
            index($0, prefix) == 1 && substr($0, length(prefix) + 1) !~ /\// { print }
        ' "${PROJECTS_FILE}")"

        if [[ -n "${found}" ]]; then
            printf '%s\n' "${found}"
            return 0
        fi
        directory="$(dirname "${directory}")"
    done

    return 1
}

is_pipeline_path() {
    case "$1" in
        "${BUILD_DIR}/"* | .github/workflows/*) return 0 ;;
        # Only the scripts. runner/ holds its README as well, and prose cannot change how
        # anything compiles; matching the whole directory would rebuild the repository to
        # document it.
        "${RUNNER_DIR}/"*.sh) return 0 ;;
        *) return 1 ;;
    esac
}

changed_files() {
    if [[ -n "${CHANGED_FILES:-}" ]]; then
        printf '%s\n' "${CHANGED_FILES}"
        return 0
    fi

    if [[ -n "${BASE_SHA:-}" ]] && git rev-parse --verify --quiet "${BASE_SHA}^{commit}" >/dev/null 2>&1; then
        # Three dots: diff against the merge base, so commits that landed on the target branch
        # after the pull request opened do not look like changes.
        git diff --name-only "${BASE_SHA}...HEAD"
        return 0
    fi

    return 1
}

compute_affected() {
    build_graph

    local files=""
    if ! files="$(changed_files)"; then
        echo "No usable BASE_SHA ('${BASE_SHA:-}'); treating every project as affected." >&2
        cp "${PROJECTS_FILE}" "${AFFECTED_FILE}"
        # The seed list drives both the coverage gate and the shard partition. Nothing is known
        # to be unchanged, so everything is seeded; an empty list would gate nothing.
        cp "${PROJECTS_FILE}" "${SEEDS_FILE}"
        return
    fi

    : > "${SEEDS_FILE}"

    if [[ -z "${files//[[:space:]]/}" ]]; then
        echo "No files changed."
    else
        echo "Changed files:"
        local file
        while IFS= read -r file; do
            [[ -z "${file}" ]] && continue

            local scope=""
            if scope="$(scoping_directory "${file}")"; then
                printf '  %s  (shared: affects %s and below)\n' "${file}" "${scope}"
                projects_under "${scope}" >> "${SEEDS_FILE}"
                continue
            fi

            # A change to the pipeline is not scoped to any subtree: it governs how everything is
            # built, tested and measured, so every project is rebuilt and re-measured under the
            # new rules.
            if is_pipeline_path "${file}"; then
                printf '  %s  (pipeline: affects every project)\n' "${file}"
                projects_under "." >> "${SEEDS_FILE}"
                continue
            fi

            local owners=""
            owners="$(owning_projects "${file}" || true)"
            if [[ -n "${owners}" ]]; then
                printf '  %s  -> %s\n' "${file}" "$(tr '\n' ' ' <<< "${owners}")"
                printf '%s\n' "${owners}" >> "${SEEDS_FILE}"
            else
                printf '  %s  (no project; ignored)\n' "${file}"
            fi
        done <<< "${files}"
    fi

    LC_ALL=C sort -u -o "${SEEDS_FILE}" "${SEEDS_FILE}"

    awk -F'\t' -v edges="${EDGES_FILE}" -v seeds="${SEEDS_FILE}" \
        -f "${BUILD_DIR}/affected.awk" "${EDGES_FILE}" "${SEEDS_FILE}" \
        | LC_ALL=C sort > "${AFFECTED_FILE}"
}

# ----------------------------------------------------------------------- project info ----

# Reads AssemblyName, TargetPath and the coverage policy of every project in one MSBuild
# process. Asking per project costs an SDK start-up each.
#
# The properties come from MSBuild rather than from the XML so that the Directory.Build.props
# defaults and any per-project override resolve as the compiler sees them.
project_info() {
    local configuration="$1"
    local output="$2"
    local projects_file="$3"

    if [[ ! -s "${projects_file}" ]]; then
        : > "${output}"
        return 0
    fi

    dotnet msbuild "${BUILD_DIR}/project-info.proj" \
        -nologo -maxCpuCount \
        -p:CiRepoRoot="${REPO_ROOT_NATIVE}" \
        -p:Configuration="${configuration}" \
        -p:CiProjectsFile="${REPO_ROOT_NATIVE}${projects_file}" \
        -p:CiInfoOutput="${REPO_ROOT_NATIVE}${output}" \
        > /dev/null \
        || fail "Failed to read project information. Re-run without redirecting stdout to see why."

    [[ -s "${output}" ]] || fail "Project information came back empty; ${output} was not written."
}

# Where the configuration-specific project information for the current selection lives. The file
# describes one selection, so it is scoped to the shard: two shards sharing a name would leave
# the second one's file for the first to misread. Only reachable locally, where shards share a
# working directory.
config_info_file() {
    printf '%s\n' "${PLAN_DIR}/info-${1}${CI_SHARD:+-shard-${CI_SHARD}}.tsv"
}

# Column accessor for an info file: relativePath, assemblyName, isTest, excluded, minLine,
# minBranch, targetPath, targetFrameworks.
info_field() {
    local file="$1" project="$2" column="$3"
    awk -F'\t' -v target="${project}" -v column="${column}" \
        '$1 == target { print $column; exit }' "${file}"
}

# ------------------------------------------------------------------------------- plan ----

shard_count() {
    local affected_count="$1"

    if [[ -n "${CI_SHARDS:-}" ]]; then
        printf '%s\n' "${CI_SHARDS}"
        return
    fi

    local count=$(( (affected_count + PROJECTS_PER_SHARD - 1) / PROJECTS_PER_SHARD ))
    (( count < 1 )) && count=1
    (( count > MAX_SHARDS )) && count="${MAX_SHARDS}"
    printf '%s\n' "${count}"
}

emit_plan_outputs() {
    local any="$1" shards="$2"

    if [[ -n "${GITHUB_OUTPUT:-}" ]]; then
        {
            echo "any=${any}"
            echo "shards=${shards}"
        } >> "${GITHUB_OUTPUT}"
    fi

    echo "any=${any}"
    echo "shards=${shards}"
}

cmd_plan() {
    compute_affected

    local affected_count
    affected_count="$(wc -l < "${AFFECTED_FILE}" | tr -d '[:space:]')"

    echo
    if [[ "${affected_count}" -eq 0 ]]; then
        echo "Affected projects: none"
        : > "${INFO_FILE}"
        printf '[]\n' > "${SHARDS_FILE}"
        emit_plan_outputs false "[]"
        return 0
    fi

    echo "Affected projects (${affected_count}):"
    sed 's/^/  /' "${AFFECTED_FILE}"

    # Debug is only named here to give TargetPath a value; the fields the plan consumes --
    # IsTestProject and the coverage policy -- do not vary by configuration.
    log "Reading project information"
    project_info Debug "${INFO_FILE}" "${AFFECTED_FILE}"

    local shards
    shards="$(shard_count "${affected_count}")"

    log "Partitioning ${affected_count} project(s) into at most ${shards} shard(s)"
    # Anything describing the previous selection is stale once the partition moves. 'info-*.tsv'
    # does not match 'info.tsv', which this run has just written.
    rm -f "${PLAN_DIR}"/shard-*.txt "${PLAN_DIR}"/info-*.tsv "${PLAN_DIR}"/gated-*.tsv "${PLAN_DIR}"/measured-*.tsv

    awk -F'\t' -v edges="${EDGES_FILE}" -v affected="${AFFECTED_FILE}" -v info="${INFO_FILE}" \
        -v seeds="${SEEDS_FILE}" -v shards="${shards}" -f "${BUILD_DIR}/shards.awk" \
        "${EDGES_FILE}" "${AFFECTED_FILE}" "${INFO_FILE}" "${SEEDS_FILE}" \
        | while IFS=$'\t' read -r shard project; do
              printf '%s\n' "${project}" >> "${PLAN_DIR}/shard-${shard}.txt"
          done

    local indices=()
    local file
    for file in "${PLAN_DIR}"/shard-*.txt; do
        [[ -e "${file}" ]] || continue
        local name="${file##*/}"
        name="${name#shard-}"
        name="${name%.txt}"
        indices+=("${name}")
        LC_ALL=C sort -o "${file}" "${file}"
        printf '  shard %s: %s project(s)\n' "${name}" "$(wc -l < "${file}" | tr -d '[:space:]')"
    done

    [[ ${#indices[@]} -gt 0 ]] || fail "The partition produced no shards for ${affected_count} affected project(s)."

    local json
    json="[$(printf '%s\n' "${indices[@]}" | LC_ALL=C sort -n | paste -sd, -)]"
    printf '%s\n' "${json}" > "${SHARDS_FILE}"

    emit_plan_outputs true "${json}"
}

# --------------------------------------------------------------------------- selection ----

require_plan() {
    [[ -f "${AFFECTED_FILE}" ]] || fail "No plan found at ${AFFECTED_FILE}. Run 'build/ci.sh plan' first."
    # The gate reads this to decide what changed. A missing file reads as "nothing changed" and
    # gates nothing, while still reporting success.
    [[ -f "${SEEDS_FILE}" ]] || fail "The plan at ${PLAN_DIR} has no ${SEEDS_FILE##*/}. Re-run 'build/ci.sh plan'."
}

# The projects this invocation is responsible for: one shard, or everything when CI_SHARD is
# unset.
selected_file() {
    if [[ -n "${CI_SHARD:-}" ]]; then
        local file="${PLAN_DIR}/shard-${CI_SHARD}.txt"
        [[ -f "${file}" ]] || fail "CI_SHARD=${CI_SHARD} but ${file} does not exist."
        printf '%s\n' "${file}"
    else
        printf '%s\n' "${AFFECTED_FILE}"
    fi
}

selected_test_projects() {
    local selected="$1" info="$2"
    awk -F'\t' -v info="${info}" '
        FILENAME == info { if ($3 == "true") test[$1] = 1; next }
        ($0 in test) { print }
    ' "${info}" "${selected}"
}

# Writes a project that builds the selected projects in a single MSBuild session. One session
# evaluates each shared dependency once and schedules the graph across cores; a loop of
# "dotnet build <project>" re-evaluates the whole reference closure of every project separately.
#
# A plain <Project> with no Sdk attribute, so it does not import Directory.Build.props and cannot
# pick up repository defaults meant for real projects.
write_build_project() {
    local selected="$1" output="$2"

    {
        echo '<Project>'
        echo '    <ItemGroup>'
        local project
        while IFS= read -r project; do
            [[ -z "${project}" ]] && continue
            # Rooted for the same reason as in build/project-info.proj: an item Include resolves
            # against the directory of the file declaring it, which here is artifacts/ci.
            printf '        <CiProject Include="$(CiRepoRoot)%s" />\n' "${project}"
        done < "${selected}"
        echo '    </ItemGroup>'
        # Restore runs serially. Each project restores its own reference closure, and MSBuild's
        # project cache does not dedupe those the way it dedupes Build, so two parallel entry
        # points sharing an upstream project race to write the same obj/ files and one fails with
        # "Cannot create a file when that file already exists".
        echo '    <Target Name="Restore">'
        echo '        <MSBuild Projects="@(CiProject)" Targets="Restore" BuildInParallel="false"'
        echo '                 Properties="Configuration=$(Configuration)" />'
        echo '    </Target>'
        echo '    <Target Name="Build">'
        echo '        <MSBuild Projects="@(CiProject)" Targets="Build" BuildInParallel="true"'
        echo '                 Properties="Configuration=$(Configuration)" />'
        echo '    </Target>'
        echo '</Project>'
    } > "${output}"
}

# ------------------------------------------------------------------------------ verify ----

cmd_verify() {
    build_graph

    log "Verifying the API does not project-reference the libraries"

    # Skipping the API on a library change is only sound while the API depends on published
    # package versions. A project reference would make it a dependent of the libraries, turning
    # every library pull request into an API build.
    local offenders
    offenders="$(awk -F'\t' '
        ($1 ~ /^src\/apps\// || $1 ~ /^tests\/apps\//) && $2 ~ /^src\/libraries\// {
            printf "  %s -> %s\n", $1, $2
        }' "${EDGES_FILE}")"

    if [[ -n "${offenders}" ]]; then
        printf '%s\n' "${offenders}" >&2
        fail "App projects project-reference the libraries. The API is meant to consume them as versioned packages; a project reference makes every library change rebuild and retest the API."
    fi
    echo "OK: no app project references src/libraries."

    log "Verifying no project multi-targets"

    # Every output is located through TargetPath, which an outer multi-targeting build leaves
    # empty. One framework declared in Directory.Build.props lets projects in other languages
    # join the graph without this script knowing anything about frameworks.
    local multi=""
    local project
    while IFS= read -r project; do
        grep -q '<TargetFrameworks>' "${project}" && multi+="  ${project}"$'\n'
    done < "${PROJECTS_FILE}"

    if [[ -n "${multi}" ]]; then
        printf '%s\n' "${multi}" >&2
        fail "These projects declare <TargetFrameworks>. Set a single <TargetFramework>, or change RepositoryTargetFramework in Directory.Build.props."
    fi
    echo "OK: every project targets a single framework."
}

# ------------------------------------------------------------------ restore/build/test ----

cmd_restore() {
    local configuration="${1:-}"
    require_configuration "${configuration}"
    require_plan

    local selected
    selected="$(selected_file)"
    [[ -s "${selected}" ]] || { echo "Nothing selected; skipping restore."; return 0; }

    local project_file="${PLAN_DIR}/build-${configuration}.proj"
    write_build_project "${selected}" "${project_file}"

    log "Restoring $(wc -l < "${selected}" | tr -d '[:space:]') project(s) (${configuration})"

    # Deliberately not RestoreLockedMode. Locked mode only means anything against a committed
    # packages.lock.json, and there is none: every version is declared centrally in
    # Directory.Packages.props, and a change to that file is a scoping change that rebuilds and
    # retests every project in the repository. What a lock file would add on top is protection
    # against a restore resolving differently for the same commit, which NuGet's lowest-applicable
    # rule already rules out for a single fixed feed.
    dotnet msbuild "${project_file}" -t:Restore -nologo -maxCpuCount \
        -p:CiRepoRoot="${REPO_ROOT_NATIVE}" \
        -p:Configuration="${configuration}"
}

cmd_build() {
    local configuration="${1:-}"
    require_configuration "${configuration}"
    require_plan

    local selected
    selected="$(selected_file)"
    [[ -s "${selected}" ]] || { echo "Nothing selected; skipping build."; return 0; }

    local project_file="${PLAN_DIR}/build-${configuration}.proj"
    write_build_project "${selected}" "${project_file}"

    log "Building $(wc -l < "${selected}" | tr -d '[:space:]') project(s) (${configuration})"

    # Upstream dependencies are compiled by MSBuild as inputs even though they are not selected;
    # only their tests are skipped.
    dotnet msbuild "${project_file}" -t:Build -nologo -maxCpuCount \
        -p:CiRepoRoot="${REPO_ROOT_NATIVE}" \
        -p:Configuration="${configuration}"
}

cmd_test() {
    local configuration="${1:-}"
    require_configuration "${configuration}"
    require_plan

    local selected
    selected="$(selected_file)"
    [[ -s "${selected}" ]] || { echo "Nothing selected; nothing to test."; return 0; }

    local info
    info="$(config_info_file "${configuration}")"
    project_info "${configuration}" "${info}" "${selected}"

    local test_projects
    test_projects="$(selected_test_projects "${selected}" "${info}")"
    if [[ -z "${test_projects}" ]]; then
        echo "No selected test projects; nothing to run."
        return 0
    fi

    local coverage_dir="${ARTIFACTS_DIR}/coverage/${configuration}${CI_SHARD:+/shard-${CI_SHARD}}"
    rm -rf "${coverage_dir}"
    mkdir -p "${coverage_dir}"

    log "Testing $(wc -l <<< "${test_projects}") test project(s) (${configuration})"

    local failed="" empty=""
    local project
    while IFS= read -r project; do
        [[ -z "${project}" ]] && continue

        local name assembly
        name="$(basename "${project}")"
        name="${name%.*}"

        # TargetPath comes from MSBuild, locating the output whatever the framework, language or
        # output layout. It is reported with the platform separator; the shell's file tests need
        # forward slashes.
        assembly="$(info_field "${info}" "${project}" 7 | tr '\\' '/')"
        [[ -n "${assembly}" ]] || fail "MSBuild reported no TargetPath for ${project}."
        [[ -f "${assembly}" ]] || fail "Test assembly not found: ${assembly}. Run 'build' first."

        echo "--- ${name}"
        # The test projects run on Microsoft.Testing.Platform. 'dotnet test' does not forward
        # extension arguments to the test app, so --coverlet would reach nothing and produce no
        # coverage; the assembly is executed directly instead.
        local status=0
        dotnet exec "${assembly}" \
            --coverlet \
            --coverlet-output-format cobertura \
            --coverlet-file-prefix "${name}" \
            --results-directory "${coverage_dir}" || status=$?

        case "${status}" in
            0) ;;
            # Microsoft.Testing.Platform returns 8 when a run discovers no tests and 5 when a
            # filter selects none. Neither is a failing assertion, and a suite scaffolded ahead
            # of its tests is a normal transient state. The coverage gate reports the same
            # condition with a better message.
            5|8) empty+="  ${name}"$'\n' ;;
            *)   failed+="  ${name} (exit ${status})"$'\n' ;;
        esac
    done <<< "${test_projects}"

    if [[ -n "${empty}" ]]; then
        warn "Test projects that ran no tests:"$'\n'"${empty}"
    fi

    if [[ -n "${failed}" ]]; then
        printf 'Test projects with failing tests:\n%s\n' "${failed}" >&2
        summary ""
        summary "**${configuration} shard ${CI_SHARD:-all}: failing test projects**"
        summary ""
        printf '%s' "${failed}" | while IFS= read -r line; do
            [[ -z "${line}" ]] && continue
            summary "- \`${line# }\`"
        done
        fail "Tests did not pass (${configuration})."
    fi

    echo "All selected tests passed (${configuration})."
}

# ---------------------------------------------------------------------- coverage-gate ----

cmd_coverage_gate() {
    local configuration="${1:-}"
    require_configuration "${configuration}"
    require_plan

    local selected
    selected="$(selected_file)"
    [[ -s "${selected}" ]] || { echo "Nothing selected; nothing to gate."; return 0; }

    log "Reading the coverage policy of each selected project"

    # Only the projects the pull request changed are judged. A dependent is rebuilt and retested
    # because its inputs moved, but neither its code nor its tests changed, so re-measuring it
    # would drag every test that touches it into one shard. See the header of build/shards.awk:
    # the partition depends on this staying narrow.
    #
    # Each surviving entry is assembly<TAB>minimumLine<TAB>minimumBranch. The policy was resolved
    # once by 'plan'; this is a table lookup, not an MSBuild run.
    local gated="${PLAN_DIR}/gated-${configuration}.tsv"
    : > "${gated}"
    awk -F'\t' -v info="${INFO_FILE}" -v seeds="${SEEDS_FILE}" -v gatedfile="${gated}" '
        FILENAME == info {
            assembly[$1] = $2; excluded[$1] = $4; line[$1] = $5; branch[$1] = $6
            next
        }
        FILENAME == seeds {
            changed[$0] = 1
            next
        }
        {
            if (!($0 in assembly) || assembly[$0] == "") {
                printf "  ?    %-52s could not read its coverage policy\n", $0 > "/dev/stderr"
                bad = 1
                next
            }
            if (!($0 in changed)) {
                printf "  keep %-52s unchanged; rebuilt but not re-measured\n", assembly[$0]
                next
            }
            if (tolower(excluded[$0]) == "true") {
                printf "  skip %-52s ExcludeFromCoverage=true\n", assembly[$0]
                next
            }
            printf "  gate %-52s line >= %s%%  branch >= %s%%\n", assembly[$0], line[$0], branch[$0]
            print assembly[$0] "\t" line[$0] "\t" branch[$0] > gatedfile
        }
        END { if (bad) exit 1 }
    ' "${INFO_FILE}" "${SEEDS_FILE}" "${selected}" \
        || fail "Failed to read the coverage policy of a selected project."

    if [[ ! -s "${gated}" ]]; then
        echo "No selected project is under the coverage gate; nothing to check."
        return 0
    fi

    local info
    info="$(config_info_file "${configuration}")"
    [[ -s "${info}" ]] || project_info "${configuration}" "${info}" "${selected}"

    # A project with no test project would otherwise fail further down with a complaint about
    # missing coverage files. The shard partition guarantees the tests covering it are in this
    # shard, so their absence is a real gap rather than a sharding artefact.
    if [[ -z "$(selected_test_projects "${selected}" "${info}")" ]]; then
        printf '::error::Nothing tests these selected projects: %s\n' "$(cut -f1 "${gated}" | tr '\n' ' ')" >&2
        fail "A project under the coverage gate must be tested by something. Add a test project with a ProjectReference to it, or set <ExcludeFromCoverage>true</ExcludeFromCoverage> in the project."
    fi

    local coverage_dir="${ARTIFACTS_DIR}/coverage/${configuration}${CI_SHARD:+/shard-${CI_SHARD}}"
    local report_dir="${ARTIFACTS_DIR}/coverage-report/${configuration}${CI_SHARD:+/shard-${CI_SHARD}}"

    shopt -s nullglob
    local reports=("${coverage_dir}"/*cobertura*.xml)
    shopt -u nullglob
    [[ ${#reports[@]} -gt 0 ]] || fail "No coverage reports under ${coverage_dir}. Did 'test' run?"

    rm -rf "${report_dir}"

    # ReportGenerator merges the per-project reports. Each test project exercises only part of a
    # library, and coverlet writes the source path differently depending on which project
    # produced the report, so a merge keyed on file path double counts every line.
    #
    # Only this shard's reports are merged. A repository-wide merge is the memory ceiling of the
    # job once there are hundreds of test projects, and a shard already holds every test project
    # covering anything it gates.
    #
    # The HTML report is the expensive part of ReportGenerator and is read only when a gate
    # fails, so CI produces it on demand and a local run gets it by default.
    local report_types="Cobertura;TextSummary"
    if [[ "${CI_COVERAGE_HTML:-${GITHUB_ACTIONS:+0}}" != "0" ]]; then
        report_types="${report_types};Html"
    fi

    local generator_output
    if ! generator_output="$(dotnet reportgenerator \
        "-reports:${coverage_dir}/*cobertura*.xml" \
        "-targetdir:${report_dir}" \
        "-reporttypes:${report_types}" 2>&1)"; then
        # The output is captured to keep the log readable, so it must be replayed on failure.
        printf '%s\n' "${generator_output}" >&2
        fail "ReportGenerator failed to merge the coverage reports."
    fi

    local merged="${report_dir}/Cobertura.xml"
    [[ -f "${merged}" ]] || fail "ReportGenerator produced no merged report at ${merged}."

    # The merged report is read once into a table. Grepping it per gated assembly is quadratic in
    # the number of assemblies, against a file that grows with all of them.
    local measured="${PLAN_DIR}/measured-${configuration}.tsv"
    awk '
        /<package / {
            name = ""; line_rate = ""; branch_rate = ""
            if (match($0, /name="[^"]*"/))        name        = substr($0, RSTART + 6,  RLENGTH - 7)
            if (match($0, /line-rate="[^"]*"/))   line_rate   = substr($0, RSTART + 11, RLENGTH - 12)
            if (match($0, /branch-rate="[^"]*"/)) branch_rate = substr($0, RSTART + 13, RLENGTH - 14)
            if (name != "")
                printf "%s\t%.2f\t%.2f\n", name, line_rate * 100, branch_rate * 100
        }
    ' "${merged}" > "${measured}"

    if ! awk -F'\t' -v measured="${measured}" '
        FILENAME == measured { line[$1] = $2; branch[$1] = $3; next }
        {
            if (!($1 in line)) {
                printf "::error::%s is under the coverage gate but no test exercised it, so it produced no coverage data.\n", $1 > "/dev/stderr"
                failures++
                next
            }
            if (line[$1] + 0 >= $2 + 0 && branch[$1] + 0 >= $3 + 0) {
                printf "  OK   %-52s line %6.2f%%  branch %6.2f%%\n", $1, line[$1], branch[$1]
                next
            }
            printf "::error::%s is below its coverage gate: line %.2f%% (min %s%%), branch %.2f%% (min %s%%)\n", $1, line[$1], $2, branch[$1], $3 > "/dev/stderr"
            failures++
        }
        END { if (failures) exit 1 }
    ' "${measured}" "${gated}"; then
        summary ""
        summary "**${configuration} shard ${CI_SHARD:-all}: below the coverage gate**"
        summary ""
        summary "| Assembly | Line | Min | Branch | Min |"
        summary "| --- | --- | --- | --- | --- |"
        awk -F'\t' -v measured="${measured}" '
            FILENAME == measured { line[$1] = $2; branch[$1] = $3; next }
            {
                if (!($1 in line)) { printf "| `%s` | none | %s%% | none | %s%% |\n", $1, $2, $3; next }
                if (line[$1] + 0 >= $2 + 0 && branch[$1] + 0 >= $3 + 0) next
                printf "| `%s` | %.2f%% | %s%% | %.2f%% | %s%% |\n", $1, line[$1], $2, branch[$1], $3
            }
        ' "${measured}" "${gated}" | while IFS= read -r line; do summary "${line}"; done

        echo "Open ${report_dir}/index.html to see which lines are uncovered; re-run with CI_COVERAGE_HTML=1 if it is missing." >&2
        exit 1
    fi

    echo "Every gated project meets its coverage policy."
}

# -------------------------------------------------------------------------- summarise ----

# Renders one shard's outcome into the job summary. Takes phase=outcome pairs as GitHub reports
# them, where a phase after a failed one reads 'skipped', distinguishing "the build broke, so
# nothing was tested" from "the tests ran and failed".
#
# Every shard writes its section whatever happened, so one page shows which stage and shard
# failed rather than a checks page of up to sixteen job names.
cmd_summarise() {
    local stage="${CI_STAGE:-Debug}"
    local shard="${CI_SHARD:-all}"

    local status="passed"
    local pair
    for pair in "$@"; do
        case "${pair#*=}" in
            success|skipped) ;;
            *) status="FAILED" ;;
        esac
    done

    summary ""
    summary "### ${stage} — shard ${shard}: ${status}"
    summary ""
    summary "| Phase | Result |"
    summary "| --- | --- |"
    for pair in "$@"; do
        summary "| ${pair%%=*} | ${pair#*=} |"
    done

    local selected="${PLAN_DIR}/shard-${shard}.txt"
    [[ "${shard}" == "all" ]] && selected="${AFFECTED_FILE}"
    if [[ -s "${selected}" ]]; then
        summary ""
        summary "<details><summary>$(wc -l < "${selected}" | tr -d '[:space:]') project(s) in this shard</summary>"
        summary ""
        sed 's|^|- `|; s|$|`|' "${selected}" | while IFS= read -r line; do summary "${line}"; done
        summary ""
        summary "</details>"
    fi
}

# ------------------------------------------------------------------------------- main ----

case "${1:-}" in
    plan)             shift; cmd_plan "$@" ;;
    summarise)        shift; cmd_summarise "$@" ;;
    graph)            shift; cmd_graph "$@" ;;
    verify)           shift; cmd_verify "$@" ;;
    verify-isolation) shift; cmd_verify "$@" ;;
    restore)          shift; cmd_restore "$@" ;;
    build)            shift; cmd_build "$@" ;;
    test)             shift; cmd_test "$@" ;;
    coverage-gate)    shift; cmd_coverage_gate "$@" ;;
    *)
        # The usage block only: from the first comment line to the section headings below it.
        sed -n '2,${/^# Selection/q;p;}' "${BASH_SOURCE[0]}" | sed 's/^# \?//'
        exit 1
        ;;
esac
