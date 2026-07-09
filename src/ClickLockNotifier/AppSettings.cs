using Microsoft.Win32;

namespace ClickLockNotifier;

internal static class AppSettings
{
    private const string SettingsRegistryPath = @"Software\ClickLockNotifier";
    private const string SoundValueName = "SoundId";
    private const string SoundVolumeValueName = "SoundVolumePercent";
    private const string FullScreenOnlyValueName = "FullScreenOnly";
    private const string DesiredClickLockEnabledValueName = "DesiredClickLockEnabled";

    public static string GetSoundId()
    {
        using var key = Registry.CurrentUser.OpenSubKey(SettingsRegistryPath, writable: false);
        return key?.GetValue(SoundValueName) as string ?? SoundNotifier.DefaultSoundId;
    }

    public static void SetSoundId(string soundId)
    {
        using var key = Registry.CurrentUser.CreateSubKey(SettingsRegistryPath, writable: true);
        key.SetValue(SoundValueName, soundId, RegistryValueKind.String);
    }

    public static int GetSoundVolumePercent()
    {
        using var key = Registry.CurrentUser.OpenSubKey(SettingsRegistryPath, writable: false);
        return key?.GetValue(SoundVolumeValueName) is int volumePercent
            ? ClampVolumePercent(volumePercent)
            : SoundNotifier.DefaultVolumePercent;
    }

    public static void SetSoundVolumePercent(int volumePercent)
    {
        using var key = Registry.CurrentUser.CreateSubKey(SettingsRegistryPath, writable: true);
        key.SetValue(SoundVolumeValueName, ClampVolumePercent(volumePercent), RegistryValueKind.DWord);
    }

    public static int ClampVolumePercent(int volumePercent)
    {
        return Math.Clamp(volumePercent / 20 * 20, 0, 100);
    }

    public static bool GetFullScreenOnly()
    {
        using var key = Registry.CurrentUser.OpenSubKey(SettingsRegistryPath, writable: false);
        return key?.GetValue(FullScreenOnlyValueName) is int value && value != 0;
    }

    public static void SetFullScreenOnly(bool isEnabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(SettingsRegistryPath, writable: true);
        key.SetValue(FullScreenOnlyValueName, isEnabled ? 1 : 0, RegistryValueKind.DWord);
    }

    public static bool? GetDesiredClickLockEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(SettingsRegistryPath, writable: false);
        return key?.GetValue(DesiredClickLockEnabledValueName) is int value ? value != 0 : null;
    }

    public static void SetDesiredClickLockEnabled(bool isEnabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(SettingsRegistryPath, writable: true);
        key.SetValue(DesiredClickLockEnabledValueName, isEnabled ? 1 : 0, RegistryValueKind.DWord);
    }
}
