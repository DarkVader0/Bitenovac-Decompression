#!/usr/bin/env bash
#
# Local development bootstrap. Installs and verifies everything needed to build, test and run
# the pipeline locally, and is safe to re-run: every step is a no-op once satisfied.
#
# Usage:
#   ./init.sh                Install what is missing, then restore
#   ./init.sh --check        Report what is missing and exit non-zero; change nothing
#   ./init.sh --no-restore   Install the toolchain, skip the package restore
#
# What it does
# ------------
#   1. Checks the shell utilities the runner scripts depend on (git, curl, tar).
#   2. Ensures a .NET SDK that satisfies global.json, installing one under ~/.dotnet if the
#      machine has none. The version is never chosen here: it is read from global.json, which
#      is the same file CI hands to actions/setup-dotnet.
#   3. Restores the local tools from .config/dotnet-tools.json (reportgenerator, which the
#      coverage gate runs).
#   4. Restores packages for every project.

set -euo pipefail

readonly REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

readonly DOTNET_INSTALL_URL="https://dot.net/v1/dotnet-install.sh"
readonly DOTNET_USER_DIR="${HOME}/.dotnet"

check_only=false
restore=true

for argument in "$@"; do
    case "${argument}" in
        --check)      check_only=true ;;
        --no-restore) restore=false ;;
        -h|--help)    sed -n '2,30p' "${BASH_SOURCE[0]}" | sed 's/^# \?//'; exit 0 ;;
        *)            printf 'Unknown argument: %s\n' "${argument}" >&2; exit 2 ;;
    esac
done

cd "${REPO_ROOT}"

log()  { printf '\n\033[1m==> %s\033[0m\n' "$*"; }
ok()   { printf '  \033[32mok\033[0m    %s\n' "$*"; }
warn() { printf '  \033[33mwarn\033[0m  %s\n' "$*"; }
bad()  { printf '  \033[31mmiss\033[0m  %s\n' "$*"; }
fail() { printf '\n\033[31merror:\033[0m %s\n' "$*" >&2; exit 1; }

missing=0

# ------------------------------------------------------------------------ shell utilities ----

log "Shell utilities"

# runner/ and docker/ are written in these. They are always present on Linux and macOS; on
# Windows they come with Git for Windows, so a missing one means this is not running under Git
# Bash. awk is no longer among them: the pipeline logic it used to drive now lives in
# src/tools/Bitenovac.Ci, which needs only the SDK checked for below.
for utility in git curl tar sed grep find sort; do
    if command -v "${utility}" > /dev/null 2>&1; then
        ok "${utility}"
    else
        bad "${utility} is not on PATH"
        missing=1
    fi
done

if [[ "${missing}" -ne 0 ]]; then
    fail "The pipeline scripts need the utilities above. On Windows run this from Git Bash, which ships all of them."
fi

# ------------------------------------------------------------------------------- .NET SDK ----

# The exact version global.json pins. Read with sed rather than a JSON parser to avoid a
# dependency the repository does not already have.
sdk_version() {
    sed -n 's/.*"version"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' global.json | head -n 1
}

# 'dotnet --version' resolves through global.json and fails when no installed SDK satisfies it.
# Comparing version strings here would re-implement the roll-forward rules.
sdk_satisfies_global_json() {
    command -v dotnet > /dev/null 2>&1 && dotnet --version > /dev/null 2>&1
}

log ".NET SDK"

readonly REQUIRED_SDK="$(sdk_version)"
[[ -n "${REQUIRED_SDK}" ]] || fail "Could not read the SDK version from global.json."

if sdk_satisfies_global_json; then
    ok "$(dotnet --version) satisfies global.json (pinned ${REQUIRED_SDK})"
elif [[ "${check_only}" == true ]]; then
    bad "no installed SDK satisfies global.json (pinned ${REQUIRED_SDK})"
    missing=1
else
    warn "no installed SDK satisfies global.json (pinned ${REQUIRED_SDK}); installing it under ${DOTNET_USER_DIR}"

    installer="$(mktemp)"
    curl -fsSL "${DOTNET_INSTALL_URL}" -o "${installer}" \
        || fail "Could not download the .NET installer from ${DOTNET_INSTALL_URL}."

    # A per-user install needs no elevation and cannot disturb a machine-wide SDK.
    bash "${installer}" --version "${REQUIRED_SDK}" --install-dir "${DOTNET_USER_DIR}" --no-path \
        || fail "The .NET installer failed."
    rm -f "${installer}"

    export PATH="${DOTNET_USER_DIR}:${PATH}"
    export DOTNET_ROOT="${DOTNET_USER_DIR}"

    sdk_satisfies_global_json \
        || fail "Installed ${REQUIRED_SDK} into ${DOTNET_USER_DIR} but it still does not satisfy global.json."

    ok "installed $(dotnet --version)"
    warn "Add ${DOTNET_USER_DIR} to PATH and set DOTNET_ROOT to it in your shell profile; this script only exports them for its own run."
fi

# ----------------------------------------------------------------------------------- check ----

if [[ "${check_only}" == true ]]; then
    if [[ "${missing}" -ne 0 ]]; then
        fail "Some prerequisites are missing. Re-run without --check to install them."
    fi
    log "Everything required is present."
    exit 0
fi

# --------------------------------------------------------------------------------- restore ----

log "Local tools"
dotnet tool restore
ok "restored .config/dotnet-tools.json"

if [[ "${restore}" == false ]]; then
    log "Skipping the package restore (--no-restore)."
else
    log "Packages"
    # 'plan' restores every discovered project before it hashes anything, so this warms the
    # package cache for the whole repository and smoke-tests the CI tool in one step. There is
    # no solution file to restore instead — the tool discovers projects from the filesystem.
    dotnet run --project src/tools/Bitenovac.Ci -- plan
    ok "restored packages"
fi

log "Ready"
cat <<'EOF'
  dotnet run --project src/tools/Bitenovac.Ci -- graph    What depends on what
  dotnet run --project src/tools/Bitenovac.Ci -- plan     What this working tree would build

  docker/ci-local.sh                  The whole pipeline, in the runner image
  docker/ci-local.sh --cacheless      The same, ignoring the local cache

  dotnet test ./tests/libraries/Bitenovac.DecompressionAlgorithms.Core.Unit.Tests/Bitenovac.DecompressionAlgorithms.Core.Unit.Tests.csproj
EOF
