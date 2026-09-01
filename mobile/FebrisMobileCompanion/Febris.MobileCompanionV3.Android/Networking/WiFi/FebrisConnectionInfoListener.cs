// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.App;
using Android.Content;
using Android.Net.Wifi.P2p;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Febris.MobileCompanionV3.Droid.Utilities;
using Febris.MobileCompanionV3.Droid.Utilities.EventHandlers;
using Febris.MobileCompanionV3.Resources;
using Java.Net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Febris.MobileCompanionV3.Droid.Networking.WiFi
{
    /// <summary>
    /// from Example https://github.com/Brighthui/WifiP2P/blob/master/WifiP2P/WifiP2P.Android/WiFi/WiFiDirectBroadcastReceiver.cs
    /// </summary>
    public class FebrisConnectionInfoListener : Java.Lang.Object, WifiP2pManager.IConnectionInfoListener
    {
        public static WiFiService wifiService;
        internal static bool inUse = false;
        //internal static bool connected = false;

        //so this is looking to see if the connection is made I think. Now that it is made it can be used to communicate
        public void OnConnectionInfoAvailable(WifiP2pInfo info)
        {
            try
            {
                WiFiStaticDetails.HostInfo = info;
                //WiFiService.wifiService.GroupConnectionStatus(info)
                Console.WriteLine("Connection Info Listener: " + info);

                // RESTART DISCOVERY ONLY IF THE GROUP IS ACTUALLY GONE.
                //
                // This used to be `if (GroupFormed && !inUse) connect; else RestartDiscovery;`,
                // which lumped two completely different situations into the else: "the group is
                // gone" and "the group is fine but a connect attempt is already running". The
                // second one is normal and must be left alone.
                //
                // Why it mattered. RestartConnectionDiscovery sets RunPeerDiscovery and
                // RunServiceListener true, and StatusUpdateLoop idles on `continue` for as long as
                // both are true (LoopLogic.cs). The loop-start guard _loopsStarted is one-way by
                // design, because the loops are meant to idle rather than exit, so nothing ever
                // restarts them. Net effect: one stray connection-info callback arriving while a
                // connect was in flight left a device with a WORKING socket, still sending
                // _initalize, that never sent another status update for the life of the process.
                //
                // Observed 2026-07-29 during the module-distribution run: "upload loops already
                // running, not starting a second pair" with _initalize flowing and zero
                // _statusUpdate for ten minutes. Because the Server only scans for needed modules
                // when a status update arrives, that silently stalled module distribution too.
                //
                // This callback fires more than once per connect, and the fault C fix in
                // ReconnectingSocketToServer deliberately added another RequestConnectionInfo
                // call, so the window got wider rather than narrower. Hence the split.
                if (info != null && info.GroupFormed)
                {
                    if (inUse == false)
                    {
                        Task.Run(() => ConnectToGroupOwner());
                    }
                    else
                    {
                        // A connect attempt owns this already. Touching the discovery flags here
                        // is what used to silence the status-update loop.
                        Console.WriteLine("connection info: group is formed and a connect attempt "
                            + "is already running, leaving discovery alone");
                    }
                }
                else
                {
                    Console.WriteLine("connection info: no group formed, restarting discovery");
                    EventHandlerHelper._eventHandler.RestartConnectionDiscovery();
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Connection Info Listener: " + ex.Message);
                Console.WriteLine("Connection Info Listener: " + ex.StackTrace);
            }
        }

        internal async static Task ConnectToGroupOwner()
        {
            inUse = true;
            bool connected = false;
            try
            {
                //lock (this) {
                while (!connected)
                {
                    try
                    {
                        if (
                            WiFiP2pServer._clientSocketThread != null &&
                            WiFiP2pServer._clientSocketThread._socket.IsBound
                            && WiFiP2pServer._clientSocketThread._socket.IsConnected
                            && !WiFiP2pServer._clientSocketThread._socket.IsClosed
                            )
                        {
                            connected = true;
                            //*************************Moved this from FebrisGroupInfoListener because it would trigger either too soon or only intermittenantly send back host info***************
                            //Did't work any better
                            //EventHandlerHelper._eventHandler.RefreshConnectSuccessChanged(WiFiStaticDetails.WifiGroupInfo, WiFiStaticDetails.networkInfo);
                            break;
                        }


                        //connected = Task.Run(async()=>await WiFiP2pServer.wiFiP2pServer.ClientSocketCreation().ConfigureAwait(false)).Result;
                        connected = await WiFiP2pServer.wiFiP2pServer.ClientSocketCreation().ConfigureAwait(false);
                        //connected = WiFiP2pServer.wiFiP2pServer.ClientSocketCreation().Result;
                        if (connected)
                        {
                            EventHandlerHelper._eventHandler.EndConnectionDiscovery();
                        }
                        else
                        {
                            await Task.Delay(LocalHardwareStaticDetails.GroupOwnerConnectAttemptLoopFrequency);
                        }

                    }
                    catch (Exception ex)
                    {
                        //Console.WriteLine("ConnectToGroupOwner Loop: " + ex.Message);
                        Console.WriteLine("ConnectToGroupOwner Loop Error: " + ex.Message);
                        break;
                    }
                }
                //}
            }
            catch (Exception ex)
            {
                //Console.WriteLine("ConnectToGroupOwner: " + ex.Message);
                Console.WriteLine("ConnectToGroupOwner Loop Error: " + ex.StackTrace);
            }
            finally
            {
                inUse = false;
            }

        }
    }
}