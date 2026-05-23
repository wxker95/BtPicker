# BtPicker

A lightweight Windows system tray app for quickly connecting and disconnecting Bluetooth devices.

![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4) ![Windows](https://img.shields.io/badge/platform-Windows%2010%2B-0078D6)

## Features

- **One-click connect/disconnect** for any paired Bluetooth device
- **Battery level** display for devices that report it
- **Grouped or flat view** — organize devices by type (Audio / Input / Other) or list alphabetically
- **Auto-start with Windows** via a toggle in the menu
- **Quick access** to Windows Bluetooth Settings
- **Single instance** — launching again brings focus to the existing tray icon
- **No third-party dependencies** — built entirely on .NET 8, WinRT, and Win32 APIs

## Installation

1. Download `BtPicker-win-x64.zip` from the [latest release](../../releases/latest)
2. Extract `BtPicker.exe` to a permanent location (e.g. `C:\Program Files\BtPicker\`)
3. Run `BtPicker.exe` — it appears as a Bluetooth icon in the system tray

No installer or .NET runtime required. The app runs entirely from the exe, so don't move or delete it after launching — especially if "Start with Windows" is enabled, since auto-start points to the exe's location.

## Build from source

Requires [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) and Windows 10 or later.

```powershell
dotnet build BtPicker.sln
dotnet run --project BtPicker
```

## Usage

Right-click the tray icon to open the context menu:

- Click a device name to toggle its connection
- Battery percentage is shown below devices that report it
- **Group by type** — toggle between categorized and flat device lists
- **Start with Windows** — toggle auto-start on login
- **Bluetooth Settings** — opens the Windows Settings Bluetooth page
- **Exit** — closes BtPicker

## Configuration

Settings are stored at:

```
%APPDATA%\BtPicker\settings.json
```

| Setting           | Default | Description                              |
|-------------------|---------|------------------------------------------|
| `GroupByType`     | `true`  | Group devices by category in the menu    |
| `StartWithWindows`| `true`  | Register/unregister auto-start on login  |

## Logs

Diagnostic logs are written to:

```
%APPDATA%\BtPicker\btpicker.log
```

## License

MIT
