<#
.SYNOPSIS
    Build script for Arcmark Windows.

.DESCRIPTION
    Builds, tests, publishes, and optionally creates a Velopack installer.

.PARAMETER Release
    Build in Release configuration (default is Debug).

.PARAMETER Publish
    Publish a self-contained single-file executable.

.PARAMETER Pack
    Create a Velopack installer (Setup.exe) for distribution.
    Implies -Release and -Publish.

.PARAMETER SignParams
    Optional code signing parameters for Velopack.
    Example: "/fd SHA256 /tr http://timestamp.digicert.com /td SHA256"

.EXAMPLE
    # Quick debug build + test
    .\scripts\build.ps1

    # Release build + publish single-file EXE
    .\scripts\build.ps1 -Release -Publish

    # Full release: build + publish + create Setup.exe installer
    .\scripts\build.ps1 -Pack

    # Full release with code signing
    .\scripts\build.ps1 -Pack -SignParams "/fd SHA256 /tr http://timestamp.digicert.com"
#>
param(
    [switch]$Release,
    [switch]$Publish,
    [switch]$Pack,
    [string]$SignParams = ""
)

$ErrorActionPreference = "Stop"
$Root = Split-Path $PSScriptRoot -Parent
$Version = (Get-Content "$Root\VERSION" -Raw).Trim()
$PackId = "Arcmark"
$MainExe = "Arcmark.exe"
$PublishDir = "$Root\publish"
$ReleasesDir = "$Root\releases"

# -Pack implies -Release and -Publish
if ($Pack) {
    $Release = $true
    $Publish = $true
}

$Config = if ($Release) { "Release" } else { "Debug" }

Write-Host ""
Write-Host "╔══════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║  Arcmark Windows Build — v$Version          ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# ── Step 1: Restore ──────────────────────────────────────────────────
Write-Host "[1/5] Restoring packages..." -ForegroundColor Yellow
dotnet restore "$Root\Arcmark.sln" --verbosity quiet
Write-Host "      Done." -ForegroundColor Green

# ── Step 2: Build ────────────────────────────────────────────────────
Write-Host "[2/5] Building ($Config)..." -ForegroundColor Yellow
dotnet build "$Root\Arcmark.sln" -c $Config --no-restore --verbosity quiet
Write-Host "      Done." -ForegroundColor Green

# ── Step 3: Test ─────────────────────────────────────────────────────
Write-Host "[3/5] Running tests..." -ForegroundColor Yellow
dotnet test "$Root\Arcmark.Tests\Arcmark.Tests.csproj" -c $Config --no-build --verbosity quiet
Write-Host "      All tests passed." -ForegroundColor Green

# ── Step 4: Publish ──────────────────────────────────────────────────
if ($Publish) {
    Write-Host "[4/5] Publishing self-contained executable..." -ForegroundColor Yellow

    # Clean previous publish
    if (Test-Path $PublishDir) { Remove-Item $PublishDir -Recurse -Force }

    dotnet publish "$Root\Arcmark\Arcmark.csproj" `
        -c Release `
        -r win-x64 `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:Version=$Version `
        -o $PublishDir `
        --verbosity quiet

    $exePath = Join-Path $PublishDir $MainExe
    if (Test-Path $exePath) {
        $size = [math]::Round((Get-Item $exePath).Length / 1MB, 1)
        Write-Host "      Published: $MainExe ($size MB)" -ForegroundColor Green
    } else {
        Write-Host "      ERROR: $MainExe not found in publish output!" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "[4/5] Skipping publish (use -Publish or -Pack)." -ForegroundColor DarkGray
}

# ── Step 5: Pack (Velopack installer) ────────────────────────────────
if ($Pack) {
    Write-Host "[5/5] Creating Velopack installer..." -ForegroundColor Yellow

    # Ensure vpk CLI is installed
    $vpkInstalled = dotnet tool list -g | Select-String "vpk"
    if (-not $vpkInstalled) {
        Write-Host "      Installing vpk CLI tool..." -ForegroundColor DarkGray
        dotnet tool install -g vpk
    }

    # Clean previous releases
    if (Test-Path $ReleasesDir) { Remove-Item $ReleasesDir -Recurse -Force }

    # Build the Velopack package
    $vpkArgs = @(
        "pack"
        "--packId", $PackId
        "--packVersion", $Version
        "--packDir", $PublishDir
        "--mainExe", $MainExe
        "--outputDir", $ReleasesDir
    )

    # Add code signing if provided
    if ($SignParams -ne "") {
        $vpkArgs += "--signParams"
        $vpkArgs += $SignParams
    }

    & vpk @vpkArgs

    Write-Host ""
    Write-Host "      ✓ Installer created!" -ForegroundColor Green
    Write-Host ""

    # List the output files
    Write-Host "      Output files:" -ForegroundColor Cyan
    Get-ChildItem $ReleasesDir | ForEach-Object {
        $size = [math]::Round($_.Length / 1MB, 1)
        Write-Host "        $($_.Name) ($size MB)" -ForegroundColor White
    }
} else {
    Write-Host "[5/5] Skipping installer (use -Pack)." -ForegroundColor DarkGray
}

Write-Host ""
Write-Host "Build complete!" -ForegroundColor Green
Write-Host ""
