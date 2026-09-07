#requires -Version 5.1
<#
.SYNOPSIS
  Supervisor for SicBoLive's three dev-time services (backend, frontend, LiveKit).

.DESCRIPTION
  All three processes in this project are unmanaged (no systemd/IIS/pm2 equivalent on this
  Windows dev box) and have been observed - twice in one session - going down silently and
  simultaneously with no crash dialog or log entry explaining why. This script polls each
  service's TCP port every $IntervalSeconds and relaunches any service that isn't listening,
  using the exact same launch commands already established in CLAUDE.md (working directory,
  CET env vars, log redirection).

  Run it once in the foreground to babysit an interactive session:
      powershell -ExecutionPolicy Bypass -File tools\watchdog.ps1

  Or launch it detached so it keeps running after you close the terminal (run from the repo's
  tools\ directory, or pass an absolute path to this file):
      Start-Process powershell -ArgumentList '-ExecutionPolicy Bypass -NoProfile -File "<repo-root>\tools\watchdog.ps1"' -WindowStyle Hidden

  Ctrl+C stops it (foreground mode). In detached mode, stop it via:
      Get-Process powershell | Where-Object { $_.MainWindowTitle -eq '' } # or just find it in Task Manager and end it -
  Simpler: it writes its own PID to tools\watchdog.pid on start; kill that PID to stop it.

.NOTES
  This script only starts/restarts the three known dev processes with their known-safe launch
  commands. It never touches ports/processes it doesn't recognize, and it never modifies files.
#>

param(
    [int]$IntervalSeconds = 15
)

$ErrorActionPreference = "Stop"

# Resolved from the script's own on-disk location at runtime (not parsed out of a string
# literal in this file), so it's immune to the Windows PowerShell 5.1 "no BOM -> script read
# as ANSI -> Persian path characters in string literals get mangled" gotcha that broke the
# first version of this script (hardcoded "D:\...سیکبو" literal -> DirectoryNotFoundException
# on every Start-Process call).
$toolsDir = $PSScriptRoot
$root = Split-Path -Parent $toolsDir
$backendDir = Join-Path $root "src\Presentation\SicBoLive.WebApi"
$clientDir = Join-Path $root "client"
$livekitDir = Join-Path $toolsDir "livekit-server"
$livekitExe = Join-Path $livekitDir "livekit-server.exe"
$logDir = Join-Path $toolsDir "watchdog-logs"

New-Item -ItemType Directory -Force -Path $logDir | Out-Null

$pidFile = Join-Path $root "tools\watchdog.pid"
$PID | Out-File -FilePath $pidFile -Encoding ascii -Force

function Write-Log([string]$message) {
    $line = "[{0}] {1}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"), $message
    Write-Host $line
    Add-Content -Path (Join-Path $logDir "watchdog.log") -Value $line
}

function Test-PortListening([int]$port) {
    # Get-NetTCPConnection returns a bare scalar object (not a 1-element array) when exactly one
    # match exists, and a scalar has no .Count property in Windows PowerShell 5.1 - "$conns.Count"
    # silently evaluates to $null there, and "$null -gt 0" is $false, so a perfectly healthy
    # single-listener port used to be misreported as down every single poll. Wrapping in @(...)
    # forces array semantics unconditionally so .Count is always meaningful.
    $conns = @(Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue)
    return $conns.Count -gt 0
}

function Start-Backend {
    Write-Log "Backend (5299) is down - relaunching..."
    Start-Process -FilePath "powershell" `
        -ArgumentList @(
            "-NoProfile", "-ExecutionPolicy", "Bypass", "-Command",
            "cd '$backendDir'; `$env:DOTNET_EnableCET='0'; `$env:DOTNET_EnableWriteXorExecute='0'; `$env:COMPlus_EnableCET='0'; `$env:ASPNETCORE_URLS='http://localhost:5299'; `$env:ASPNETCORE_ENVIRONMENT='Development'; dotnet exec bin/Debug/net9.0/SicBoLive.WebApi.dll"
        ) `
        -WorkingDirectory $backendDir `
        -RedirectStandardOutput (Join-Path $logDir "backend-out.log") `
        -RedirectStandardError (Join-Path $logDir "backend-err.log") `
        -WindowStyle Hidden
}

function Start-Frontend {
    Write-Log "Frontend (5173) is down - relaunching..."
    Start-Process -FilePath "powershell" `
        -ArgumentList @(
            "-NoProfile", "-ExecutionPolicy", "Bypass", "-Command",
            "cd '$clientDir'; npm run dev"
        ) `
        -WorkingDirectory $clientDir `
        -RedirectStandardOutput (Join-Path $logDir "frontend-out.log") `
        -RedirectStandardError (Join-Path $logDir "frontend-err.log") `
        -WindowStyle Hidden
}

function Start-LiveKit {
    Write-Log "LiveKit (7880) is down - relaunching..."
    Start-Process -FilePath $livekitExe `
        -ArgumentList '--dev --keys "sicbolive: BvEIHLQwo7qsS1FMZtkhpPb3YcDVm56rNU92XKTW"' `
        -WorkingDirectory $livekitDir `
        -RedirectStandardOutput (Join-Path $logDir "livekit-out.log") `
        -RedirectStandardError (Join-Path $logDir "livekit-err.log") `
        -WindowStyle Hidden
}

Write-Log "Watchdog started (PID $PID). Polling every $IntervalSeconds s. Ports: backend=5299 frontend=5173 livekit=7880."

try {
    while ($true) {
        if (-not (Test-PortListening 5299)) { Start-Backend }
        if (-not (Test-PortListening 5173)) { Start-Frontend }
        if (-not (Test-PortListening 7880)) { Start-LiveKit }
        Start-Sleep -Seconds $IntervalSeconds
    }
}
finally {
    Write-Log "Watchdog stopping."
    Remove-Item -Path $pidFile -Force -ErrorAction SilentlyContinue
}
