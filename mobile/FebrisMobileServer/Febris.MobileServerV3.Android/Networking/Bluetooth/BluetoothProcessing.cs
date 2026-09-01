// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Febris.MobileServerV3.Droid.Networking.Bluetooth
{
    public class BluetoothProcessing
    {
        public void RefreshBTState(State state)
        {
            switch (state)
            {
                case State.Disconnected:
                    {
                        break;
                    }
                case State.Connecting:
                    {
                        break;
                    }
                case State.Connected:
                    {
                        break;
                    }
                case State.Disconnecting:
                    {
                        break;
                    }
                case State.Off:
                    {
                        break;
                    }
                case State.TurningOn:
                    {
                        break;
                    }
                case State.On:
                    {
                        break;
                    }
                case State.TurningOff:
                    {
                        break;
                    }
                default:
                    {

                        break;
                    }
            }
        }



    }
}