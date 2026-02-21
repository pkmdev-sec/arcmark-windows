# Build & Distribution Guide

## Prerequisites

| Tool | Version | Install |
|------|---------|---------|
| .NET 8 SDK | 8.0+ | [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/8.0) |
| Windows | 10 (1903)+ | Required for WPF |
| vpk CLI | Latest | `dotnet tool install -g vpk` |
| Git | Any | [git-scm.com](https://git-scm.com) |

## Quick Commands

```powershell
# Debug build + run
.\scripts\run.ps1

# Run tests
.\scripts\test.ps1

# Release build + tests
.\scripts\build.ps1 -Release

# Publish self-contained EXE
.\scripts\build.ps1 -Release -Publish
# Output: publish/Arcmark.exe (~60-80 MB, self-contained)

# Create installer (Setup.exe)
.\scripts\build.ps1 -Pack
# Output: releases/Arcmark-win-Setup.exe

# Full release to GitHub
.\scripts\release.ps1 -GitHubToken "ghp_..."
```

## Build Pipeline

### Step 1: Debug Build

```powershell
dotnet build Arcmark.sln -c Debug
dotnet run --project Arcmark/Arcmark.csproj
```

### Step 2: Run Tests

```powershell
dotnet test Arcmark.Tests/Arcmark.Tests.csproj -v normal
```

Tests cover:
- **ModelTests** — AppModel CRUD operations (workspace, node, pin/unpin)
- **JsonRoundTripTests** — JSON serialization compatibility with macOS
- **NodeFilteringTests** — Search/filter logic

### Step 3: Publish

```powershell
dotnet publish Arcmark/Arcmark.csproj \
    -c Release \
    -r win-x64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -o ./publish
```

This produces a single `Arcmark.exe` (~60-80 MB) that includes the .NET runtime. Users don't need .NET installed.

### Step 4: Create Installer

```powershell
# Install vpk CLI (one-time)
dotnet tool install -g vpk

# Pack into Velopack installer
vpk pack \
    --packId Arcmark \
    --packVersion 0.1.4 \
    --packDir ./publish \
    --mainExe Arcmark.exe \
    --outputDir ./releases
```

Output:
```
releases/
├── Arcmark-win-Setup.exe          ← User downloads this (one-click install)
├── Arcmark-0.1.4-win-full.nupkg   ← Full update package
└── releases.win.json               ← Update manifest
```

### Step 5: Upload to GitHub Releases

```powershell
vpk upload github \
    --repoUrl "https://github.com/Geek-1001/arcmark-windows" \
    --tag "v0.1.4" \
    --outputDir ./releases \
    --token "$env:GITHUB_TOKEN"
```

## Installer Behavior

The Velopack installer (`Setup.exe`):

1. **Installs** to `%LocalAppData%\Arcmark\` (no admin/UAC required)
2. **Creates** a Start Menu shortcut
3. **Launches** the app immediately after install
4. **Auto-updates** — on each launch, checks GitHub Releases for new versions
5. **Delta updates** — only downloads the diff between versions (fast, small)
6. **Uninstall** — via Windows Settings → Apps → Arcmark

## Code Signing (Optional)

Without code signing, Windows SmartScreen shows an "Unknown Publisher" warning. Options:

### Azure Trusted Signing (~$9/month)
```powershell
vpk pack ... --signParams "/fd SHA256 /tr http://timestamp.acs.microsoft.com /td SHA256 /dlib Azure.CodeSigning.Dlib.dll /dmdf credentials.json"
```

### Traditional EV Certificate ($300-500/year)
```powershell
vpk pack ... --signParams "/fd SHA256 /tr http://timestamp.digicert.com /td SHA256 /f cert.pfx /p password"
```

### No Signing (Open Source)
Add a note in your README explaining the SmartScreen warning. Most open-source users understand this.

## CI/CD

The project includes GitHub Actions workflows:

- **`.github/workflows/ci.yml`** — Runs on every push/PR: build + test
- **`.github/workflows/release.yml`** — Runs on version tags (`v*`): build + test + pack + upload to GitHub Releases

### Creating a Release

```bash
# 1. Update version
echo "0.2.0" > VERSION

# 2. Commit and tag
git add VERSION
git commit -m "Bump version to 0.2.0"
git tag v0.2.0
git push origin main --tags

# 3. GitHub Actions automatically builds and publishes the release
```

## Troubleshooting

### "dotnet: command not found"
Install the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) and restart your terminal.

### "vpk: command not found"
```powershell
dotnet tool install -g vpk
# Restart terminal or run: $env:PATH += ";$env:USERPROFILE\.dotnet\tools"
```

### SmartScreen blocks the installer
Click "More info" → "Run anyway". This happens because the app isn't code-signed.

### Build fails with "WPF not supported"
WPF requires Windows. You cannot build this project on macOS or Linux.

### Tests fail with "Platform not supported"
Some tests reference WPF types. Run tests on Windows only.
