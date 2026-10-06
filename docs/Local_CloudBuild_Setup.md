# Local CloudBuild setup

Runs the GitHub pull request pipeline on your own PC, inside WSL.

What you end up with:

- 2 GitHub runners in Ubuntu (WSL), each job limited to 6 CPUs and 6 GB RAM
- the CloudBuild tool already built into the runner image
- the build cache already filled from `master`

Ubuntu commands go in the **Ubuntu** terminal. Windows commands go in **PowerShell**.

---

## First time only: prepare Ubuntu

Skip this section if Ubuntu is already installed and set up.

**1. Install Ubuntu** (PowerShell)

```powershell
wsl --install -d Ubuntu
```

**2. Turn on systemd** (Ubuntu)

```bash
printf '[boot]\nsystemd=true\n' | sudo tee /etc/wsl.conf
```

Then restart WSL (PowerShell) and open Ubuntu again:

```powershell
wsl --shutdown
```

**3. If you have Docker Desktop**, go to Docker Desktop → Settings → Resources → WSL Integration
and **untick Ubuntu**. Setup installs its own Docker inside Ubuntu.

---

## Setup

**1. Get the code** (Ubuntu)

First time:

```bash
git clone https://github.com/DarkVader0/Bitenovac-Decompression.git ~/bitenovac
```

Every time after that:

```bash
cd ~/bitenovac
git switch master
git pull
```

**2. Get a token**

Open https://github.com/DarkVader0/Bitenovac-Decompression/settings/actions/runners/new
and copy the value after `--token`.

- It works for 1 hour.

**3. Run setup** (Ubuntu)

```bash
cd ~/bitenovac
sudo bash runner/setup-runner.sh --token <TOKEN> --instances 2 --cpus 6 --memory 6g
```

Takes a few minutes. It must end with:

```
Promoted 38 entries into main.
  ok    main is warm
==> Ready
```

**4. Check it** (Ubuntu)

```bash
systemctl list-units 'actions.runner.*' --no-pager
```

Both runners must be `active running`.

On GitHub, Settings → Actions → Runners must show `DESKTOP-...-1` and `DESKTOP-...-2` as **Idle**.

**5. Done.** Open a pull request into `master` and it runs on your PC.

---

## Every day

- **The Ubuntu window must be running.** After a reboot, open Ubuntu once and the runners start
  by themselves. While WSL is off, pull requests wait in "Queued".
- **Watch the runners work** (Ubuntu): `journalctl -u 'actions.runner.*' -f`

---

## After changing the CloudBuild tool, Dockerfile or `global.json`

Pull requests use the tool **inside the runner image**, not the one in the pull request. After
such a change is merged into `master` (Ubuntu):

```bash
cd ~/bitenovac
git switch master
git pull
bash runner/build-image.sh
sudo bash runner/seed-cache.sh
```

`seed-cache.sh` is needed because a new tool empties the cache: every project would build from
zero on the next pull request.

---

## Fill the cache again

Every hour the **Official** workflow fills the cache by itself: it builds, tests and
promotes what changed. Run this only when pull requests are slower than they should be (Ubuntu):

```bash
cd ~/bitenovac
git switch master
git pull
sudo bash runner/seed-cache.sh
```

It must end with `Promoted 38 entries into main.`

---

## Remove everything

**1.** On GitHub, Settings → Actions → Runners: remove both runners.

**2.** Ubuntu:

```bash
cd ~/bitenovac
sudo bash runner/setup-runner.sh --uninstall --purge --token unused
docker image rm bitenovac-cloudbuild-runner:10.0.400
```

Use the SDK version from `global.json` in the image name.

**3.** Check that nothing is left:

```bash
systemctl list-units 'actions.runner.*' --no-pager   # 0 loaded units
docker volume ls --filter 'name=bitenovac-'          # empty
docker image ls | grep bitenovac                     # nothing
```

To set it up again, start from [Setup](#setup).

---