# Security Policy

## Supported Versions

Only the latest release is supported.

## Reporting A Vulnerability

Open a private advisory or issue on GitHub if the repository enables security advisories. Otherwise, open an issue without including sensitive exploit details.

## Security Notes

ClickLock Notifier:

- Does not read or modify game memory.
- Does not inject DLLs or code into other processes.
- Does not require administrator privileges for normal use.
- Stores settings under `HKCU\Software\ClickLockNotifier`.
- Uses `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` only when startup is enabled.
- Uses Raw Input to observe mouse button events.
- Uses `SystemParametersInfoW` to change Windows Mouse ClickLock settings.

Because this is an input-related utility, some games or anti-cheat systems may classify it differently from normal desktop software. Users are responsible for checking game rules before use.
