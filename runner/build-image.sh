#!/usr/bin/env bash
#
# Builds the runner image, with the RemoteBuildTool release pinned in docker/ci-runner.Dockerfile
# installed into it. Pull requests run the RemoteBuildTool this image holds, so re-run this after
# bumping that pin, changing global.json or anything else in docker/ci-runner.Dockerfile.
#
# Usage:
#   bash runner/build-image.sh [docker build options...]
#
# Environment:
#   REMOTEBUILDTOOL_VERSION   Install this release instead of the pinned one.
#   REMOTEBUILDTOOL_SOURCE    NuGet feed to install it from. Defaults to nuget.org.

set -euo pipefail

readonly REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

sdk_version="$(sed -n 's/.*"version"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' "${REPO_ROOT}/global.json" | head -n 1)"
[[ -n "${sdk_version}" ]] || { printf 'error: could not read the SDK version from %s/global.json.\n' "${REPO_ROOT}" >&2; exit 1; }

build_args=(--build-arg "DOTNET_SDK_VERSION=${sdk_version}")
[[ -n "${REMOTEBUILDTOOL_VERSION:-}" ]] && build_args+=(--build-arg "REMOTEBUILDTOOL_VERSION=${REMOTEBUILDTOOL_VERSION}")
[[ -n "${REMOTEBUILDTOOL_SOURCE:-}" ]] && build_args+=(--build-arg "REMOTEBUILDTOOL_SOURCE=${REMOTEBUILDTOOL_SOURCE}")

exec docker build \
    --file "${REPO_ROOT}/docker/ci-runner.Dockerfile" \
    "${build_args[@]}" \
    --tag "bitenovac-remotebuildtool-runner:${sdk_version}" \
    "$@" \
    "${REPO_ROOT}"
