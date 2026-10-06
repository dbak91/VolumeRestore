namespace VolumeRestore;

public partial class MainPage : ContentPage
{
    private bool _isLoadingSettings;

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
            || !TryReadPositiveNumber(MinPauseLevelEntry.Text, "Min Pause Level", out var minPauseLevel)
            || !TryReadPositiveNumber(MaxPauseLevelEntry.Text, "Max Pause Level", out var maxPauseLevel)
            || !TryReadPositiveNumber(PauseDurationEntry.Text, "Pause Duration", out var pauseDurationMinutes))
        {
            return;
        }

        if (minPauseLevel > maxPauseLevel)
        {
            ShowError("Min Pause Level cannot be greater than Max Pause Level.");
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

    private void ShowError(string message)
    {
        StatusLabel.TextColor = Color.FromArgb("#EF9A9A");
        StatusLabel.Text = message;
    }
}
