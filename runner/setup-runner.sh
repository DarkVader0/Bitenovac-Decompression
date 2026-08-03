#!/usr/bin/env bash
#
# Provisions this machine as a self-hosted CI runner for the repository. Run once, with root
# rights; every later pull request runs here without further input.
#
# Usage:
#   sudo runner/setup-runner.sh --token <registration token>
#   sudo runner/setup-runner.sh --uninstall --token <registration token>
#
# Options:
#   --token TOKEN      Runner registration token. Take it from
#                      Settings > Actions > Runners > New self-hosted runner on the repository.
#                      Valid for one hour and needed only here: config.sh exchanges it for
#                      long-lived credentials that the agent refreshes itself, so the runner
#                      keeps working indefinitely without another token.
#   --repo URL         Repository to serve. Defaults to the origin remote of this checkout.
#   --labels LIST      Comma separated labels the workflow selects on.
#                      Defaults to self-hosted,linux,x64,bitenovac.
#   --user NAME        Account the agent runs as. Created if missing. Defaults to ci-runner.
#   --runner-version V Agent version. Defaults to the latest release.
#   --uninstall        Stop the service, deregister the runner and remove its files. Needs
#                      --token as well, since deregistering asks GitHub too.
#   --purge            With --uninstall, also delete the cached packages.
#
# Installs three things: the agent as a systemd service, the image jobs build in, and the volume
# their packages are cached in. The image is built here rather than per job, so re-run this after
# changing global.json or docker/ci-runner.Dockerfile.

set -euo pipefail

readonly REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
readonly RUNNER_HOME="/opt/bitenovac-runner"
readonly AGENT_DIR="${RUNNER_HOME}/actions-runner"
readonly NUGET_VOLUME="bitenovac-runner-nuget"

log()  { printf '\n\033[1m==> %s\033[0m\n' "$*"; }
ok()   { printf '  \033[32mok\033[0m    %s\n' "$*"; }
warn() { printf '  \033[33mwarn\033[0m  %s\n' "$*"; }
fail() { printf '\n\033[31merror:\033[0m %s\n' "$*" >&2; exit 1; }

token=""
repo_url=""
labels="self-hosted,linux,x64,bitenovac"
run_as="ci-runner"
runner_version=""
uninstall=false
purge=false

while [[ $# -gt 0 ]]; do
    case "$1" in
        --token)          token="${2:-}";          shift 2 ;;
        --repo)           repo_url="${2:-}";       shift 2 ;;
        --labels)         labels="${2:-}";         shift 2 ;;
        --user)           run_as="${2:-}";         shift 2 ;;
        --runner-version) runner_version="${2:-}"; shift 2 ;;
        --uninstall)      uninstall=true;          shift ;;
        --purge)          purge=true;              shift ;;
        -h|--help)        sed -n '2,32p' "${BASH_SOURCE[0]}" | sed 's/^# \?//'; exit 0 ;;
        *)                fail "Unknown argument: $1" ;;
    esac
done

[[ "${EUID}" -eq 0 ]] || fail "Run this with sudo. It creates a user and installs a systemd service."
command -v systemctl > /dev/null 2>&1 || fail "systemd is required; the agent is installed as a service."

# --------------------------------------------------------------------------------- uninstall ----

if [[ "${uninstall}" == true ]]; then
    [[ -n "${token}" ]] || fail "--uninstall needs --token as well, to deregister the runner with GitHub."

    log "Removing the runner"

    if [[ -f "${AGENT_DIR}/svc.sh" ]]; then
        (cd "${AGENT_DIR}" && ./svc.sh stop > /dev/null 2>&1 || true)
        (cd "${AGENT_DIR}" && ./svc.sh uninstall > /dev/null 2>&1 || true)
        ok "stopped and uninstalled the service"
    fi

    if [[ -f "${AGENT_DIR}/.runner" ]]; then
        (cd "${AGENT_DIR}" && sudo -u "${run_as}" ./config.sh remove --token "${token}" > /dev/null 2>&1 || true)
        ok "deregistered from the repository"
    fi

    rm -rf "${RUNNER_HOME}"
    ok "removed ${RUNNER_HOME}"

    if [[ "${purge}" == true ]]; then
        docker volume rm "${NUGET_VOLUME}" > /dev/null 2>&1 || true
        ok "deleted the package cache volume"
    else
        warn "kept the package cache volume ${NUGET_VOLUME}; pass --purge to delete it"
    fi

    log "Uninstalled."
    exit 0
fi

# ------------------------------------------------------------------------------------ inputs ----

[[ -n "${token}" ]] || fail "--token is required. Get one from Settings > Actions > Runners > New self-hosted runner."

if [[ -z "${repo_url}" ]]; then
    repo_url="$(git -C "${REPO_ROOT}" remote get-url origin 2>/dev/null | sed -E 's|^git@github\.com:|https://github.com/|; s|\.git$||')" || true
fi
[[ -n "${repo_url}" ]] || fail "Could not determine the repository. Pass --repo https://github.com/OWNER/NAME."

case "$(uname -m)" in
    x86_64)  runner_arch="x64" ;;
    aarch64) runner_arch="arm64" ;;
    *)       fail "Unsupported architecture $(uname -m). The agent ships for x64 and arm64." ;;
esac

# ------------------------------------------------------------------------------------ docker ----

log "Docker"

apt-get update -qq
apt-get install --yes --no-install-recommends curl git tar ca-certificates > /dev/null

if command -v docker > /dev/null 2>&1; then
    ok "$(docker --version)"
else
    warn "docker is not installed; installing from the official repository"

    install -m 0755 -d /etc/apt/keyrings
    curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
    chmod a+r /etc/apt/keyrings/docker.asc
    printf 'deb [arch=%s signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/ubuntu %s stable\n' \
        "$(dpkg --print-architecture)" "$(. /etc/os-release && echo "${VERSION_CODENAME}")" \
        > /etc/apt/sources.list.d/docker.list
    apt-get update -qq
    apt-get install --yes docker-ce docker-ce-cli containerd.io > /dev/null
    ok "installed $(docker --version)"
fi

systemctl enable --now docker > /dev/null 2>&1 || true
docker info > /dev/null 2>&1 || fail "The Docker daemon is not running."

# -------------------------------------------------------------------------------------- user ----

log "Account"

if id "${run_as}" > /dev/null 2>&1; then
    ok "${run_as} exists"
else
    useradd --create-home --shell /bin/bash "${run_as}"
    ok "created ${run_as}"
fi

# The agent starts the job containers, so it needs the Docker socket. This is root-equivalent on
# this host: anything that can start a container can mount the filesystem into one.
usermod --append --groups docker "${run_as}"
ok "${run_as} is in the docker group"

readonly RUN_UID="$(id -u "${run_as}")"
readonly RUN_GID="$(id -g "${run_as}")"

# ------------------------------------------------------------------------------------- image ----

sdk_version="$(sed -n 's/.*"version"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' "${REPO_ROOT}/global.json" | head -n 1)"
[[ -n "${sdk_version}" ]] || fail "Could not read the SDK version from ${REPO_ROOT}/global.json."

log "Building bitenovac-ci-runner:${sdk_version} (.NET SDK ${sdk_version})"
docker build \
    --file "${REPO_ROOT}/docker/ci-runner.Dockerfile" \
    --build-arg "DOTNET_SDK_VERSION=${sdk_version}" \
    --tag "bitenovac-ci-runner:${sdk_version}" \
    "${REPO_ROOT}/docker"

docker volume create "${NUGET_VOLUME}" > /dev/null
# A new volume is owned by root, and the job containers run as the agent's user. Without this
# they cannot write to the cache and every restore downloads everything again.
docker run --rm --user 0 --entrypoint chown \
    --volume "${NUGET_VOLUME}:/cache" \
    "bitenovac-ci-runner:${sdk_version}" -R "${RUN_UID}:${RUN_GID}" /cache
ok "package cache volume ${NUGET_VOLUME}, owned by ${run_as}"

# ------------------------------------------------------------------------------------- agent ----

log "Agent"

# A previous installation is removed rather than reconfigured: config.sh refuses to reconfigure a
# registered runner, and a half-updated directory is harder to reason about than a fresh one.
if [[ -d "${AGENT_DIR}" ]]; then
    warn "an existing installation was found; replacing it"
    [[ -f "${AGENT_DIR}/svc.sh" ]] && (cd "${AGENT_DIR}" && ./svc.sh stop > /dev/null 2>&1 || true)
    [[ -f "${AGENT_DIR}/svc.sh" ]] && (cd "${AGENT_DIR}" && ./svc.sh uninstall > /dev/null 2>&1 || true)
    [[ -f "${AGENT_DIR}/.runner" ]] && (cd "${AGENT_DIR}" && sudo -u "${run_as}" ./config.sh remove --token "${token}" > /dev/null 2>&1 || true)
    rm -rf "${AGENT_DIR}"
fi

install -d -o "${run_as}" -g "${run_as}" "${RUNNER_HOME}" "${AGENT_DIR}"

if [[ -z "${runner_version}" ]]; then
    runner_version="$(curl -fsSL https://api.github.com/repos/actions/runner/releases/latest \
        | sed -n 's/.*"tag_name"[[:space:]]*:[[:space:]]*"v\([^"]*\)".*/\1/p' | head -n 1)"
    [[ -n "${runner_version}" ]] || fail "Could not resolve the latest agent release. Pass --runner-version."
fi

log "Installing agent ${runner_version}"
curl -fsSL -o /tmp/actions-runner.tar.gz \
    "https://github.com/actions/runner/releases/download/v${runner_version}/actions-runner-linux-${runner_arch}-${runner_version}.tar.gz"
tar -xzf /tmp/actions-runner.tar.gz -C "${AGENT_DIR}"
rm -f /tmp/actions-runner.tar.gz
chown -R "${run_as}:${run_as}" "${AGENT_DIR}"

"${AGENT_DIR}/bin/installdependencies.sh" > /dev/null
ok "agent dependencies installed"

# The token is spent here. What config.sh writes into .credentials is long lived and refreshed by
# the agent, so no token is needed again for the runner to keep accepting jobs.
log "Registering with ${repo_url}"
sudo -u "${run_as}" "${AGENT_DIR}/config.sh" \
    --url "${repo_url}" \
    --token "${token}" \
    --name "$(hostname)" \
    --labels "${labels}" \
    --work "${AGENT_DIR}/_work" \
    --unattended \
    --replace

(cd "${AGENT_DIR}" && ./svc.sh install "${run_as}" > /dev/null && ./svc.sh start > /dev/null)
ok "service installed and started"

# ------------------------------------------------------------------------------------- ready ----

log "Ready"
cat <<EOF
  Serving ${repo_url} as $(hostname), labelled: ${labels}

  The workflow selects it with:  runs-on: [${labels//,/, }]

  systemctl status 'actions.runner.*'    Service state
  journalctl -u 'actions.runner.*' -f    Follow the agent
  docker ps                              The container holding the current step

  Jobs build in throwaway containers; only ${NUGET_VOLUME} persists. Re-run this script after
  changing global.json or docker/ci-runner.Dockerfile, since the image is built here.
EOF
