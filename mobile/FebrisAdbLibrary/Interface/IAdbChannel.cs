// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.Hardware.Usb;
using Android.Util;
using Febris.AdbLibrary.AdbLib;
using Java.Interop;
using Java.IO;
using System;
using System.Threading.Tasks;

namespace Febris.AdbLibrary.Interface
{
    public interface IAdbChannel
    {
        //UsbEndpoint _EndPoint_Host_To_Device { get; set; }

        Task Writex(AdbMessage message);        
        void Close();
        byte[] Readx(int length);
    }
    //public class AdbChannel : Java.Lang.Object, ICloseable
    //{
    //    public LogPrinter _logger = new Android.Util.LogPrinter(LogPriority.Debug, "Febris.AdbLibrary");

    //    public void Readx(byte[] buffer, int length) => throw new IOException();

    //    public void Writex(AdbMessage message) => throw new IOException();


    //    public void Close()
    //    {
    //        throw new NotImplementedException();
    //    }


    //    #region NOT Implemented
    //    //public IntPtr Handle => throw new NotImplementedException();

    //    //public int JniIdentityHashCode => throw new NotImplementedException();

    //    //public JniObjectReference PeerReference => throw new NotImplementedException();

    //    //public JniPeerMembers JniPeerMembers => throw new NotImplementedException();

    //    //public JniManagedPeerStates JniManagedPeerState => throw new NotImplementedException();

    //    //public void Dispose()
    //    //{
    //    //    throw new NotImplementedException();
    //    //}

    //    //public void Disposed()
    //    //{
    //    //    throw new NotImplementedException();
    //    //}

    //    //public void DisposeUnlessReferenced()
    //    //{
    //    //    throw new NotImplementedException();
    //    //}

    //    //public void Finalized()
    //    //{
    //    //    throw new NotImplementedException();
    //    //}

    //    //public void SetJniIdentityHashCode(int value)
    //    //{
    //    //    throw new NotImplementedException();
    //    //}

    //    //public void SetJniManagedPeerState(JniManagedPeerStates value)
    //    //{
    //    //    throw new NotImplementedException();
    //    //}

    //    //public void SetPeerReference(JniObjectReference reference)
    //    //{
    //    //    throw new NotImplementedException();
    //    //}

    //    //public void UnregisterFromRuntime()
    //    //{
    //    //    throw new NotImplementedException();
    //    //}
    //    #endregion

    //}
}
