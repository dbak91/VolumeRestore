using Android.Content;
using Android.OS;

namespace VolumeRestore;

internal static class VolumeMonitorController
{
    internal static event Action<bool>? RunningStateChanged;

    internal static bool IsRunning { get; private set; }
    internal static string? LastError { get; private set; }

    internal static void ApplyEnabledState()
    {
        try
        {
            var context = Android.App.Application.Context;
            var intent = new Intent(context, typeof(VolumeMonitorService));

            if (!VolumeRestoreSettings.MonitoringEnabled)
            {
                context.StopService(intent);
                SetRunning(false);
                return;
            }

            LastError = null;
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                context.StartForegroundService(intent);
            }
            else
            {
                context.StartService(intent);
            }
        }
        catch (Exception exception)
        {
            SetFailure(exception.Message);
        }
    }

    internal static void SetEnabled(bool enabled)
    {
        VolumeRestoreSettings.MonitoringEnabled = enabled;
        ApplyEnabledState();
    }

    internal static void RestartIfRunning()
    {
        if (!IsRunning)
        {
            return;
        }

        try
        {
            var context = Android.App.Application.Context;
            var intent = new Intent(context, typeof(VolumeMonitorService));

            context.StopService(intent);
            SetRunning(false);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                context.StartForegroundService(intent);
            }
            else
            {
                context.StartService(intent);
            }
        }
        catch (Exception exception)
        {
            SetFailure(exception.Message);
        }
    }

    internal static void SetRunning(bool isRunning)
    {
        if (isRunning)
        {
            LastError = null;
        }

        if (IsRunning == isRunning)
        {
            return;
        }

        IsRunning = isRunning;
        RunningStateChanged?.Invoke(isRunning);
    }

    internal static void SetFailure(string message)
    {
        LastError = message;
        IsRunning = false;
        RunningStateChanged?.Invoke(false);
    }
}
