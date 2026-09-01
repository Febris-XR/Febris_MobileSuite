// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using System;

namespace Febris.MobileCompanionV3.Droid.Services
{
    /// <summary>
    /// Foreground service that hosts the screen-capture session.
    ///
    /// WHY IT EXISTS AT ALL. From API 29 Android refuses MediaProjection.CreateVirtualDisplay
    /// unless a foreground service with the mediaProjection type is already running. This is
    /// not optional plumbing, it is a hard platform gate, and there was previously NO Android
    /// Service of any kind anywhere in mobile/ (no [Service] attributes, no Service
    /// subclasses, no service elements in either manifest). So this has no in-repo precedent.
    /// The nearest structural precedent is the attribute style on
    /// Services/StatementReceiver.cs, which is mirrored here.
    ///
    /// The Companion targets sdk 33 with minSdk 25, so:
    ///   - FOREGROUND_SERVICE (API 28+) is required and is now declared in the manifest
    ///   - android:foregroundServiceType (API 29+) is emitted by the attribute below and is
    ///     simply ignored on older devices
    ///   - FOREGROUND_SERVICE_MEDIA_PROJECTION only becomes mandatory at targetSdk 34, which
    ///     a MAUI migration would force, so it is pre-declared now because it costs nothing
    ///
    /// The user-visible notification is not decoration. Android requires one, and for a
    /// product that screen-captures a learner's session it is the honest signal that capture
    /// is live. Do not make it silent or hide it.
    ///
    /// NOT VERIFIED ON A DEVICE. Nothing in this file has run. It compiles, which is all that
    /// can be proven without a headset attached.
    /// </summary>
    [Service(
        Enabled = true,
        Exported = false,
        ForegroundServiceType = ForegroundService.TypeMediaProjection)]
    public class ScreenCaptureService : Service
    {
        internal const string ChannelId = "febris_screen_capture";
        internal const int NotificationId = 4711;

        private const string ChannelName = "Screen sharing";
        private const string ChannelDescription = "Shown while this headset is sharing its screen with the instructor device.";

        /// <summary>Starts the service. Must be running BEFORE MediaProjection.CreateVirtualDisplay
        /// is called, or the platform throws.</summary>
        public static void Start(Context context)
        {
            Intent intent = new Intent(context, typeof(ScreenCaptureService));
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                context.StartForegroundService(intent);
            }
            else
            {
                context.StartService(intent);
            }
        }

        public static void Stop(Context context)
        {
            context.StopService(new Intent(context, typeof(ScreenCaptureService)));
        }

        public override IBinder OnBind(Intent intent)
        {
            // Not a bound service. The capture session is driven through IScreenEncoder,
            // not through a binder, so returning null is correct rather than lazy.
            return null;
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            try
            {
                CreateNotificationChannel();
                StartForeground(NotificationId, BuildNotification());
            }
            catch (Exception ex)
            {
                // If the notification cannot be raised the platform will kill the service
                // shortly afterwards anyway. Log and stop cleanly rather than leaving a
                // half-started service that MediaProjection will then fail against.
                Console.WriteLine("ScreenCaptureService failed to enter the foreground: " + ex.Message);
                StopSelf();
                return StartCommandResult.NotSticky;
            }

            // NotSticky on purpose: if the process dies mid-capture, Android must not silently
            // restart screen capture without the user present and without fresh consent.
            return StartCommandResult.NotSticky;
        }

        public override void OnDestroy()
        {
            try
            {
                if (Build.VERSION.SdkInt >= BuildVersionCodes.N)
                {
                    StopForeground(true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ScreenCaptureService teardown: " + ex.Message);
            }
            base.OnDestroy();
        }

        private void CreateNotificationChannel()
        {
            // Channels are API 26+. There were zero NotificationChannel usages in the
            // Companion before this, so there is no existing channel to reuse.
            if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            {
                return;
            }

            NotificationManager manager = (NotificationManager)GetSystemService(NotificationService);
            if (manager == null || manager.GetNotificationChannel(ChannelId) != null)
            {
                return;
            }

            NotificationChannel channel = new NotificationChannel(
                ChannelId, ChannelName, NotificationImportance.Low)
            {
                Description = ChannelDescription
            };
            channel.SetShowBadge(false);
            manager.CreateNotificationChannel(channel);
        }

        private Notification BuildNotification()
        {
            return new NotificationCompat.Builder(this, ChannelId)
                .SetContentTitle("Screen sharing active")
                .SetContentText("This headset is sharing its screen with the instructor device.")
                .SetSmallIcon(Resource.Mipmap.Febris_Companion)
                .SetOngoing(true)
                .SetPriority((int)NotificationPriority.Low)
                .SetVisibility((int)NotificationVisibility.Public)
                .Build();
        }
    }
}
