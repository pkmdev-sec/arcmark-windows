# Arcmark Windows — Architecture

## Overview

Arcmark for Windows is a port of the macOS SwiftUI application to **.NET 8 + WPF**, targeting Windows 10 (1903+) and Windows 11. It shares the same JSON data format as the macOS version, enabling seamless cross-platform data portability.

```
Arcmark.sln
├── Arcmark/               # Main WPF application
│   ├── App.xaml           # Application entry point, resource dictionaries
│   ├── MainWindow.xaml    # Shell window (sidebar + content area)
│   ├── Models/            # Plain data types (Workspace, INode, FolderNode, LinkNode)
│   ├── ViewModels/        # MVVM ViewModels (CommunityToolkit.Mvvm)
│   ├── Views/             # XAML UserControls and Pages
│   ├── Services/          # AppModel, DataStore, FaviconService, NodeFilter
│   └── Serialization/     # ArcmarkJsonOptions, NodeJsonConverter
│
└── Arcmark.Tests/         # xUnit test project
    ├── ModelTests.cs       # AppModel CRUD operations
    ├── JsonRoundTripTests.cs
    └── NodeFilteringTests.cs
```

---

## MVVM Pattern

The app uses **CommunityToolkit.Mvvm** (`ObservableObject`, `[RelayCommand]`, `[ObservableProperty]`).

| Layer      | Responsibility                                                   |
|------------|------------------------------------------------------------------|
| **Model**  | Pure C# records/classes in `Arcmark.Models`. No UI dependencies. |
| **ViewModel** | Wraps `AppModel`; exposes `ObservableCollection<WorkspaceViewModel>`, commands, etc. |
| **View**   | XAML-only where possible. No code-behind logic beyond event wiring. |
| **Service** | `AppModel` is the single source of truth. `DataStore` owns disk I/O. |

Data flows one way: `AppModel` → `ViewModel` → `View`. User actions trigger `[RelayCommand]` methods on ViewModels, which call into `AppModel`, which then raises `PropertyChanged`/collection notifications upward.

---

## Feature Mapping: macOS → Windows

| macOS (SwiftUI)                          | Windows (WPF)                                     |
|------------------------------------------|---------------------------------------------------|
| `@Observable` / `@State`                 | `ObservableObject` + `[ObservableProperty]`       |
| `List` with `ForEach`                    | `ItemsControl` / `TreeView`                       |
| Drag-and-drop (`onDrop`, `onDrag`)       | `DragDrop` events + `IDropTarget` helper          |
| `NavigationSplitView`                    | `Grid` with splitter (`GridSplitter`)             |
| SF Symbols                               | Segoe Fluent Icons (via `Symbol` font)            |
| `NSPasteboard` (future share extension)  | `DataObject` / `IDataObject`                      |
| `.contextMenu` modifier                  | `ContextMenu` resource in `App.xaml`              |
| `Color` accent                           | `SystemAccentColor` resource brush               |
| `FocusedValue` / `FocusedBinding`        | Keyboard focus via `FocusManager`                 |
| `openURL(url)`                           | `Process.Start(new ProcessStartInfo { UseShellExecute = true })` |

---

## Win32 Interop Details

Several behaviours require P/Invoke or Windows-specific APIs:

### Window chrome (title bar customisation)
```csharp
// Remove default title bar; draw custom chrome
WindowChrome.SetWindowChrome(window, new WindowChrome { CaptionHeight = 0 });
```
For Windows 11 Mica/Acrylic material, `DwmSetWindowAttribute` (DWMWA_SYSTEMBACKDROP_TYPE) is called via P/Invoke at startup.

### System accent color
```csharp
var uiSettings = new Windows.UI.ViewManagement.UISettings();
var accent = uiSettings.GetColorValue(UIColorType.Accent);
```
Requires the `Microsoft.Windows.SDK.Contracts` NuGet package (already included).

### Favicon fetching
`FaviconService` uses `HttpClient` to fetch `https://<host>/favicon.ico` and falls back to Google's favicon CDN. Fetched icons are cached to `%APPDATA%\Arcmark\favicons\`.

---

## Data Storage

| Platform | Location                          |
|----------|-----------------------------------|
| macOS    | `~/Library/Application Support/Arcmark/state.json` |
| Windows  | `%APPDATA%\Arcmark\state.json`    |

`DataStore` resolves the path via `Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)`. The format is identical on both platforms.

---

## JSON Schema Compatibility

The serializer is configured in `ArcmarkJsonOptions`:

```csharp
new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true,
    Converters =
    {
        new NodeJsonConverter(),           // tagged-union {"type":"link","link":{...}}
        new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) // "sky" not "Sky"
    }
}
```

The `NodeJsonConverter` implements the same discriminated-union encoding used by Swift's `Codable` synthesis with `@CodingKeys`-based `type` discriminator. This ensures files saved on macOS open correctly on Windows and vice versa.

---

## Build & Development

### Prerequisites

- .NET 8 SDK (`winget install Microsoft.DotNet.SDK.8`)
- Windows 10 (1903 / build 18362) or Windows 11
- Visual Studio 2022 17.8+ **or** VS Code with C# Dev Kit

### Quick start

```powershell
# Clone and open
git clone https://github.com/yourorg/arcmark-windows
cd arcmark-windows

# Build & run
.\scripts\run.ps1

# Run tests
.\scripts\test.ps1
```

### Environment variables

| Variable              | Default                         | Purpose                        |
|-----------------------|---------------------------------|--------------------------------|
| `ARCMARK_DATA_DIR`    | `%APPDATA%\Arcmark`             | Override data directory        |
| `ARCMARK_LOG_LEVEL`   | `Warning`                       | Serilog minimum level          |

---

## Dependency Graph

```
Arcmark (WPF)
├── CommunityToolkit.Mvvm        — MVVM source generators
├── Microsoft.Extensions.Hosting — DI container, ILogger<T>
├── Serilog.Sinks.File           — structured file logging
└── Microsoft.Windows.SDK.Contracts — UISettings, accent color

Arcmark.Tests (xUnit)
├── xunit
├── xunit.runner.visualstudio
└── Microsoft.NET.Test.Sdk
```
