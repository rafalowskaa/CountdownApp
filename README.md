# CountdownApp

A personalised Windows desktop countdown for an upcoming event, with a chosen image or animated GIF and locally saved settings.

**C# · .NET 8 · WPF · JSON**

[Selected projects](https://github.com/rafalowskaa/BrowserGamesPlatform/blob/main/PORTFOLIO.md)

## Features

- Countdown in days, hours, minutes and seconds.
- A date and time picker for the target event.
- An event icon, caption and image or GIF.
- Local settings restored between sessions.
- An always-on-top window.
- Optional startup with Windows, controlled from the application.

## Run locally

Requirements: Windows and .NET 8 SDK.

```powershell
git clone https://github.com/rafalowskaa/CountdownApp.git
cd CountdownApp
dotnet restore CountdownApp.sln
dotnet run --project CountdownApp/CountdownApp.csproj
```

On first launch, choose **Ustaw datę...** to set a future event. The source contains a fixed initial date; it may already be in the past.

## Build a local Windows package

```powershell
dotnet publish CountdownApp/CountdownApp.csproj -c Release -r win-x64 --self-contained true -o artifacts/countdown-win-x64
```

Run `CountdownApp.exe` from the published folder. Configure autostart after placing that folder in its final location.

## Local data

Settings are stored in `%APPDATA%\CountdownApp\settings.json`. Windows autostart uses the current user's `Software\Microsoft\Windows\CurrentVersion\Run` registry key.

## Source map

| File | Purpose |
| --- | --- |
| `MainWindow.xaml` / `.xaml.cs` | Main UI, countdown and persistence |
| `SetDateWindow.xaml` / `.xaml.cs` | Event settings dialog |
| `AnimatedGifImage.cs` | GIF rendering |
| `AppSettings.cs` | Saved settings model |

This is a desktop portfolio project. There is no dedicated automated test project in the repository.
