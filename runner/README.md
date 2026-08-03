# Self-hosted runner: approaches

Notes on how CI runs on our own hardware, what is implemented today, and the alternative that was
built first and then replaced. Written so the decision can be revisited.

## What we need from it

1. Set it up once. Every later pull request runs without anyone touching the machine.
2. Survive reboots.
3. Builds are isolated from each other and from the host.
4. NuGet packages are cached between runs, or every job pays a full restore.
5. The environment a job builds in matches what `docker/ci-local.sh` gives locally, so "works on
   my machine" and "works in CI" mean the same thing.

Point 3 and point 1 pull against each other, and that tension is the whole story below.

---

## Approach A — persistent agent, containerised work (implemented)

The GitHub Actions agent is installed on the host as a systemd service and stays registered. It
compiles nothing itself. Each workflow step calls `runner/in-container.sh`, which starts a
container, runs one `build/ci.sh` command in it, and throws the container away.

```
Ubuntu host
│
├── actions agent  (systemd, persistent, registered once)
│     │
│     └── step:  bash runner/in-container.sh test Debug
│                       │  docker run --rm --user <uid>
│                       ▼
│               ┌──────────────────────────────┐
│               │ container — fresh, discarded │
│               │   bash build/ci.sh test Debug│
│               └──────────────────────────────┘
│                   ▲                    ▲
│                   │ mount /repo        │ mount /cache
├── _work/<repo> ───┘                    │
│   (checkout, rewritten each job)       │
└── volume bitenovac-runner-nuget ───────┘
    (packages, survives everything)
```

### Files

| File | Role |
|---|---|
| `runner/setup-runner.sh` | One-shot host provisioning |
| `runner/in-container.sh` | Starts the container for one `build/ci.sh` command |
| `docker/ci-runner.Dockerfile` | The image both CI and `docker/ci-local.sh` use |

### Setup

```
sudo runner/setup-runner.sh --token <registration token>
```

The token comes from **Settings → Actions → Runners → New self-hosted runner**. It is valid for
one hour and is spent once: `config.sh` exchanges it for credentials in `.credentials` (an RSA
key pair plus an OAuth token) which the agent refreshes on its own. **No token or PAT is needed
again.** That is the property that makes this "set up once".

The script also installs Docker, creates the `ci-runner` account, builds the image, creates the
package volume and chowns it to that account.

### What persists and what does not

| | Between jobs |
|---|---|
| Agent process, registration | persists |
| `_work` checkout | persists on disk, overwritten by each `actions/checkout` |
| Build environment (SDK, tools, `/tmp`, installed packages) | destroyed with each container |
| NuGet packages | persist in the named volume |

### Consequences

- The workflow has no `setup-dotnet`, no `actions/cache` and no `dotnet tool restore` step: the
  SDK is in the image, packages are on the volume, and `in-container.sh` restores tools inside
  the container.
- `runs-on: [self-hosted, linux, x64, bitenovac]` in `.github/workflows/pr.yml`.
- The image is built at setup time, not per job. **Re-run `setup-runner.sh` after changing
  `global.json` or `docker/ci-runner.Dockerfile`**, or jobs keep using the old SDK.
- Containers run as the agent's uid so the next `actions/checkout` can delete what they leave in
  the workspace.

### Honest limits

- Isolation covers the build, not the machine. The agent process and the checkout live on the
  host between jobs. A job that wants to persist something can write outside the workspace.
- `ci-runner` is in the `docker` group, which is root-equivalent on that host: anything able to
  start a container can bind-mount `/` into one.
- Concurrency is one job at a time unless more agents are installed. The `debug` and `release`
  stages will therefore serialise.

---

## Approach B — ephemeral containerised agent (built first, replaced)

The agent itself ran inside the container. It registered with `--ephemeral`, accepted exactly one
job, and exited. systemd (`Restart=always`) started a replacement, which registered again.

```
Ubuntu host
│
├── systemd: bitenovac-runner@1, @2  (Restart=always)
│     │
│     └── docker run --rm <agent image>
│               │
│               ▼
│         ┌────────────────────────────────────────┐
│         │ container = agent AND build environment│
│         │  1. mint registration token from PAT   │
│         │  2. config.sh --ephemeral              │
│         │  3. run.sh  → one job → exit           │
│         └────────────────────────────────────────┘
│                              │ container dies, systemd restarts it
└── volume: /root/.nuget/packages  (survives)
```

### Why it needed a PAT

A registration token lives one hour. Re-registering on **every job** therefore cannot use a
stored token — something has to mint a fresh one each time. That means a Personal Access Token
with administration rights on the repository, stored at `/etc/bitenovac-runner/env` (mode 0600),
and a `POST /repos/{owner}/{repo}/actions/runners/registration-token` call in the entrypoint
before each registration.

This is the trade that killed it: it converts "set up once" into "hold a long-lived
repo-admin credential on the box forever". The PAT is strictly more powerful than the
registration token it replaces, and it does not expire on its own.

### What it bought

- Genuinely pristine machine per job. No agent state, no `_work` carried over, nothing a previous
  job could leave behind except the package volume.
- Runner count is a systemd instance count (`bitenovac-runner@1..N`), so concurrency is trivial
  to scale.
- The GitHub-recommended shape for untrusted workloads. If this repo ever goes public and accepts
  fork pull requests, ephemeral runners are the baseline expectation, not an optimisation.

### What it cost

- The PAT, as above.
- An extra image (`agent.Dockerfile`) layered on the CI image, plus its own entrypoint, so two
  images to keep in step instead of one.
- A container start and a full re-registration per job (a few seconds each).
- Deregistration handling: a killed container leaves an offline runner entry, so `--replace` and
  a cleanup trap were needed.

The files were deleted in the switch. Recovering them means re-adding `runner/agent.Dockerfile`
and `runner/entrypoint.sh` and pointing the systemd unit at the agent image; the workflow itself
needs no change, except that `in-container.sh` becomes unnecessary because the job already runs
inside a container.

---

## Two alternatives worth reading about before deciding

**GitHub's native `container:` job key.** Declare `container: image: ...` on a job and the runner
executes every step inside it — no wrapper script. Cleaner than `in-container.sh`. The catch is
image distribution: the runner pulls the image, so a locally built tag is not enough and we would
need a registry (GHCR) plus credentials. Worth it if the image ever needs to be shared across
several machines.

**actions-runner-controller (ARC).** The Kubernetes operator for ephemeral runners, and the
direction GitHub itself pushes for fleets. It authenticates with a **GitHub App** rather than a
PAT, which fixes Approach B's main flaw: short-lived installation tokens, scoped permissions,
revocable independently of any user account. Overkill for one machine, correct for many. If we
ever want ephemeral runners without a standing PAT, a GitHub App is the answer — with or without
Kubernetes.

---

## Comparison

| | A — persistent agent | B — ephemeral agent |
|---|---|---|
| Credential at rest | none after setup | PAT with repo admin rights |
| Setup input | one registration token | one PAT |
| Isolation | build only | whole machine per job |
| Concurrency | one per installed agent | systemd instance count |
| Images to maintain | 1 | 2 |
| Per-job overhead | container start | container start + registration |
| Cache | named volume | named volume |
| Suitable for public repo / fork PRs | no | closer, still not sufficient alone |

---

## Security, independent of approach

**If the repository is public, neither approach is safe as configured.** `.github/workflows/pr.yml`
triggers on `pull_request`, which on a public repo means an arbitrary fork's code executes on our
hardware. A container is not a security boundary against someone trying to escape it, and the
agent's `docker` group membership is a direct path to root on the host. Options if it goes
public: require approval for first-time contributors, move to `pull_request_target` with explicit
gating, restrict CI to `merge_group`, or run the box as disposable infrastructure.

**The `docker` group is root-equivalent.** True in both approaches, and in the previous project's
setup script too. It is inherent to letting CI start containers.

---

## To do

**Add `runner/` to the pipeline paths in `build/ci.sh`.** `is_pipeline_path` currently matches
only `build/` and `.github/workflows/`, so a pull request touching just this directory selects
nothing:

```
$ CHANGED_FILES="runner/in-container.sh" bash build/ci.sh plan
Changed files:
  runner/in-container.sh  (no project; ignored)
Affected projects: none
any=false
```

`in-container.sh` now sits between the workflow and every build, so a subtle change to it — wrong
mount, missing environment variable, wrong user — can merge green having tested nothing. A gross
breakage still fails loudly, because the `plan` step itself calls it.

The fix is one condition:

```bash
is_pipeline_path() {
    [[ "$1" == "${BUILD_DIR}/"* || "$1" == .github/workflows/* || "$1" == runner/* ]]
}
```

Note the cost: like any pipeline path, this marks **every** project affected, so editing this
README would trigger a full-repository build. Excluding `*.md` from the rule, or scoping it to
`runner/*.sh`, avoids that.

## Status

Approach A is implemented and partially verified. Confirmed on this machine: the image builds,
and `plan`, `verify`, `restore`, `build`, `test` and `coverage-gate` all run correctly as a
non-root user in separate containers sharing one cache volume (which reached 315 MB / 46
packages). Not yet verified, because it needs the actual host: `config.sh` registration,
`svc.sh install`, and a real job driven by GitHub.

Also unverified and worth watching on the first run: `timeout-minutes: 1` on each job was sized
for GitHub-hosted runners with a warm `actions/cache`. The volume starts empty, so the first
restore may well exceed it.
