<#
.SYNOPSIS
    Release script for Arcmark Windows.

.DESCRIPTION
    Builds the app, creates a Velopack installer, and uploads to GitHub Releases.

.PARAMETER Version
    The version to release (e.g., "0.2.0"). If not specified, reads from VERSION file.

.PARAMETER DryRun
    Build and pack only — don't push to GitHub.

.PARAMETER GitHubToken
    GitHub personal access token. Can also be set via GITHUB_TOKEN env var.

.EXAMPLE
    # Full release
    .\scripts\release.ps1 -GitHubToken "ghp_..."

    # Dry run (build + pack only)
    .\scripts\release.ps1 -DryRun

    # Release specific version
    .\scripts\release.ps1 -Version "0.2.0" -GitHubToken "ghp_..."
#>
param(
    [string]$Version = "",
    [switch]$DryRun,
    [string]$GitHubToken = ""
)

$ErrorActionPreference = "Stop"
$Root = Split-Path $PSScriptRoot -Parent
$RepoUrl = "https://github.com/Geek-1001/arcmark-windows"
$ReleasesDir = "$Root\releases"

# Resolve version
if ($Version -eq "") {
    $Version = (Get-Content "$Root\VERSION" -Raw).Trim()
}

# Resolve GitHub token
if ($GitHubToken -eq "" -and $env:GITHUB_TOKEN) {
    $GitHubToken = $env:GITHUB_TOKEN
}

Write-Host ""
Write-Host "╔══════════════════════════════════════════╗" -ForegroundColor Magenta
Write-Host "║  Arcmark Windows Release — v$Version        ║" -ForegroundColor Magenta
Write-Host "╚══════════════════════════════════════════╝" -ForegroundColor Magenta
Write-Host ""

# ── Step 1: Update VERSION file ──────────────────────────────────────
Write-Host "[1/4] Setting version to $Version..." -ForegroundColor Yellow
$Version | Set-Content "$Root\VERSION" -NoNewline
Write-Host "      Done." -ForegroundColor Green

# ── Step 2: Build + Pack ─────────────────────────────────────────────
Write-Host "[2/4] Building and packing..." -ForegroundColor Yellow
& "$PSScriptRoot\build.ps1" -Pack

# ── Step 3: Verify output ────────────────────────────────────────────
Write-Host "[3/4] Verifying release artifacts..." -ForegroundColor Yellow
$setupExe = Get-ChildItem "$ReleasesDir\*Setup*" -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $setupExe) {
    Write-Host "      ERROR: No Setup.exe found in releases/" -ForegroundColor Red
    exit 1
}
$size = [math]::Round($setupExe.Length / 1MB, 1)
Write-Host "      Found: $($setupExe.Name) ($size MB)" -ForegroundColor Green

# ── Step 4: Upload to GitHub ─────────────────────────────────────────
if ($DryRun) {
    Write-Host "[4/4] Dry run — skipping GitHub upload." -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "To upload manually, run:" -ForegroundColor Cyan
    Write-Host "  vpk upload github --repoUrl `"$RepoUrl`" --tag `"v$Version`" --outputDir `"$ReleasesDir`" --token YOUR_TOKEN" -ForegroundColor White
} else {
    if ($GitHubToken -eq "") {
        Write-Host "      ERROR: No GitHub token provided. Use -GitHubToken or set GITHUB_TOKEN env var." -ForegroundColor Red
        exit 1
    }

    Write-Host "[4/4] Uploading to GitHub Releases..." -ForegroundColor Yellow
    vpk upload github `
        --repoUrl $RepoUrl `
        --tag "v$Version" `
        --outputDir $ReleasesDir `
        --token $GitHubToken

    Write-Host "      ✓ Released v$Version to GitHub!" -ForegroundColor Green
    Write-Host "      $RepoUrl/releases/tag/v$Version" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "Release complete!" -ForegroundColor Green
Write-Host ""
