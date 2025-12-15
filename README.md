# Teams Joystick Controller

A background Windows 11 tray application that listens to a USB joystick/button box via Raw Input and drives the new Microsoft Teams client using keyboard shortcuts. Button single/double/triple presses map to Teams actions such as mute, camera, hand raise, reactions, and sharing.

## Features
- Runs headless with a system tray icon for quick access.
- Global joystick/gamepad button handling using Raw Input (works without window focus).
- Configurable single/double/triple press mappings stored under `%APPDATA%\\TeamsJoystickController\\config.json`.
- Sends Teams shortcuts by foregrounding the Teams window and restoring the previous window.
- Automatic startup registration under the current user.
- Lightweight logging to `%APPDATA%\\TeamsJoystickController\\logs\\log.txt`.

## Prerequisites
- **Operating system:** Windows 11 (or Windows 10 with the new Teams client installed).
- **.NET SDK:** .NET 8 SDK (includes Windows Desktop SDK for WPF). Verify with `dotnet --list-sdks`.
- **Visual Studio Code:** Recommended extensions
  - C# Dev Kit
  - C# (powered by Roslyn/OmniSharp)
  - IntelliCode (optional but helpful)
- **Hardware:** USB joystick/gamepad/button box exposed as a HID device.

## Getting started in Visual Studio Code
1. Install the .NET 8 SDK and restart VS Code so the C# extensions pick it up.
2. Clone the repository and open the folder in VS Code.
3. Restore dependencies: `dotnet restore TeamsJoystickController.sln`.
4. Build the solution: `dotnet build TeamsJoystickController.sln`.
5. Run the tray app (from a Windows terminal): `dotnet run --project TeamsJoystickController.App`.

> Note: WPF apps require Windows; builds/tests should be run on Windows for correct results.

## Configuration
- Location: `%APPDATA%\\TeamsJoystickController\\config.json` (created on first run if missing).
- Default button mappings:
  - Button 1: Single = `ToggleMute`, Double = `ToggleCamera`
  - Button 2: Single = `ShareScreenPreferred`, Double = `OpenShareTray`
  - Button 3: Single = `React:Like`, Double = `React:Love`, Triple = `React:Clap`
  - Button 4: Single = `ToggleHand`
  - Button 5: Single = `Spare`
- Timing defaults: Double press = 250 ms, Triple press = 350 ms.
- Preferred monitor index for sharing defaults to `1` (future use).

Use the tray menu to **Open config** in Notepad or **Reload config** after edits. Timing changes take effect immediately on reload.

## Tray controls
- **Open config:** Opens the JSON configuration in Notepad.
- **Reload config:** Reloads config from disk and refreshes mappings/timing.
- **Exit:** Disposes the tray icon and shuts down the app.

## Startup registration
On launch, the app registers itself in `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` as `TeamsJoystickController` pointing to the current executable. If registration fails, the app continues running and logs the error.

## Logging
Log file: `%APPDATA%\\TeamsJoystickController\\logs\\log.txt`. Entries are timestamped with INFO/WARN/ERROR levels. Logging failures are swallowed to avoid impacting runtime.

## Notes and limitations
- Teams interaction relies on keyboard shortcuts. The app briefly brings Teams to the foreground to send the chord, then restores the previous window; a slight focus flicker is expected.
- Reaction and share monitor selection behaviors are best-effort placeholders and may need refinement for specific Teams versions.

## Build command
dotnet build
dotnet publish TeamsJoystickController.App -c Release -r win-x64 --self-contained true /p:PublishSingleFile=false