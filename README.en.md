# CodexUsage

[简体中文](README.md) | [English](README.en.md)

A lightweight Windows widget for Codex quota and reset time next to the notification area, with light, dark, and automatic sunrise/sunset themes.

**[Download stable v1.0.3](https://github.com/Amygdala42/CodexUsage/releases/download/v1.0.3/CodexUsage.exe)** · [Stable release notes](https://github.com/Amygdala42/CodexUsage/releases) · [Current development branch](https://github.com/bigbig-mua/CodexUsage/tree/feature/light-theme)

For Windows x64.

![CodexUsage taskbar widget](assets/widget-preview.png)

## Features

- The transparent taskbar widget uses a cyan disk and percentage for remaining quota, plus a blue disk and an hours-and-minutes countdown for time left in the same window. Both rows are right aligned and use the same high-contrast colors in light and dark themes.
- In the current development branch, the widget only appears on the taskbar. It hides when fullscreen video, image, or other applications cover that taskbar, and returns after fullscreen ends.
- The details window uses a translucent glass background with light, dark, and automatic sunrise/sunset themes.
- Automatic mode calculates local sunrise and sunset offline from coordinates entered manually, without network access or system location services.
- Shows returned quota windows with the same rules for Plus and Pro; Spark quotas are excluded.
- Refreshes automatically every five minutes, with manual refresh available.
- Shows the latest public reset announcement and its type, with a separate Source link. Announcements do not confirm an individual account reset.
- Saves language, theme, and coordinate preferences; fixed 100% app sizing follows Windows system DPI.

## Theme settings

- Select Light or Dark to keep the corresponding glass theme active.
- Select Sunrise/sunset, then choose Location and enter latitude and longitude. North latitudes and east longitudes are positive.
- Sunrise and sunset are calculated from the computer's local date and time zone. The app switches near those times and recalculates after the date changes, the location changes, or the computer wakes.

## Examples

These images illustrate the stable v1.0.3 layout with example data. The current development branch uses the new glass themes, so its interface differs. Available quota windows depend on the account's actual response.

### Plus

![Plus interface example](assets/plus-preview-en.png)

### Pro

![Pro interface example](assets/pro-preview-en.png)

## Download and run

Requires Windows x64, .NET Framework 4.8 or a newer 4.x version, and Codex installed and signed in with a subscription account.

1. Download `CodexUsage.exe` from the stable Releases, or download and extract `CodexUsage-Windows-x64.zip`. The glass and automatic theme features currently live on the `feature/light-theme` development branch.
2. Run `CodexUsage.exe`. Click the widget for details; move the pointer away to dismiss them.

Hover for a quick summary; move away or click to dismiss it. Right-click for the menu. The detail header shows the version and a link to the GitHub project.

To update, exit the old version from the tray, then replace the EXE. Settings live in `%LOCALAPPDATA%\CodexUsage`; existing settings beside the EXE are imported on first run.

See [TESTING](docs/TESTING.md) for verification scope.

## Development

```powershell
.\scripts\build.ps1
```

If the app is running and the default output file is locked, choose another output name:

```powershell
.\scripts\build.ps1 -OutputName CodexUsage-update.exe
```

[BUILD](docs/BUILD.md) · [TESTING](docs/TESTING.md) · [PRIVACY](docs/PRIVACY.md)

## License

[MIT](LICENSE) · Amygdala42
