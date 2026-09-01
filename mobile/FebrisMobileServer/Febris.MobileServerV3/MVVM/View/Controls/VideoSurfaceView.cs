// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Xamarin.Forms;

namespace Febris.MobileServerV3.MVVM.View.Controls
{
    /// <summary>
    /// A Forms view backed by a native SurfaceView, for displaying the decoded Companion
    /// screen stream.
    ///
    /// It carries no data-bound frame property on purpose. The old path bound an ImageSource
    /// and replaced it per frame, which forced a full PNG decode plus a layout pass for
    /// every picture. MediaCodec renders straight into this view's Surface, so frames never
    /// pass through Forms at all and there is nothing here to bind.
    ///
    /// The Android renderer hands the Surface to IScreenDecoder when it becomes available
    /// and revokes it on teardown, which is why the decoder exposes IsReady separately.
    ///
    /// There were no custom renderers anywhere on the Server head before this, so there is
    /// no in-repo precedent for the pattern.
    /// </summary>
    public class VideoSurfaceView : Xamarin.Forms.View
    {
    }
}
