// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.App;
using Android.Content;
using Android.Media.Projection;
using System;
using System.Threading.Tasks;

namespace Febris.MobileCompanionV3.Droid.Services
{
    /// <summary>
    /// Obtains the user's screen-capture consent.
    ///
    /// Android requires an explicit per-session consent dialog before MediaProjection will
    /// hand out a projection, and that consent CANNOT be pre-granted here. The design doc
    /// (docs/MOBILE_AUTH.md, docs/MOBILE_P2P_VIDEO.md 5.1) investigated whether the MDM
    /// device-owner position could suppress it and concluded that it cannot, so the dialog
    /// stands and the flow has to survive the user declining it.
    ///
    /// The result arrives on MainActivity.OnActivityResult, which is why this is a static
    /// rendezvous rather than something the encoder owns: the encoder is created through
    /// DependencyService with a parameterless constructor and never sees the Activity.
    ///
    /// NOT VERIFIED ON A DEVICE.
    /// </summary>
    public static class ScreenCaptureConsent
    {
        /// <summary>Distinct from MainActivity's existing 2231 and 1234 codes.</summary>
        public const int RequestCode = 4712;

        private static readonly object Gate = new object();
        private static TaskCompletionSource<Intent> _pending;

        /// <summary>
        /// Shows the system consent dialog and resolves with the result Intent, or null when
        /// the user declined or the platform refused. Never throws for a decline: refusing
        /// consent is an ordinary outcome, not an error.
        /// </summary>
        public static Task<Intent> RequestAsync(Activity activity)
        {
            if (activity == null)
            {
                return Task.FromResult<Intent>(null);
            }

            TaskCompletionSource<Intent> tcs;
            lock (Gate)
            {
                if (_pending != null && !_pending.Task.IsCompleted)
                {
                    // A dialog is already up. Returning the same task rather than raising a
                    // second one keeps this idempotent, which matters because StartAsync is
                    // documented as idempotent.
                    return _pending.Task;
                }
                tcs = new TaskCompletionSource<Intent>();
                _pending = tcs;
            }

            try
            {
                MediaProjectionManager manager =
                    (MediaProjectionManager)activity.GetSystemService(Context.MediaProjectionService);
                if (manager == null)
                {
                    Complete(null);
                    return tcs.Task;
                }

                activity.StartActivityForResult(manager.CreateScreenCaptureIntent(), RequestCode);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ScreenCaptureConsent could not be requested: " + ex.Message);
                Complete(null);
            }

            return tcs.Task;
        }

        /// <summary>
        /// Called from MainActivity.OnActivityResult.
        ///
        /// It MUST be reached even when the result is not Ok. The existing handler returns
        /// early on any non-Ok result before its switch, so a declined consent would never
        /// reach a case there and the awaiting task would hang forever. The call site is
        /// therefore placed ahead of that early return.
        /// </summary>
        public static void Deliver(Result resultCode, Intent data)
        {
            Complete(resultCode == Result.Ok ? data : null);
        }

        private static void Complete(Intent result)
        {
            TaskCompletionSource<Intent> tcs;
            lock (Gate)
            {
                tcs = _pending;
                _pending = null;
            }
            tcs?.TrySetResult(result);
        }
    }
}
