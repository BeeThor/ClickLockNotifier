namespace ClickLockNotifier;

internal sealed class ClickLockWatcher : IDisposable
{
    private readonly RawMouseInputListener _mouseInput;
    private readonly System.Windows.Forms.Timer _activationTimer = new();
    private ClickLockState _settings = ClickLockSettings.Read();
    private ClickLockTrackingState _trackingState = ClickLockTrackingState.Idle;

    public ClickLockWatcher()
    {
        _mouseInput = new RawMouseInputListener(OnMouseEvent);
        _activationTimer.Tick += ActivationTimer_Tick;
        UpdateTimerInterval();
    }

    public event EventHandler? Activated;

    public event EventHandler? Deactivated;

    public ClickLockState Settings => _settings;

    public void RefreshSettings()
    {
        _settings = ClickLockSettings.Read();
        UpdateTimerInterval();

        if (!_settings.IsEnabled)
        {
            ResetClickLockState();
        }
    }

    public void Dispose()
    {
        _activationTimer.Dispose();
        _mouseInput.Dispose();
    }

    private void OnMouseEvent(MouseHookMessage message)
    {
        switch (message)
        {
            case MouseHookMessage.LeftButtonDown:
                HandleLeftButtonDown();
                break;

            case MouseHookMessage.LeftButtonUp:
                HandleLeftButtonUp();
                break;
        }
    }

    private void HandleLeftButtonDown()
    {
        RefreshSettings();
        if (!_settings.IsEnabled)
        {
            return;
        }

        if (_trackingState == ClickLockTrackingState.LockedReadyToUnlock)
        {
            ResetClickLockState();
            RaiseDeactivated();
            return;
        }

        if (_trackingState == ClickLockTrackingState.LockedWaitingInitialRelease)
        {
            return;
        }

        _trackingState = ClickLockTrackingState.Pressing;
        _activationTimer.Stop();
        _activationTimer.Start();
    }

    private void HandleLeftButtonUp()
    {
        switch (_trackingState)
        {
            case ClickLockTrackingState.Pressing:
                ResetPressState();
                return;

            case ClickLockTrackingState.LockedWaitingInitialRelease:
                _trackingState = ClickLockTrackingState.LockedReadyToUnlock;
                return;

            case ClickLockTrackingState.LockedReadyToUnlock:
                ResetClickLockState();
                RaiseDeactivated();
                return;
        }
    }

    private void ActivationTimer_Tick(object? sender, EventArgs e)
    {
        _activationTimer.Stop();

        if (!_settings.IsEnabled || _trackingState != ClickLockTrackingState.Pressing)
        {
            return;
        }

        _trackingState = ClickLockTrackingState.LockedWaitingInitialRelease;
        Activated?.Invoke(this, EventArgs.Empty);
    }

    private void ResetPressState()
    {
        _activationTimer.Stop();
        _trackingState = ClickLockTrackingState.Idle;
    }

    private void ResetClickLockState()
    {
        ResetPressState();
    }

    private void UpdateTimerInterval()
    {
        _activationTimer.Interval = _settings.LockTimeMilliseconds;
    }

    private void RaiseDeactivated()
    {
        var handler = Deactivated;
        if (handler is null)
        {
            return;
        }

        _ = Task.Run(() => handler.Invoke(this, EventArgs.Empty));
    }

    private enum ClickLockTrackingState
    {
        Idle,
        Pressing,
        LockedWaitingInitialRelease,
        LockedReadyToUnlock
    }
}
