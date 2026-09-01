// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.Content;
using Android.Views;
using Febris.MobileServerV3.Droid.Renderers;
using Febris.MobileServerV3.Droid.Services;
using Febris.MobileServerV3.MVVM.View.Controls;
using Febris.SharedMobileLibrary.Interfaces;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(VideoSurfaceView), typeof(VideoSurfaceViewRenderer))]

namespace Febris.MobileServerV3.Droid.Renderers
{
    /// <summary>
    /// Backs <see cref="VideoSurfaceView"/> with a native SurfaceView and hands its Surface
    /// to the decoder.
    ///
    /// This is the only reason the decode path allocates nothing per frame: MediaCodec is
    /// configured against this Surface and renders into it directly, so pictures never pass
    /// through Forms.
    ///
    /// There were no custom renderers anywhere on the Server head before this, so there is
    /// no in-repo precedent for the ExportRenderer pattern here.
    ///
    /// LIFETIME IS THE WHOLE JOB. A MediaCodec configured against a destroyed Surface is a
    /// hard crash, not a dropped frame, so SurfaceDestroyed must tear the decoder down
    /// before returning. The callbacks and the arrival of video are independent and
    /// unordered, which is why the decoder caches its config and can be started from either
    /// side.
    ///
    /// NOT VERIFIED ON A DEVICE.
    /// </summary>
    public class VideoSurfaceViewRenderer : ViewRenderer<VideoSurfaceView, SurfaceView>,
        ISurfaceHolderCallback
    {
        private SurfaceView _surfaceView;

        public VideoSurfaceViewRenderer(Context context) : base(context)
        {
        }

        protected override void OnElementChanged(ElementChangedEventArgs<VideoSurfaceView> e)
        {
            base.OnElementChanged(e);

            if (e.NewElement != null && Control == null)
            {
                _surfaceView = new SurfaceView(Context);
                _surfaceView.Holder?.AddCallback(this);
                SetNativeControl(_surfaceView);
            }

            if (e.OldElement != null && e.NewElement == null)
            {
                Detach();
            }
        }

        public void SurfaceCreated(ISurfaceHolder holder)
        {
            try
            {
                Decoder?.AttachSurface(holder?.Surface);
            }
            catch (Exception ex)
            {
                Console.WriteLine("VideoSurfaceViewRenderer.SurfaceCreated: " + ex.Message);
            }
        }

        public void SurfaceChanged(ISurfaceHolder holder, Android.Graphics.Format format, int width, int height)
        {
            // Re-attach on a size or format change. The decoder treats a repeat attach as a
            // reconfigure, which is the correct response to the Surface being replaced.
            SurfaceCreated(holder);
        }

        public void SurfaceDestroyed(ISurfaceHolder holder)
        {
            // Must tear down synchronously. A codec still configured against a destroyed
            // Surface crashes the process rather than dropping frames.
            Detach();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Detach();
                _surfaceView?.Holder?.RemoveCallback(this);
                _surfaceView = null;
            }
            base.Dispose(disposing);
        }

        private void Detach()
        {
            try
            {
                Decoder?.DetachSurface();
            }
            catch (Exception ex)
            {
                Console.WriteLine("VideoSurfaceViewRenderer.Detach: " + ex.Message);
            }
        }

        /// <summary>The single shared decoder instance. Resolved each time rather than
        /// cached, because the renderer can outlive a DependencyService reset.</summary>
        private static ScreenDecoder Decoder
        {
            get { return DependencyService.Get<IScreenDecoder>() as ScreenDecoder; }
        }
    }
}
