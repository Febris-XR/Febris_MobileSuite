// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
//using Android.App;
//using Android.Content;
//using Android.Hardware.Usb;
//using Android.OS;
//using Android.Runtime;
//using Android.Views;
//using Android.Widget;
//using Febris.MobileServerV3.Droid.Networking.USB;
//using Febris.MobileServerV3.P2pCommunication.USB;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//[assembly: Xamarin.Forms.Dependency(typeof(USBService))]
//namespace Febris.MobileServerV3.Droid.Networking.USB
//{
//    public class USBService: IUsbService
//    {
//        //private readonly UsbManager _usbManager;//=USBStaticDetails._usbManager;
//        //private readonly UsbInterface _interface;// = USBStaticDetails._interface;
//        ////private 
//        //public USBService()
//        //{
//        //    _usbManager = USBStaticDetails._usbManager;
//        //    _interface = USBStaticDetails._interface;
//        //}

//        public async Task HostCreationFactory()
//        {
//            //UsbHost serverSocket = null;
//            //Socket socket = null;
//            try
//            {
//                while (true)
//                {
//                    try
//                    {

//                        //if (serverSocket == null)
//                        //{
//                        //    serverSocket = GenerateFreePort();
//                        //}
//                        //socket = await serverSocket.AcceptAsync();

//                        //Task.Run(() => SocketReceiver(socket));
//                        await Task.Delay(1000);
//                    }
//                    catch (Exception ex)
//                    {
//                        Console.WriteLine("error on recieved data: " + ex.Message);
//                        Console.WriteLine("error on recieved data: " + ex.StackTrace);
//                        //socket.Close();
//                        //socket.Dispose();
//                    }
//                    finally
//                    {
//                        //socket = null;
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine("error on recieved data: " + ex.Message);
//                Console.WriteLine("error on recieved data: " + ex.StackTrace);
               

//            }
//            finally
//            {
//            }
//        }

       
//    }
//}