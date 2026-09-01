// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.Net.Wifi.P2p;
using Febris.MobileCompanionV3.Droid.Utilities.EventHandlers;
using System;
using System.Threading.Tasks;

namespace Febris.MobileCompanionV3.Droid.Networking.WiFi
{
    internal class FebrisGroupInfoListener : Java.Lang.Object, WifiP2pManager.IGroupInfoListener
    {
        public void OnGroupInfoAvailable(WifiP2pGroup group)
        {
            if (group != default)
            {
                Console.WriteLine("group data: " + group);
                // ResolveServerIdentity's live-group source and the pairing confirm path read
                // WifiGroup. Nothing on this head ever assigned it (the line below sat commented
                // out since the import), so that source was dead code and a data-cleared device
                // could never key its pairing secret: the ceremony completed and the save was
                // refused with the Server already committed. Both statics are written until the
                // readers converge on one.
                WiFiStaticDetails.WifiGroup = group;
                WiFiStaticDetails.WifiGroupInfo = group;
                //WiFiService.wifiService.GroupConnectionStatus(group);

                //EventHandlerHelper._eventHandler.RefreshConnectSuccessChanged(WiFiStaticDetails.WifiGroup, WiFiStaticDetails.networkInfo);

                //if (WiFiStaticDetails.networkInfo == default)
                //{
                //    Task.Run(() => ConnectToGroupOwnerFallback());
                //}

                ///********************IS is where I was previously getting connection information**********************
                EventHandlerHelper._eventHandler.RefreshConnectSuccessChanged(WiFiStaticDetails.WifiGroupInfo, WiFiStaticDetails.networkInfo);
            }
            //else
            //{
            //    WiFiService.wifiService.ListenForServices();                
            //}
        }


        //internal static async Task  ConnectToGroupOwnerFallback()
        //{
        //    Task.Run(() => FebrisConnectionInfoListener.ConnectToGroupOwner());
        //}

        //internal void ConnectToGroupOwner(WifiP2pGroup group)
        //{
        //    try
        //    {
        //        while ()
        //        {
        //            try
        //            {
        //                WiFiService.EndConnectionDiscovery();
        //                Task.Run(() => WiFiP2pServer.wiFiP2pServer.ClientSocketCreation());
        //                Task.Delay(5000);
        //            }
        //            catch (Exception ex)
        //            {
        //                Console.WriteLine("Group Info Listener: " + ex.Message);
        //                Console.WriteLine("Group Info Listener: " + ex.StackTrace);
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("Group Info Listener: " + ex.Message);
        //        Console.WriteLine("Group Info Listener: " + ex.StackTrace);
        //    }

        //}       
    }
}