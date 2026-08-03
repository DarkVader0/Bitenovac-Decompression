<#
.SYNOPSIS
    Local development bootstrap for Windows.

.DESCRIPTION
    Installs and verifies everything needed to build, test and run the pipeline locally, and is
    safe to re-run: every step is a no-op once satisfied. The PowerShell twin of ./init.sh, which
    is the one to use on Linux, macOS or under Git Bash.

    What it does:

      1. Checks the shell utilities build/ci.sh depends on. They are not installable from here --
         they ship with Git for Windows -- so a missing one is reported as a missing Git install.
      2. Ensures a .NET SDK that satisfies global.json, installing one under
         $env:USERPROFILE\.dotnet if the machine has none. The version is never chosen here: it
         is read from global.json, which is the same file CI hands to actions/setup-dotnet.
      3. Restores the local tools from .config\dotnet-tools.json (reportgenerator, which the
         coverage gate runs).
      4. Restores packages for every project.

.PARAMETER Check
    Report what is missing and exit non-zero; change nothing.

.PARAMETER NoRestore
    Install the toolchain but skip the package restore.

.EXAMPLE
    .\init.ps1

.EXAMPLE
    .\init.ps1 -Check
#>

[CmdletBinding()]
param(
    [switch] $Check,
    [switch] $NoRestore
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = $PSScriptRoot
$dotnetInstallUrl = 'https://dot.net/v1/dotnet-install.ps1'
$dotnetUserDir = Join-Path $env:USERPROFILE '.dotnet'

function Write-Log  { param([string] $Message) Write-Host "`n==> $Message" -ForegroundColor White }
function Write-Ok   { param([string] $Message) Write-Host '  ok    ' -ForegroundColor Green -NoNewline; Write-Host $Message }
function Write-Warn { param([string] $Message) Write-Host '  warn  ' -ForegroundColor Yellow -NoNewline; Write-Host $Message }
function Write-Miss { param([string] $Message) Write-Host '  miss  ' -ForegroundColor Red -NoNewline; Write-Host $Message }

function Stop-WithError
{
    param([string] $Message)

    Write-Host "`nerror: $Message" -ForegroundColor Red
    exit 1
}

function Test-Command
{
    param([string] $Name)

    return $null -ne (Get-Command $Name -ErrorAction SilentlyContinue)
}

# 'dotnet --version' resolves through global.json and fails when no installed SDK satisfies it,
# which is exactly the question being asked. Comparing version strings here would only
# re-implement the roll-forward rules, and get them subtly wrong.
function Test-SdkSatisfiesGlobalJson
{
    if (-not (Test-Command 'dotnet'))
    {
        return $false
    }

    & dotnet --version *> $null
    return $LASTEXITCODE -eq 0
}

Push-Location $repoRoot
try
{
    $missing = $false

    # -------------------------------------------------------------------- shell utilities ----

    Write-Log 'Shell utilities'

    # build/ci.sh and docker/ci-local.sh are bash scripts driving awk. On Windows every one of
    # these comes from Git for Windows, so they are checked together and reported as one install.
    $gitBash = $null
    foreach ($candidate in @(
        (Join-Path $env:ProgramFiles 'Git\bin\bash.exe'),
        (Join-Path ${env:ProgramFiles(x86)} 'Git\bin\bash.exe'),
        (Join-Path $env:LOCALAPPDATA 'Programs\Git\bin\bash.exe')))
    {
        if ($candidate -and (Test-Path $candidate))
        {
            $gitBash = $candidate
            break
        }
    }

    if (-not $gitBash -and (Test-Command 'bash'))
    {
        $gitBash = (Get-Command 'bash').Source
    }

    if ($gitBash)
    {
        Write-Ok "bash ($gitBash)"

        foreach ($utility in @('git', 'awk', 'curl', 'tar', 'sed', 'grep', 'find', 'sort'))
        {
            & $gitBash -lc "command -v $utility" *> $null
            if ($LASTEXITCODE -eq 0)
            {
                Write-Ok $utility
            }
            else
            {
                Write-Miss "$utility is not available inside bash"
                $missing = $true
            }
        }
    }
    else
    {
        Write-Miss 'bash was not found'
        Write-Warn 'Install Git for Windows (winget install --id Git.Git) -- it provides bash, awk and the rest of the utilities build/ci.sh runs on.'
        $missing = $true
    }

    # --------------------------------------------------------------------------- .NET SDK ----

    Write-Log '.NET SDK'

    # The exact version global.json pins.
    $requiredSdk = (Get-Content 'global.json' -Raw | ConvertFrom-Json).sdk.version
    if ([string]::IsNullOrWhiteSpace($requiredSdk))
    {
        Stop-WithError 'Could not read the SDK version from global.json.'
    }

    if (Test-SdkSatisfiesGlobalJson)
    {
        Write-Ok "$(& dotnet --version) satisfies global.json (pinned $requiredSdk)"
    }
    elseif ($Check)
    {
        Write-Miss "no installed SDK satisfies global.json (pinned $requiredSdk)"
        $missing = $true
    }
    else
    {
        Write-Warn "no installed SDK satisfies global.json (pinned $requiredSdk); installing it under $dotnetUserDir"

        $installer = Join-Path ([System.IO.Path]::GetTempPath()) 'dotnet-install.ps1'
        try
        {
            Invoke-WebRequest -Uri $dotnetInstallUrl -OutFile $installer -UseBasicParsing
        }
        catch
        {
            Stop-WithError "Could not download the .NET installer from $dotnetInstallUrl. $($_.Exception.Message)"
        }

        # A per-user install needs no elevation and cannot disturb a machine-wide SDK.
        & $installer -Version $requiredSdk -InstallDir $dotnetUserDir -NoPath
        if ($LASTEXITCODE -ne 0)
        {
            Stop-WithError 'The .NET installer failed.'
        }
        Remove-Item $installer -Force -ErrorAction SilentlyContinue

        $env:PATH = "$dotnetUserDir;$env:PATH"
        $env:DOTNET_ROOT = $dotnetUserDir

        if (-not (Test-SdkSatisfiesGlobalJson))
        {
            Stop-WithError "Installed $requiredSdk into $dotnetUserDir but it still does not satisfy global.json."
        }

        Write-Ok "installed $(& dotnet --version)"
        Write-Warn "Add $dotnetUserDir to PATH and set DOTNET_ROOT to it permanently; this script only sets them for its own run."
    }
    
    # ------------------------------------------------------------------------------- check ----

    if ($Check)
    {
        if ($missing)
        {
            Stop-WithError 'Some prerequisites are missing. Re-run without -Check to install them.'
        }

        Write-Log 'Everything required is present.'
        exit 0
    }

    if ($missing)
    {
        Stop-WithError 'Some prerequisites could not be installed from here. Resolve the items marked "miss" above and re-run.'
    }

    # ----------------------------------------------------------------------------- restore ----

    Write-Log 'Local tools'
    & dotnet tool restore
    if ($LASTEXITCODE -ne 0)
    {
        Stop-WithError 'dotnet tool restore failed.'
    }
    Write-Ok 'restored .config\dotnet-tools.json'

    if ($NoRestore)
    {
        Write-Log 'Skipping the package restore (-NoRestore).'
    }
    else
    {
        Write-Log 'Packages'
        # The whole solution, not the affected set: this is a first-run bootstrap, so it warms the
        # package cache for every project rather than for whichever ones a plan happens to select.
        & dotnet restore 'Bitenovac-Decompression.slnx'
        if ($LASTEXITCODE -ne 0)
        {
            Stop-WithError 'dotnet restore failed.'
        }
        Write-Ok 'restored packages'
    }

    Write-Log 'Ready'
    @'
  dotnet build Bitenovac-Decompression.slnx
  dotnet test .\tests\libraries\Bitenovac.DecompressionAlgorithms.Core.Unit.Tests\Bitenovac.DecompressionAlgorithms.Core.Unit.Tests.csproj

  bash build/ci.sh graph              What depends on what
  bash build/ci.sh plan               What a pull request from HEAD would run
'@ | Write-Host
}
finally
{
    Pop-Location
}
