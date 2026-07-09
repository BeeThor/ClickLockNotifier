using Microsoft.Win32;
using System.Reflection;

namespace ClickLockNotifier;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private const int AutoRefreshIntervalMilliseconds = 2000;
    private const string StartupRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string StartupValueName = "ClickLockNotifier";
    private static readonly int[] LockTimeOptions = [200, 500, 800, 1000, 1200, 1500, 2000, 3000, 5000];
    private static readonly int[] SoundVolumeOptions = [0, 20, 40, 60, 80, 100];

    private readonly ClickLockWatcher _watcher;
    private readonly SoundNotifier _soundNotifier = new();
    private readonly Icon _enabledTrayIcon;
    private readonly Icon _disabledTrayIcon;
    private readonly ContextMenuStrip _menu = new();
    private readonly NotifyIcon _notifyIcon;
    private readonly System.Windows.Forms.Timer _statusRefreshTimer = new();
    private readonly ToolStripMenuItem _clickLockEnabledItem = new("启用单击锁定");
    private readonly ToolStripMenuItem _fullScreenOnlyItem = new("仅全屏启用");
    private readonly ToolStripMenuItem _statusItem = new();
    private readonly ToolStripMenuItem _lockTimeMenuItem = new("锁定时间");
    private readonly ToolStripMenuItem _startupItem = new("开机启动");
    private readonly ToolStripMenuItem _soundMenuItem = new("提示音");
    private readonly ToolStripMenuItem _soundVolumeMenuItem = new("提示音音量");
    private readonly ToolStripMenuItem _testSoundItem = new("测试提示音");
    private readonly ToolStripMenuItem _exitItem = new("退出");
    private bool _desiredClickLockEnabled;
    private bool _fullScreenOnly;
    private bool _isForegroundFullScreen;
    private bool _isUpdatingMenu;

    public TrayApplicationContext()
    {
        _enabledTrayIcon = LoadTrayIcon("app.ico");
        _disabledTrayIcon = LoadTrayIcon("app-off.ico");
        var initialClickLockState = ClickLockSettings.Read();
        _fullScreenOnly = AppSettings.GetFullScreenOnly();
        _desiredClickLockEnabled = AppSettings.GetDesiredClickLockEnabled() ?? initialClickLockState.IsEnabled;
        _soundNotifier.SelectedSoundId = NormalizeSoundId(AppSettings.GetSoundId());
        _soundNotifier.VolumePercent = AppSettings.GetSoundVolumePercent();
        _soundNotifier.Preload();
        _watcher = new ClickLockWatcher();
        _watcher.Activated += Watcher_Activated;
        _watcher.Deactivated += Watcher_Deactivated;

        _clickLockEnabledItem.CheckOnClick = true;
        _clickLockEnabledItem.CheckedChanged += (_, _) => SetClickLockEnabledFromMenu();
        _fullScreenOnlyItem.CheckOnClick = true;
        _fullScreenOnlyItem.CheckedChanged += (_, _) => SetFullScreenOnlyFromMenu();

        _startupItem.CheckOnClick = true;
        _startupItem.Checked = IsStartupEnabled();
        _startupItem.CheckedChanged += (_, _) => SetStartupEnabled(_startupItem.Checked);
        _testSoundItem.Click += (_, _) => _soundNotifier.Play();
        _exitItem.Click += (_, _) => ExitThread();
        BuildLockTimeMenu();
        BuildSoundMenu();
        BuildSoundVolumeMenu();

        _statusRefreshTimer.Interval = AutoRefreshIntervalMilliseconds;
        _statusRefreshTimer.Tick += (_, _) => RefreshStatus();
        _statusRefreshTimer.Start();

        _statusItem.Enabled = false;
        _menu.Opening += (_, _) => RefreshMenuState();
        _menu.Items.Add(_clickLockEnabledItem);
        _menu.Items.Add(_fullScreenOnlyItem);
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(_statusItem);
        _menu.Items.Add(_lockTimeMenuItem);
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(_startupItem);
        _menu.Items.Add(_soundMenuItem);
        _menu.Items.Add(_soundVolumeMenuItem);
        _menu.Items.Add(_testSoundItem);
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(_exitItem);

        _notifyIcon = new NotifyIcon
        {
            ContextMenuStrip = _menu,
            Icon = _disabledTrayIcon,
            Text = "ClickLock Notifier",
            Visible = true
        };

        SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
        RefreshStatus();
    }

    protected override void ExitThreadCore()
    {
        SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
        _statusRefreshTimer.Stop();
        _statusRefreshTimer.Dispose();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _menu.Dispose();
        _enabledTrayIcon.Dispose();
        _disabledTrayIcon.Dispose();
        _watcher.Activated -= Watcher_Activated;
        _watcher.Deactivated -= Watcher_Deactivated;
        _watcher.Dispose();
        base.ExitThreadCore();
    }

    private void Watcher_Activated(object? sender, EventArgs e)
    {
        _soundNotifier.Play();
    }

    private void Watcher_Deactivated(object? sender, EventArgs e)
    {
        _soundNotifier.PlayUnlock();
    }

    private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category is UserPreferenceCategory.Mouse or UserPreferenceCategory.Accessibility)
        {
            RefreshStatus();
        }
    }

    private void RefreshStatus(bool showBalloon = false)
    {
        ApplyEffectiveClickLockState();
        _watcher.RefreshSettings();
        var state = _watcher.Settings;

        var status = GetStatusText(state);

        _statusItem.Text = status;
        _notifyIcon.Text = TruncateForNotifyIcon($"ClickLock Notifier - {status}");
        _notifyIcon.Icon = state.IsEnabled ? _enabledTrayIcon : _disabledTrayIcon;
    }

    private static string TruncateForNotifyIcon(string text)
    {
        return text.Length <= 63 ? text : string.Concat(text.AsSpan(0, 60), "...");
    }

    private void SetClickLockEnabledFromMenu()
    {
        if (_isUpdatingMenu)
        {
            return;
        }

        try
        {
            _desiredClickLockEnabled = _clickLockEnabledItem.Checked;
            AppSettings.SetDesiredClickLockEnabled(_desiredClickLockEnabled);
            ApplyEffectiveClickLockState(forceDesiredState: true);
            RefreshStatus();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "ClickLock Notifier", MessageBoxButtons.OK, MessageBoxIcon.Error);
            RefreshMenuState();
        }
    }

    private void SetFullScreenOnlyFromMenu()
    {
        if (_isUpdatingMenu)
        {
            return;
        }

        try
        {
            _fullScreenOnly = _fullScreenOnlyItem.Checked;
            AppSettings.SetFullScreenOnly(_fullScreenOnly);
            ApplyEffectiveClickLockState(forceDesiredState: true);
            RefreshStatus();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "ClickLock Notifier", MessageBoxButtons.OK, MessageBoxIcon.Error);
            RefreshMenuState();
        }
    }

    private void BuildLockTimeMenu()
    {
        foreach (var milliseconds in LockTimeOptions)
        {
            var item = new ToolStripMenuItem(FormatLockTime(milliseconds))
            {
                Tag = milliseconds
            };
            item.Click += (_, _) => SetLockTime(milliseconds);
            _lockTimeMenuItem.DropDownItems.Add(item);
        }
    }

    private void SetLockTime(int milliseconds)
    {
        try
        {
            ClickLockSettings.SetLockTime(milliseconds);
            RefreshStatus();
            RefreshLockTimeMenuChecks(_watcher.Settings.LockTimeMilliseconds);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "ClickLock Notifier", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BuildSoundMenu()
    {
        foreach (var choice in SoundNotifier.Choices)
        {
            var item = new ToolStripMenuItem(choice.DisplayName)
            {
                CheckOnClick = true,
                Tag = choice.Id
            };
            item.Click += (_, _) => SelectSound(choice.Id);
            _soundMenuItem.DropDownItems.Add(item);
        }

        RefreshSoundMenuChecks();
    }

    private void SelectSound(string soundId)
    {
        _soundNotifier.SelectedSoundId = soundId;
        AppSettings.SetSoundId(soundId);
        RefreshSoundMenuChecks();
        _soundNotifier.Play();
    }

    private void BuildSoundVolumeMenu()
    {
        foreach (var volumePercent in SoundVolumeOptions)
        {
            var item = new ToolStripMenuItem($"{volumePercent}%")
            {
                CheckOnClick = true,
                Tag = volumePercent
            };
            item.Click += (_, _) => SelectSoundVolume(volumePercent);
            _soundVolumeMenuItem.DropDownItems.Add(item);
        }

        RefreshSoundVolumeMenuChecks();
    }

    private void SelectSoundVolume(int volumePercent)
    {
        _soundNotifier.VolumePercent = AppSettings.ClampVolumePercent(volumePercent);
        AppSettings.SetSoundVolumePercent(_soundNotifier.VolumePercent);
        RefreshSoundVolumeMenuChecks();
        _soundNotifier.Play();
    }

    private void RefreshSoundMenuChecks()
    {
        foreach (var item in _soundMenuItem.DropDownItems.OfType<ToolStripMenuItem>())
        {
            item.Checked = string.Equals(item.Tag as string, _soundNotifier.SelectedSoundId, StringComparison.Ordinal);
        }
    }

    private void RefreshSoundVolumeMenuChecks()
    {
        foreach (var item in _soundVolumeMenuItem.DropDownItems.OfType<ToolStripMenuItem>())
        {
            item.Checked = item.Tag is int volumePercent && volumePercent == _soundNotifier.VolumePercent;
        }
    }

    private void RefreshMenuState()
    {
        RefreshStatus();
        var state = _watcher.Settings;

        _isUpdatingMenu = true;
        try
        {
            _clickLockEnabledItem.Checked = _desiredClickLockEnabled;
            _fullScreenOnlyItem.Checked = _fullScreenOnly;
            _startupItem.Checked = IsStartupEnabled();
            RefreshLockTimeMenuChecks(state.LockTimeMilliseconds);
            RefreshSoundMenuChecks();
            RefreshSoundVolumeMenuChecks();
        }
        finally
        {
            _isUpdatingMenu = false;
        }
    }

    private void ApplyEffectiveClickLockState(bool forceDesiredState = false)
    {
        _isForegroundFullScreen = FullScreenDetector.IsForegroundWindowFullScreen();

        if (!_fullScreenOnly)
        {
            var currentState = ClickLockSettings.Read();
            if (forceDesiredState)
            {
                if (currentState.IsEnabled != _desiredClickLockEnabled)
                {
                    ClickLockSettings.SetEnabled(_desiredClickLockEnabled);
                }

                return;
            }

            _desiredClickLockEnabled = currentState.IsEnabled;
            AppSettings.SetDesiredClickLockEnabled(_desiredClickLockEnabled);
            return;
        }

        var shouldEnableClickLock = _desiredClickLockEnabled && _isForegroundFullScreen;
        var state = ClickLockSettings.Read();
        if (state.IsEnabled != shouldEnableClickLock)
        {
            ClickLockSettings.SetEnabled(shouldEnableClickLock);
        }
    }

    private string GetStatusText(ClickLockState state)
    {
        if (!_desiredClickLockEnabled)
        {
            return "未启用：Windows 单击锁定关闭";
        }

        if (!_fullScreenOnly)
        {
            return state.IsEnabled
                ? $"已启用，长按 {state.LockTimeMilliseconds} ms 后提示"
                : "未启用：Windows 单击锁定关闭";
        }

        return state.IsEnabled
            ? $"仅全屏启用中，长按 {state.LockTimeMilliseconds} ms 后提示"
            : _isForegroundFullScreen
                ? "仅全屏启用：正在应用 Windows 单击锁定"
                : "仅全屏启用：等待前台程序全屏";
    }

    private void RefreshLockTimeMenuChecks(int currentMilliseconds)
    {
        foreach (var item in _lockTimeMenuItem.DropDownItems.OfType<ToolStripMenuItem>())
        {
            item.Checked = item.Tag is int milliseconds && milliseconds == currentMilliseconds;
        }
    }

    private static string FormatLockTime(int milliseconds)
    {
        return milliseconds % 1000 == 0
            ? $"{milliseconds / 1000} 秒"
            : $"{milliseconds} 毫秒";
    }

    private static string NormalizeSoundId(string soundId)
    {
        return SoundNotifier.Choices.Any(choice => string.Equals(choice.Id, soundId, StringComparison.Ordinal))
            ? soundId
            : SoundNotifier.DefaultSoundId;
    }

    private static Icon LoadTrayIcon(string fileName)
    {
        var resourceName = $"ClickLockNotifier.Assets.{fileName}";
        using var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
        if (resourceStream is not null)
        {
            using var icon = new Icon(resourceStream);
            return (Icon)icon.Clone();
        }

        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", fileName);
        if (File.Exists(iconPath))
        {
            return new Icon(iconPath);
        }

        var executableIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        return executableIcon is not null ? new Icon(executableIcon, SystemInformation.SmallIconSize) : SystemIcons.Application;
    }

    private static bool IsStartupEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryPath, writable: false);
        return key?.GetValue(StartupValueName) is string;
    }

    private static void SetStartupEnabled(bool isEnabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryPath, writable: true)
            ?? Registry.CurrentUser.CreateSubKey(StartupRegistryPath, writable: true);

        if (!isEnabled)
        {
            key.DeleteValue(StartupValueName, throwOnMissingValue: false);
            return;
        }

        var executablePath = Environment.ProcessPath ?? Application.ExecutablePath;
        key.SetValue(StartupValueName, $"\"{executablePath}\"", RegistryValueKind.String);
    }
}
