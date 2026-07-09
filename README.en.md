<p align="center">
  <img src="src/ClickLockNotifier/Assets/shubiao.svg" width="96" height="96" alt="ClickLock Notifier logo">
</p>

<h1 align="center">ClickLock Notifier</h1>

<p align="center">
  A small Windows tray helper for Mouse ClickLock notifications.
</p>

<p align="center">
  <a href="README.md">中文</a>
  ·
  <a href="https://github.com/BeeThor/ClickLockNotifier/releases/latest">Latest Release</a>
</p>

<p align="center">
  <img alt="Windows" src="https://img.shields.io/badge/Windows-10%20%7C%2011-0078D4?logo=windows&logoColor=white">
  <img alt=".NET" src="https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet&logoColor=white">
  <img alt="License" src="https://img.shields.io/github/license/BeeThor/ClickLockNotifier">
  <img alt="Release" src="https://img.shields.io/github/v/release/BeeThor/ClickLockNotifier?include_prereleases">
</p>

<p align="center">
  <a href="https://linux.do/">
    <img alt="LINUX DO" src="https://img.shields.io/badge/LINUX%20DO-Community-2EA44F?style=for-the-badge">
  </a>
</p>

---

ClickLock Notifier is a small Windows tray utility for players who use Windows Mouse ClickLock in games such as Russian Fishing 4.

It helps you hear exactly when ClickLock has been engaged, so you do not need to guess whether a long left-button press has turned into a locked hold.

## Preview

<p align="center">
  <img src="docs/images/tray-menu.png" width="238" alt="ClickLock Notifier tray menu preview">
</p>

## Who It Is For

- Russian Fishing 4 players.
- Games or desktop workflows that require a long left-button hold.
- Users who already use Windows Mouse ClickLock and want an audible confirmation when it engages.

## What It Does

- Controls the Windows Mouse ClickLock switch from the tray menu.
- Lets you choose the ClickLock trigger time.
- Plays a configurable sound when ClickLock is engaged.
- Plays a fixed unlock sound when the next click releases the locked hold.
- Supports sound volume presets from `0%` to `100%` in `20%` steps.
- Can run at Windows startup.
- Can optionally enable ClickLock only when the foreground window is fullscreen.
- Uses Raw Input for mouse button detection so game and ClickLock-modified mouse messages are less likely to confuse the notifier.
- Embeds tray icons into the executable, so runtime icons do not depend on external icon files.

## What It Does Not Do

- It does not read or modify game memory.
- It does not inject code into games.
- It does not automate fishing, aiming, movement, or gameplay decisions.
- It does not send network traffic.
- It is not a cheat client. It is only a local ClickLock state notifier and Windows setting helper.

## Controls

The application lives in the Windows notification area. Right-click the tray icon to open the menu.

Main controls:

- `启用单击锁定`: enable or disable Windows Mouse ClickLock.
- `仅全屏启用`: only apply ClickLock while the foreground window is fullscreen.
- `锁定时间`: choose the Windows ClickLock trigger time.
- `开机启动`: add or remove the app from current-user startup.
- `提示音`: choose the lock notification sound.
- `提示音音量`: choose notification volume.
- `测试提示音`: play the selected lock notification sound.

## Recommended Use For Russian Fishing 4

1. Start ClickLock Notifier.
2. Enable `启用单击锁定`.
3. Enable `仅全屏启用` if you want to avoid accidental ClickLock outside the game.
4. Set a comfortable `锁定时间`, for example `800 ms` or `1000 ms`.
5. Pick a short notification sound and volume.
6. In game, hold the left mouse button until you hear the lock sound.
7. Click once again to release the locked hold.

## Quick Download

Download `ClickLockNotifier-win-x64.zip` from [GitHub Releases](https://github.com/BeeThor/ClickLockNotifier/releases/latest).

## Requirements

- Windows 10 or Windows 11
- x64 Windows
- No administrator permission required for normal use
- .NET SDK 9.0 only required when building from source

The published executable is self-contained and does not require users to install .NET separately.

## Build From Source

```powershell
dotnet restore ClickLockNotifier.sln
dotnet build ClickLockNotifier.sln
```

Create a release build:

```powershell
.\scripts\publish.ps1
```

## Security And Game Policy Notes

This project uses documented Windows APIs:

- `SystemParametersInfo` to read and write Mouse ClickLock settings.
- Raw Input to observe physical mouse button down/up events.
- The current-user `Run` registry key when startup is enabled.

Some games or anti-cheat systems may have their own rules about input helpers, even when they do not modify game memory. Review the rules for your game before using any input-related utility.

## Third-Party Assets

Sound effects and dependencies are listed in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

## Community Link

<p align="center">
  <a href="https://linux.do/">
    <img alt="LINUX DO Community" src="https://img.shields.io/badge/LINUX%20DO-Community%20Link-2EA44F?style=for-the-badge">
  </a>
</p>

- **This open-source project links to and recognizes the LINUX DO community:** Yes
- Community: [https://linux.do/](https://linux.do/)

## License

This project is released under the MIT License. See [LICENSE](LICENSE).
