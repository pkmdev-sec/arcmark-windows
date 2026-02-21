<p align="center">
  <img src="https://raw.githubusercontent.com/Geek-1001/arcmark/main/Resources/AppIcon.png" width="96" style="filter: drop-shadow(0 2px 6px rgba(0, 0, 0, 0.18));">
</p>

# Arcmark for Windows

A native Windows bookmark manager that attaches to any browser window as a sidebar — port of the [macOS Arcmark app](https://github.com/Geek-1001/arcmark).

Organize bookmarks into workspaces with folders, pin your most-visited sites, and keep everything stored locally in a plain JSON file.

---

## Download & Install

### One-Click Installer (Recommended)

1. Go to the [**Releases**](https://github.com/Geek-1001/arcmark-windows/releases/latest) page
2. Download **`Arcmark-win-Setup.exe`**
3. Double-click to install — no admin rights needed
4. Arcmark launches automatically after install

The installer places Arcmark in your user profile (`%LocalAppData%\Arcmark`) and creates a Start Menu shortcut. Updates are downloaded automatically in the background.

> **Windows SmartScreen:** Since the app is not yet code-signed, Windows may show a "Windows protected your PC" warning. Click **"More info"** → **"Run anyway"**. This is normal for open-source software.

### Portable (No Install)

Download `Arcmark.exe` from [Releases](https://github.com/Geek-1001/arcmark-windows/releases/latest) and run it directly — no installation required. Note: auto-updates won't work in portable mode.

---

## Features

- **Browser Sidebar** — Attaches to Chrome, Edge, Brave, or any browser window and follows it as you move/resize
- **Workspaces** — Separate bookmark collections with 8 color themes
- **Nested Folders** — Drag-and-drop hierarchical organization
- **Pinned Links** — Surface your most-used URLs at the top of any workspace
- **Search** — Filter across titles and URLs with hierarchy preserved
- **Global Hotkey** — Toggle sidebar visibility with a keyboard shortcut (default: Ctrl+Shift+A)
- **Always on Top** — Pin the window above all other apps
- **Auto-Updates** — Background update checks with one-click restart
- **Cross-Platform Data** — JSON format identical to macOS; sync via any file sync tool
- **Local-First** — No account, no cloud, no telemetry

---

## System Requirements

- Windows 10 (build 18362 / version 1903) or later
- Windows 11 supported
- x64 processor

---

## Build from Source

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Windows 10+ (WPF requires Windows)

### Quick Start

```powershell
git clone https://github.com/Geek-1001/arcmark-windows.git
cd arcmark-windows

# Build and run
.\scripts\run.ps1

# Run tests
.\scripts\test.ps1
```

### Create Installer

```powershell
# Install the Velopack CLI (one-time)
dotnet tool install -g vpk

# Build + test + create Setup.exe
.\scripts\build.ps1 -Pack

# Output: releases/Arcmark-win-Setup.exe
```

### Release to GitHub

```powershell
# Full release (build + pack + upload)
.\scripts\release.ps1 -GitHubToken "ghp_your_token"

# Dry run (build + pack only, no upload)
.\scripts\release.ps1 -DryRun
```

See [docs/BUILD.md](docs/BUILD.md) for detailed build documentation.

---

## Architecture

| Layer | Technology |
|-------|-----------|
| UI Framework | WPF (.NET 8) |
| Pattern | MVVM (CommunityToolkit.Mvvm) |
| Persistence | JSON → `%LocalAppData%\Arcmark\data.json` |
| Browser Attachment | Win32 API (SetWinEventHook, GetWindowRect) |
| Global Hotkey | Win32 RegisterHotKey |
| Auto-Updates | Velopack (delta updates via GitHub Releases) |
| Testing | xUnit |

The app follows a strict MVVM pattern. `AppModel` is the single source of truth — it owns all workspace and node state, persists to disk on every mutation, and fires change notifications for ViewModels.

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for the full architecture guide.

---

## Cross-Platform Data

The JSON schema is identical between macOS and Windows:

```json
{
  "schemaVersion": 1,
  "workspaces": [{
    "id": "550e8400-...",
    "name": "Inbox",
    "colorId": "sky",
    "items": [
      { "type": "folder", "folder": { "id": "...", "name": "Work", "children": [...], "isExpanded": true } },
      { "type": "link", "link": { "id": "...", "title": "GitHub", "url": "https://github.com", "faviconPath": null } }
    ],
    "pinnedLinks": []
  }]
}
```

Copy `data.json` between platforms to sync your bookmarks.

---

## Contributing

Contributions welcome! Please open an issue before submitting a large PR.

1. Fork → branch → make changes → test → PR
2. Follow C# conventions (PascalCase types, camelCase locals)
3. No logic in code-behind — use ViewModels or Services

---

## License

MIT License — see [LICENSE](LICENSE) for details.
