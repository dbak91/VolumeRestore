# Volume Restore

Volume Restore is a small .NET MAUI Android app that watches the media volume (`%VOLM` equivalent) and restores a configured level when a trigger level is reached.

The app is designed for a specific volume-state workflow without internet access, accounts, analytics, or cloud services.

## Behavior

With the default volume settings:

- Restore trigger: `6`
- Restore level: `25`
- Lower pause level: `5`
- Higher pause level: `7`
- Pause duration: `1 minute`

A direct media-volume change to `6` restores the level to `25`.

The lower and higher pause levels are discrete rocker-protection events, not a range. Reaching either `5` or `7` starts or restarts the pause timer. Trigger events are ignored while the pause is active, allowing normal manual volume adjustment with the device buttons.

## Monitoring control

Event monitoring defaults to **off**. The user must enable it with the switch near the top of the app.

The selected state is saved locally. The status below the switch reports:

- `ACTIVE` when the Android service is running
- `STARTING` while service startup is pending
- `OFF` when monitoring is disabled
- `ERROR` if Android rejects service startup

When enabled, monitoring uses a sticky Android foreground service. Removing the app from Android Recents does not normally stop monitoring. Android **Force stop** always stops the app and service until the app is opened again. After restarting the phone, open the app to start enabled monitoring again.

## Settings

The app stores these settings locally with .NET MAUI Preferences:

1. Level to restore to
2. Restore trigger
3. Lower pause level
4. Higher pause level
5. Pause duration in minutes

No network connection is used.

## Android permissions

The app declares only the normal permissions required to control media volume and run the foreground monitor:

- `android.permission.MODIFY_AUDIO_SETTINGS`
- `android.permission.FOREGROUND_SERVICE`
- `android.permission.FOREGROUND_SERVICE_SPECIAL_USE`

These permissions do not display runtime consent prompts. The app does not request notification permission. Android still requires foreground-service visibility and may show the service under **Active apps**.

## Requirements

- .NET 10 SDK with the MAUI Android workload
- Android SDK 36
- Visual Studio 2026 or the .NET CLI
- Android 7.0 (API 24) or later

The configured release publish target is Android ARM64.

## Build

```powershell
dotnet build VolumeRestore.csproj
```

## Publish

```powershell
dotnet publish VolumeRestore.csproj -c Release -p:PublishProfile=FolderProfile
```

The installable signed APK is written to:

```text
bin\Release\net10.0-android36.0\publish\android-arm64\com.example.volumerestore-Signed.apk
```

Release signing is loaded from the local `release-signing` directory. That directory is excluded from Git and must never be committed. Back it up securely because Android updates must be signed with the same key.

## Sideload installation

1. Copy the signed APK to the Android device.
2. Open the APK using the Files app.
3. If prompted, allow that Files source to install unknown apps.
4. Install or update Volume Restore.
5. Open the app and enable Event Monitoring when required.

## Privacy

Volume Restore operates entirely on the device. It does not request internet access, send telemetry, or collect personal data.
