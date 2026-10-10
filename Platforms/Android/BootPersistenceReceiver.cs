using Android.App;
using Android.Content;
using Android.OS;

namespace VolumeRestore;

[BroadcastReceiver(Enabled = true, Exported = true, DirectBootAware = true)]
[IntentFilter(new[]
{
    Intent.ActionBootCompleted,
    Intent.ActionLockedBootCompleted,
    "android.intent.action.QUICKBOOT_POWERON"
})]
public sealed class BootPersistenceReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context is null)
        {
            return;
        }

        var action = intent?.Action;
        if (action != Intent.ActionBootCompleted
            && action != Intent.ActionLockedBootCompleted
            && action != "android.intent.action.QUICKBOOT_POWERON")
        {
            return;
        }

        if (!VolumeRestoreSettings.BootPersistenceEnabled || !VolumeRestoreSettings.MonitoringEnabled)
        {
            return;
        }

        var monitorIntent = new Intent(context, typeof(VolumeMonitorService));

        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            context.StartForegroundService(monitorIntent);
        }
        else
        {
            context.StartService(monitorIntent);
        }
    }
}
