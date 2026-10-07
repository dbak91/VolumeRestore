namespace VolumeRestore;

public partial class MainPage : ContentPage
{
    private bool _isLoadingSettings;
    private bool _isKeyboardInsetTracking;
    private bool _shouldTrackKeyboardInset;
    private double _maxKeyboardSpacerHeight;
    private bool _hadFocusedEntry;
    private int _focusTransitionId;

    public MainPage()
    {
        InitializeComponent();
        VolumeRestoreSettings.ApplyMonitoringRecovery();
        VolumeMonitorController.RunningStateChanged += OnMonitoringRunningStateChanged;
        LoadSettings();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        VolumeMonitorController.ApplyEnabledState();
        UpdateMonitoringStatus();
    }

    private void LoadSettings()
    {
        _isLoadingSettings = true;
        var settings = VolumeRestoreSettings.Load();

        MonitoringSwitch.IsToggled = VolumeRestoreSettings.MonitoringEnabled;
        RestoreLevelEntry.Text = settings.RestoreLevel.ToString();
        RestoreTriggerEntry.Text = settings.RestoreTrigger.ToString();
        MinPauseLevelEntry.Text = settings.MinPauseLevel.ToString();
        MaxPauseLevelEntry.Text = settings.MaxPauseLevel.ToString();
        PauseDurationEntry.Text = settings.PauseDurationMinutes.ToString();
        _isLoadingSettings = false;
        UpdateMonitoringStatus();
    }

    private void OnMonitoringToggled(object sender, ToggledEventArgs e)
    {
        if (_isLoadingSettings)
        {
            return;
        }

        VolumeMonitorController.SetEnabled(e.Value);
        UpdateMonitoringStatus();
    }

    private void OnMonitoringRunningStateChanged(bool isRunning)
    {
        Dispatcher.Dispatch(UpdateMonitoringStatus);
    }

    private void UpdateMonitoringStatus()
    {
        if (!VolumeRestoreSettings.MonitoringEnabled)
        {
            MonitoringStatusLabel.Text = "OFF — volume events are not monitored";
            MonitoringStatusLabel.TextColor = Color.FromArgb("#EF9A9A");
            return;
        }

        if (!string.IsNullOrWhiteSpace(VolumeMonitorController.LastError))
        {
            MonitoringStatusLabel.Text = $"ERROR — {VolumeMonitorController.LastError}";
            MonitoringStatusLabel.TextColor = Color.FromArgb("#EF9A9A");
            return;
        }

        MonitoringStatusLabel.Text = VolumeMonitorController.IsRunning
            ? "ACTIVE — monitoring media volume"
            : "STARTING — waiting for the monitor service";
        MonitoringStatusLabel.TextColor = VolumeMonitorController.IsRunning
            ? Color.FromArgb("#A5D6A7")
            : Color.FromArgb("#FFE082");
    }

    private void OnSaveSettingsClicked(object sender, EventArgs e)
    {
        if (!TryReadPositiveNumber(RestoreLevelEntry.Text, "Level to restore to", out var restoreLevel)
            || !TryReadPositiveNumber(RestoreTriggerEntry.Text, "Restore Trigger", out var restoreTrigger)
            || !TryReadPositiveNumber(MinPauseLevelEntry.Text, "Lower Pause Level", out var minPauseLevel)
            || !TryReadPositiveNumber(MaxPauseLevelEntry.Text, "Higher Pause Level", out var maxPauseLevel)
            || !TryReadPositiveNumber(PauseDurationEntry.Text, "Pause Duration", out var pauseDurationMinutes))
        {
            return;
        }

        if (minPauseLevel > maxPauseLevel)
        {
            ShowError("Lower Pause Level cannot be greater than Higher Pause Level.");
            return;
        }

        VolumeRestoreSettings.Save(new VolumeRestoreOptions(
            restoreLevel,
            restoreTrigger,
            minPauseLevel,
            maxPauseLevel,
            pauseDurationMinutes));

        StatusLabel.TextColor = Color.FromArgb("#A5D6A7");
        StatusLabel.Text = "Settings saved on this device.";
    }

    private void OnResetDefaultsClicked(object sender, EventArgs e)
    {
        VolumeRestoreSettings.Reset();

        LoadSettings();
        VolumeMonitorController.ApplyEnabledState();
        StatusLabel.TextColor = Color.FromArgb("#A5D6A7");
        StatusLabel.Text = "Default settings restored and saved.";
    }

    private bool TryReadPositiveNumber(string? text, string fieldName, out int value)
    {
        if (int.TryParse(text, out value) && value >= 0)
        {
            return true;
        }

        ShowError($"{fieldName} must be a whole number of zero or greater.");
        return false;
    }

    private async void OnEntryFocused(object sender, FocusEventArgs e)
    {
        _focusTransitionId++;

        var keyboardAlreadyVisible = GetKeyboardHeightInDp() > 0;
        var isFocusSwitchWhileKeyboardOpen = keyboardAlreadyVisible && _hadFocusedEntry;

        _shouldTrackKeyboardInset = true;
        StartKeyboardInsetTracking();
        _hadFocusedEntry = true;

        if (!isFocusSwitchWhileKeyboardOpen && sender is View view)
        {
            await Task.Delay(120);
            await EnsureFocusedViewVisibleAboveKeyboardAsync(view);
        }
    }

    private async Task EnsureFocusedViewVisibleAboveKeyboardAsync(View view)
    {
        var keyboardHeight = GetKeyboardHeightInDp();
        var visibleHeight = SettingsScrollView.Height - keyboardHeight - 12;
        if (visibleHeight <= 0)
        {
            return;
        }

        var fieldTop = GetYRelativeToSettingsLayout(view);
        var fieldBottom = fieldTop + view.Height;
        var currentScrollY = SettingsScrollView.ScrollY;
        var visibleBottom = currentScrollY + visibleHeight;

        if (fieldBottom <= visibleBottom)
        {
            return;
        }

        var targetScrollY = Math.Max(0, fieldBottom - visibleHeight);
        await SettingsScrollView.ScrollToAsync(0, targetScrollY, false);
    }

    private double GetYRelativeToSettingsLayout(View view)
    {
        double y = view.Y;
        Element? parent = view.Parent;

        while (parent is VisualElement visualParent && !ReferenceEquals(visualParent, SettingsLayout))
        {
            y += visualParent.Y;
            parent = visualParent.Parent;
        }

        return y;
    }

    private async void OnEntryUnfocused(object sender, FocusEventArgs e)
    {
        var transitionId = ++_focusTransitionId;
        await Task.Delay(120);

        if (transitionId != _focusTransitionId || IsAnyEntryFocused())
        {
            return;
        }

        _shouldTrackKeyboardInset = false;
        _maxKeyboardSpacerHeight = 0;
        _hadFocusedEntry = false;
        KeyboardSpacer.HeightRequest = 0;
    }

    private bool IsAnyEntryFocused()
    {
        return RestoreLevelEntry.IsFocused
            || RestoreTriggerEntry.IsFocused
            || MinPauseLevelEntry.IsFocused
            || MaxPauseLevelEntry.IsFocused
            || PauseDurationEntry.IsFocused;
    }

    private async void StartKeyboardInsetTracking()
    {
        if (_isKeyboardInsetTracking)
        {
            return;
        }

        _isKeyboardInsetTracking = true;

        try
        {
            while (_shouldTrackKeyboardInset)
            {
                var keyboardHeight = GetKeyboardHeightInDp();
                var spacerHeight = keyboardHeight > 0 ? keyboardHeight + 16 : 0;

                if (spacerHeight > _maxKeyboardSpacerHeight)
                {
                    _maxKeyboardSpacerHeight = spacerHeight;
                }

                if (_maxKeyboardSpacerHeight > 0 && Math.Abs(KeyboardSpacer.HeightRequest - _maxKeyboardSpacerHeight) > 0.5)
                {
                    KeyboardSpacer.HeightRequest = _maxKeyboardSpacerHeight;
                }

                await Task.Delay(50);
            }
        }
        finally
        {
            _isKeyboardInsetTracking = false;
        }
    }

    private static double GetKeyboardHeightInDp()
    {
#if ANDROID
        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
        if (activity?.Window?.DecorView?.RootView is null)
        {
            return 0;
        }

        var rootView = activity.Window.DecorView.RootView;
        var visibleFrame = new Android.Graphics.Rect();
        rootView.GetWindowVisibleDisplayFrame(visibleFrame);

        var keyboardHeightPx = rootView.Height - visibleFrame.Height();
        if (keyboardHeightPx <= 0)
        {
            return 0;
        }

        var density = DeviceDisplay.MainDisplayInfo.Density;
        return density > 0 ? keyboardHeightPx / density : 0;
#else
        return 0;
#endif
    }

    private void ShowError(string message)
    {
        StatusLabel.TextColor = Color.FromArgb("#EF9A9A");
        StatusLabel.Text = message;
    }
}
