#!/usr/bin/env bash
#
# Builds the runner image from this checkout, with the CloudBuild tool published into it. Pull
# requests run the tool this image holds, so re-run this after changing the tool, global.json or
# docker/ci-runner.Dockerfile.
#
# Usage:
#   bash runner/build-image.sh [docker build options...]

set -euo pipefail

readonly REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

sdk_version="$(sed -n 's/.*"version"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' "${REPO_ROOT}/global.json" | head -n 1)"
[[ -n "${sdk_version}" ]] || { printf 'error: could not read the SDK version from %s/global.json.\n' "${REPO_ROOT}" >&2; exit 1; }

exec docker build \
    --file "${REPO_ROOT}/docker/ci-runner.Dockerfile" \
    --build-arg "DOTNET_SDK_VERSION=${sdk_version}" \
    --tag "bitenovac-cloudbuild-runner:${sdk_version}" \
    "$@" \
    "${REPO_ROOT}"
