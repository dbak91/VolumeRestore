namespace VolumeRestore;

internal readonly record struct VolumeRestoreOptions(
    int RestoreLevel,
    int RestoreTrigger,
    int MinPauseLevel,
    int MaxPauseLevel,
    int PauseDurationMinutes);

internal static class VolumeRestoreSettings
{
    internal const int DefaultRestoreLevel = 25;
    internal const int DefaultRestoreTrigger = 6;
    internal const int DefaultMinPauseLevel = 5;
    internal const int DefaultMaxPauseLevel = 7;
    internal const int DefaultPauseDurationMinutes = 1;
    internal const bool DefaultMonitoringEnabled = false;

    private const string RestoreLevelKey = "RestoreLevel";
    private const string RestoreTriggerKey = "RestoreTrigger";
    private const string MinPauseLevelKey = "MinPauseLevel";
    private const string MaxPauseLevelKey = "MaxPauseLevel";
    private const string PauseDurationMinutesKey = "PauseDurationMinutes";
    private const string MonitoringEnabledKey = "MonitoringEnabled";
    private const string MonitoringRecoveryAppliedKey = "MonitoringRecoveryAppliedV2";

    internal static bool MonitoringEnabled
    {
        get => Preferences.Get(MonitoringEnabledKey, DefaultMonitoringEnabled);
        set => Preferences.Set(MonitoringEnabledKey, value);
    }

    internal static void ApplyMonitoringRecovery()
    {
        if (Preferences.Get(MonitoringRecoveryAppliedKey, false))
        {
            return;
        }

        MonitoringEnabled = false;
        Preferences.Set(MonitoringRecoveryAppliedKey, true);
    }

    internal static VolumeRestoreOptions Load() => new(
        Preferences.Get(RestoreLevelKey, DefaultRestoreLevel),
        Preferences.Get(RestoreTriggerKey, DefaultRestoreTrigger),
        Preferences.Get(MinPauseLevelKey, DefaultMinPauseLevel),
        Preferences.Get(MaxPauseLevelKey, DefaultMaxPauseLevel),
        Preferences.Get(PauseDurationMinutesKey, DefaultPauseDurationMinutes));

    internal static void Save(VolumeRestoreOptions options)
    {
        Preferences.Set(RestoreLevelKey, options.RestoreLevel);
        Preferences.Set(RestoreTriggerKey, options.RestoreTrigger);
        Preferences.Set(MinPauseLevelKey, options.MinPauseLevel);
        Preferences.Set(MaxPauseLevelKey, options.MaxPauseLevel);
        Preferences.Set(PauseDurationMinutesKey, options.PauseDurationMinutes);
    }

    internal static void Reset()
    {
        Save(new VolumeRestoreOptions(
            DefaultRestoreLevel,
            DefaultRestoreTrigger,
            DefaultMinPauseLevel,
            DefaultMaxPauseLevel,
            DefaultPauseDurationMinutes));
        MonitoringEnabled = DefaultMonitoringEnabled;
    }
}
