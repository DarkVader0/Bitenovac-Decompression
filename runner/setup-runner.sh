#!/usr/bin/env bash
#
# Provisions this machine as a self-hosted CI runner for the repository. Run once, with root
# rights; every later pull request runs here without further input.
#
# Usage:
#   sudo runner/setup-runner.sh --token <registration token>
#   sudo runner/setup-runner.sh --token <registration token> --instances 4
#   sudo runner/setup-runner.sh --uninstall --token <registration token>
#
# Options:
#   --token TOKEN      Runner registration token. Take it from
#                      Settings > Actions > Runners > New self-hosted runner on the repository.
#                      Valid for one hour and needed only here: config.sh exchanges it for
#                      long-lived credentials that the agent refreshes itself, so the runner
#                      keeps working indefinitely without another token. One token registers
#                      every instance, provided they are all configured within that hour.
#   --instances N      How many agents to install. An agent accepts one job at a time, so this is
#                      how many workflow jobs the machine runs at once; at 1 the Debug and Release
#                      stages of a pull request serialise. Defaults to 1. See "Sizing" below.
#   --repo URL         Repository to serve. Defaults to the origin remote of this checkout.
#   --labels LIST      Comma separated labels the workflow selects on.
#                      Defaults to self-hosted,linux,x64,bitenovac.
#   --user NAME        Account the agents run as. Created if missing. Defaults to ci-runner.
#   --runner-version V Agent version. Defaults to the latest release.
#   --uninstall        Stop the services, deregister every agent and remove its files. Needs
#                      --token as well, since deregistering asks GitHub too.
#   --purge            With --uninstall, also delete the cached packages.
#
# Installs three things: the agents as systemd services, the image jobs build in, and the volume
# their packages are cached in. The image is built here rather than per job, so re-run this after
# changing global.json or docker/ci-runner.Dockerfile.
#
# Sizing --instances
# ------------------
# One pull request runs plan, then verify plus a Debug and a Release job per shard, then the PR
# aggregator. Peak demand is therefore (2 x shards) + 1, and 'build/ci.sh plan' prints the shard
# count for a given change.
#
# Every agent shares this machine's cores, so past that point they only contend. MSBuild claims
# the whole box by default: set CI_MAX_CPU to roughly cores / instances in the agent environment
# and build/ci.sh bounds each job to that instead.

set -euo pipefail

readonly REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
readonly RUNNER_HOME="/opt/bitenovac-runner"
readonly NUGET_VOLUME="bitenovac-runner-nuget"

# Every agent lives in its own directory under RUNNER_HOME. The glob is what uninstall and
# reinstall sweep, so it must match every layout this script has ever produced.
readonly AGENT_DIR_GLOB="actions-runner*"

agent_dir_for() { printf '%s\n' "${RUNNER_HOME}/actions-runner-${1}"; }
runner_name_for() { printf '%s\n' "$(hostname)-${1}"; }

existing_agent_dirs() {
    shopt -s nullglob
    printf '%s\n' "${RUNNER_HOME}"/${AGENT_DIR_GLOB}
    shopt -u nullglob
}

# Stops the service, deregisters with GitHub and deletes the directory. Every step tolerates a
# half-installed agent, since that is exactly the state a failed run leaves behind.
remove_agent() {
    local dir="$1"
    [[ -d "${dir}" ]] || return 0

    if [[ -f "${dir}/svc.sh" ]]; then
        (cd "${dir}" && ./svc.sh stop > /dev/null 2>&1 || true)
        (cd "${dir}" && ./svc.sh uninstall > /dev/null 2>&1 || true)
    fi

    if [[ -f "${dir}/.runner" ]]; then
        (cd "${dir}" && sudo -u "${run_as}" ./config.sh remove --token "${token}" > /dev/null 2>&1 || true)
    fi

    rm -rf "${dir}"
    ok "removed ${dir##*/}"
}

log()  { printf '\n\033[1m==> %s\033[0m\n' "$*"; }
ok()   { printf '  \033[32mok\033[0m    %s\n' "$*"; }
warn() { printf '  \033[33mwarn\033[0m  %s\n' "$*"; }
fail() { printf '\n\033[31merror:\033[0m %s\n' "$*" >&2; exit 1; }

token=""
repo_url=""
labels="self-hosted,linux,x64,bitenovac"
run_as="ci-runner"
runner_version=""
instances=1
uninstall=false
purge=false

while [[ $# -gt 0 ]]; do
    case "$1" in
        --token)          token="${2:-}";          shift 2 ;;
        --repo)           repo_url="${2:-}";       shift 2 ;;
        --labels)         labels="${2:-}";         shift 2 ;;
        --user)           run_as="${2:-}";         shift 2 ;;
        --runner-version) runner_version="${2:-}"; shift 2 ;;
        --instances)      instances="${2:-}";      shift 2 ;;
        --uninstall)      uninstall=true;          shift ;;
        --purge)          purge=true;              shift ;;
        -h|--help)        sed -n '2,42p' "${BASH_SOURCE[0]}" | sed 's/^# \?//'; exit 0 ;;
        *)                fail "Unknown argument: $1" ;;
    esac
done

[[ "${EUID}" -eq 0 ]] || fail "Run this with sudo. It creates a user and installs a systemd service."
command -v systemctl > /dev/null 2>&1 || fail "systemd is required; the agent is installed as a service."

# --------------------------------------------------------------------------------- uninstall ----

if [[ "${uninstall}" == true ]]; then
    [[ -n "${token}" ]] || fail "--uninstall needs --token as well, to deregister the runners with GitHub."

    log "Removing the runners"

    found=false
    while IFS= read -r dir; do
        [[ -n "${dir}" ]] || continue
        found=true
        remove_agent "${dir}"
    done < <(existing_agent_dirs)

    [[ "${found}" == true ]] || warn "no agent directory found under ${RUNNER_HOME}"

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

[[ "${instances}" =~ ^[1-9][0-9]*$ ]] || fail "--instances must be a positive integer, got '${instances}'."

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

host_cores="$(nproc)"

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

# Every previous installation is removed rather than reconfigured: config.sh refuses to
# reconfigure a registered runner, and a half-updated directory is harder to reason about than a
# fresh one. Sweeping the whole glob also retires instances that a larger --instances left behind,
# and the single unsuffixed directory that earlier versions of this script produced.
while IFS= read -r dir; do
    [[ -n "${dir}" ]] || continue
    warn "an existing installation was found in ${dir##*/}; replacing it"
    remove_agent "${dir}"
done < <(existing_agent_dirs)

install -d -o "${run_as}" -g "${run_as}" "${RUNNER_HOME}"

if [[ -z "${runner_version}" ]]; then
    runner_version="$(curl -fsSL https://api.github.com/repos/actions/runner/releases/latest \
        | sed -n 's/.*"tag_name"[[:space:]]*:[[:space:]]*"v\([^"]*\)".*/\1/p' | head -n 1)"
    [[ -n "${runner_version}" ]] || fail "Could not resolve the latest agent release. Pass --runner-version."
fi

# Downloaded once and unpacked into each instance, rather than once per instance.
log "Installing ${instances} agent(s), version ${runner_version}"
curl -fsSL -o /tmp/actions-runner.tar.gz \
    "https://github.com/actions/runner/releases/download/v${runner_version}/actions-runner-linux-${runner_arch}-${runner_version}.tar.gz"

# System packages, so once for the machine rather than once per agent.
dependencies_installed=false

for (( instance = 1; instance <= instances; instance++ )); do
    agent_dir="$(agent_dir_for "${instance}")"
    runner_name="$(runner_name_for "${instance}")"

    install -d -o "${run_as}" -g "${run_as}" "${agent_dir}"
    tar -xzf /tmp/actions-runner.tar.gz -C "${agent_dir}"

    # The agent exports this file's contents into every job, and runner/in-container.sh forwards
    # it into the container. Deriving the share here rather than naming it in the workflow keeps a
    # core count that is only true of this machine out of the repository.
    cpu_share=$(( host_cores / instances ))
    (( cpu_share < 1 )) && cpu_share=1
    printf 'CI_MAX_CPU=%s\n' "${cpu_share}" > "${agent_dir}/.env"

    chown -R "${run_as}:${run_as}" "${agent_dir}"

    if [[ "${dependencies_installed}" == false ]]; then
        "${agent_dir}/bin/installdependencies.sh" > /dev/null
        ok "agent dependencies installed"
        dependencies_installed=true
    fi

    # The token is not spent by this: it stays valid for its hour and registers every instance.
    # What config.sh writes into .credentials is long lived and refreshed by the agent, so no
    # token is needed again for the runner to keep accepting jobs.
    log "Registering ${runner_name} with ${repo_url}"
    sudo -u "${run_as}" "${agent_dir}/config.sh" \
        --url "${repo_url}" \
        --token "${token}" \
        --name "${runner_name}" \
        --labels "${labels}" \
        --work "${agent_dir}/_work" \
        --unattended \
        --replace

    # Each runner name yields its own systemd unit, so the instances start and stop independently.
    (cd "${agent_dir}" && ./svc.sh install "${run_as}" > /dev/null && ./svc.sh start > /dev/null)
    ok "${runner_name}: service installed and started"
done

rm -f /tmp/actions-runner.tar.gz

# ------------------------------------------------------------------------------------- ready ----

log "Ready"
cat <<EOF
  Serving ${repo_url} with ${instances} agent(s), labelled: ${labels}
  $(for (( i = 1; i <= instances; i++ )); do printf '%s ' "$(runner_name_for "${i}")"; done)

  The workflow selects them with:  runs-on: [${labels//,/, }]

  ${instances} job(s) run at once. A pull request needs (2 x shard count) + 1 to reach full
  parallelism; 'build/ci.sh plan' prints the shard count.

  systemctl status 'actions.runner.*'    Service state, one unit per agent
  journalctl -u 'actions.runner.*' -f    Follow every agent
  docker ps                              The containers holding the current steps

  Jobs build in throwaway containers; only ${NUGET_VOLUME} persists. Re-run this script after
  changing global.json or docker/ci-runner.Dockerfile, since the image is built here.
EOF
