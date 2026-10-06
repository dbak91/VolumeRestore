using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Database;
using Android.Media;
using Android.OS;
using Android.Provider;

namespace VolumeRestore;

[Service(Exported = false, ForegroundServiceType = ForegroundService.TypeSpecialUse)]
[MetaData("android.app.PROPERTY_SPECIAL_USE_FGS_SUBTYPE", Value = "Monitors media volume and restores a configured level")]
internal sealed class VolumeMonitorService : Service
{
    private const int NotificationId = 1001;
    private const string NotificationChannelId = "volume_restore_monitor";

    private AudioManager? _audioManager;
    private VolumeSettingsObserver? _volumeObserver;
    private int _lastVolume = -1;
    private DateTimeOffset _pausedUntil = DateTimeOffset.MinValue;

    public override void OnCreate()
    {
        base.OnCreate();

        try
        {
            _audioManager = (AudioManager?)GetSystemService(AudioService);
            if (_audioManager is null)
            {
                VolumeMonitorController.SetFailure("Android media audio service is unavailable.");
                StopSelf();
                return;
            }

            CreateNotificationChannel();
            StartForeground(NotificationId, CreateNotification());

            _lastVolume = _audioManager.GetStreamVolume(Android.Media.Stream.Music);
            _volumeObserver = new VolumeSettingsObserver(new Handler(Looper.MainLooper!), HandleVolumeChanged);
            ContentResolver?.RegisterContentObserver(Settings.System.ContentUri!, true, _volumeObserver);
            VolumeMonitorController.SetRunning(true);
        }
        catch (Exception exception)
        {
            VolumeMonitorController.SetFailure(exception.Message);
            StopSelf();
        }
    }

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        if (!VolumeRestoreSettings.MonitoringEnabled)
        {
            StopSelf();
            return StartCommandResult.NotSticky;
        }

        return StartCommandResult.Sticky;
    }

    public override IBinder? OnBind(Intent? intent) => null;

    public override void OnDestroy()
    {
        VolumeMonitorController.SetRunning(false);

        if (_volumeObserver is not null)
        {
            ContentResolver?.UnregisterContentObserver(_volumeObserver);
            _volumeObserver.Dispose();
            _volumeObserver = null;
        }

        base.OnDestroy();
    }

    private void HandleVolumeChanged()
    {
        if (_audioManager is null)
        {
            return;
        }

        var currentVolume = _audioManager.GetStreamVolume(Android.Media.Stream.Music);
        if (currentVolume == _lastVolume)
        {
            return;
        }

        _lastVolume = currentVolume;
        var options = VolumeRestoreSettings.Load();
        var now = DateTimeOffset.UtcNow;

        if (currentVolume == options.MinPauseLevel || currentVolume == options.MaxPauseLevel)
        {
            var pauseMinutes = Math.Clamp(options.PauseDurationMinutes, 0, 525600);
            _pausedUntil = now.AddMinutes(pauseMinutes);
            return;
        }

        if (currentVolume != options.RestoreTrigger || now < _pausedUntil)
        {
            return;
        }

        var maximumVolume = _audioManager.GetStreamMaxVolume(Android.Media.Stream.Music);
        var restoreLevel = Math.Clamp(options.RestoreLevel, 0, maximumVolume);
        if (restoreLevel == currentVolume)
        {
            return;
        }

        _lastVolume = restoreLevel;
        _audioManager.SetStreamVolume(Android.Media.Stream.Music, restoreLevel, VolumeNotificationFlags.RemoveSoundAndVibrate);
    }

    private void CreateNotificationChannel()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O)
        {
            return;
        }

        var notificationManager = (NotificationManager?)GetSystemService(NotificationService);
        var channel = new NotificationChannel(
            NotificationChannelId,
            "Volume monitoring",
            NotificationImportance.Low)
        {
            Description = "Keeps media volume restore monitoring active"
        };
        channel.SetSound(null, null);
        channel.EnableVibration(false);
        notificationManager?.CreateNotificationChannel(channel);
        channel.Dispose();
    }

    private Notification CreateNotification()
    {
        var launchIntent = PackageManager?.GetLaunchIntentForPackage(PackageName!);
        var pendingIntent = launchIntent is null
            ? null
            : PendingIntent.GetActivity(
                this,
                0,
                launchIntent,
                PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);

        var builder = Build.VERSION.SdkInt >= BuildVersionCodes.O
            ? new Notification.Builder(this, NotificationChannelId)
            : new Notification.Builder(this);

        builder
            .SetContentTitle("Volume Restore is active")
            .SetContentText("Monitoring media volume")
            .SetSmallIcon(Android.Resource.Drawable.IcLockSilentModeOff)
            .SetOngoing(true)
            .SetCategory(Notification.CategoryService)
            .SetShowWhen(false);

        if (pendingIntent is not null)
        {
            builder.SetContentIntent(pendingIntent);
        }

        return builder.Build();
    }

    private sealed class VolumeSettingsObserver(Handler handler, Action volumeChanged) : ContentObserver(handler)
    {
        public override void OnChange(bool selfChange)
        {
            base.OnChange(selfChange);
            volumeChanged();
        }
    }
}
