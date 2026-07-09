# Third-Party Notices

This project includes third-party dependencies and sound assets. Keep this file with source and binary distributions.

## NuGet Dependencies

### NAudio

- Package: `NAudio`
- Version used by project: see `src/ClickLockNotifier/ClickLockNotifier.csproj`
- License: MIT
- Project: https://github.com/naudio/NAudio
- NuGet: https://www.nuget.org/packages/NAudio/

## Sound Effects

### Mixkit Sound Effects

The following embedded WAV files are from Mixkit sound effects:

- `correct-answer-tone.wav`
- `correct-answer-notification.wav`
- `mixkit-interface-option-select-2573.wav`
- `mixkit-software-interface-back-2575.wav`

License reference:

- Mixkit License: https://mixkit.co/license/
- Mixkit Sound Effects: https://mixkit.co/free-sound-effects/

Mixkit lists Sound Effects under its Free License category. Check Mixkit's current license terms before redistributing modified builds.

### akx/Notifications

The following embedded WAV files are from `akx/Notifications`:

- `akx-sharp-notification.wav`
- `akx-cloud-unlock.wav`

Source:

- https://github.com/akx/Notifications

The upstream project states a dual license choice between CC Attribution 3.0 Unported and CC0 Public Domain. This project uses the CC0 option for these sounds.

## Windows APIs

The application uses documented Windows APIs through P/Invoke, including:

- `SystemParametersInfoW`
- Raw Input APIs such as `RegisterRawInputDevices` and `GetRawInputData`
- Shell notification area APIs through Windows Forms `NotifyIcon`

These APIs are provided by Windows and are not distributed with this project.
