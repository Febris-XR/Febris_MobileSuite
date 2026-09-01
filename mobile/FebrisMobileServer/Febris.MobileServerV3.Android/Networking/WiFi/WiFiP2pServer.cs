// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Febris.MobileServerV3.Droid.Networking.WiFi;
using Febris.MobileServerV3.MVVM.ViewModel;
using Febris.MobileServerV3.P2pCommunication.WiFi;
using Febris.SharedMobileLibrary.Models;
using Java.Net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[assembly: Xamarin.Forms.Dependency(typeof(WiFiP2pServer))]
namespace Febris.MobileServerV3.Droid.Networking.WiFi
{
    public class WiFiP2pServer : IWiFiP2pServer
    {
        #region variables and constructors
        public static WiFiService wifiService;
        public static WiFiP2pServer wiFiP2pServer;

        //IWiFiService wifi = DependencyService.Get<IWiFiService>();

        public WiFiP2pServer()
        {
            wiFiP2pServer = this;
            //wifiService = new WiFiService();
        }
        #endregion

        // MP2P-9: [Historical] block preserved under a relabeled region. These helpers
        // came from an earlier socket-API attempt (raw ServerSocket / String<->Bytes utilities
        // tried before the v2 framer landed in MP2P-1/2). All members are commented out and
        // have no live callers. Kept in-file rather than deleted so future readers can see
        // the boundary between the abandoned approach and the live one -- collapse the region
        // in the IDE to hide it during normal navigation.
        #region [Historical - MP2P-9] Helpers from earlier socket-API attempt

        //#region Socket
        //public static ServerSocket GenerateFreePort()
        //{
        //    ServerSocket serverSocket = new ServerSocket();
        //    try
        //    {
        //        serverSocket = new ServerSocket(WiFiStaticDetails.FebrisSocket);
        //        //serverSocket.ReuseAddress = false;
        //        serverSocket.ReuseAddress = true;
        //        return serverSocket;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("Error generating server socket: " + ex.Message);
        //        Console.WriteLine("Error generating server socket: " + ex.StackTrace);
        //        serverSocket.Close();
        //        //serverSocket.Dispose();
        //        throw;
        //    }

        //}
        //#endregion

        //#region encoding/decoding
        //#region encoding 
        //public static byte[] String2Bytes(string input)
        //{
        //    return Encoding.ASCII.GetBytes(input);
        //}

        ////internal async static Task<byte[]> Encode<T>(string input, BodyType type)
        ////{
        ////    byte[] output = { };

        ////    try
        ////    {
        ////        if (type == BodyType._module)
        ////        {
        ////            using (FileStream fileStream = new FileStream(input, FileMode.Open))
        ////            {
        ////                using (MemoryStream memoryStream = new MemoryStream())
        ////                {
        ////                    await fileStream.CopyToAsync(memoryStream);
        ////                    output = memoryStream.ToArray();
        ////                }
        ////            }
        ////        }
        ////        else if (type == BodyType._statement)
        ////        {
        ////            output = Encoding.ASCII.GetBytes(input);
        ////        }
        ////        else
        ////        {
        ////            throw new Exception();
        ////        }



        ////    }
        ////    catch (Exception ex)
        ////    {
        ////        Console.WriteLine(ex.StackTrace);
        ////    }

        ////    return output;
        ////}

        ////internal async static Task<byte[]> Encode<T>(string input)
        ////{
        ////    byte[] output = { };
        ////    output = Encoding.ASCII.GetBytes(input);
        ////    return output;
        ////}

        //////internal static (byte[] header, byte[] body) Encode<T>(T input)
        //////{
        //////    byte[] header = { };
        //////    byte[] body = { };

        //////    return (header, body);
        //////}

        ////public Stream GenerateStreamFromString(string s)
        ////{
        ////    MemoryStream stream = new MemoryStream();
        ////    StreamWriter writer = new StreamWriter(stream);
        ////    writer.Write(s);
        ////    writer.Flush();
        ////    stream.Position = 0;
        ////    return stream;
        ////}

        ////internal static byte[] SocketHeaderEncoder(string input)
        ////{
        ////    byte[] output = new byte[WiFiStaticDetails.HeaderLength];
        ////    output = String2Bytes(input);
        ////    return output;
        ////}
        //internal static byte[] SocketHeaderEncoder(int input)
        //{
        //    byte[] output = new byte[WiFiStaticDetails.ExpectedHeaderLength];
        //    byte[] buffer = Int2Bytes(input);
        //    buffer.CopyTo(output, 0);
        //    return output;
        //}

        //public static byte[] Int2Bytes(int input)
        //{
        //    return BitConverter.GetBytes(input);
        //}

        //#endregion

        //#region decoding

        //public static int Bytes2Int(byte[] input)
        //{
        //    return BitConverter.ToInt32(input, 0);
        //}
        //public static string Bytes2String(byte[] input)
        //{
        //    return Encoding.Default.GetString(input);
        //}

        //////internal static T Decode<T>(byte[] input)
        //////{
        //////    var output;// = Encoding.Default(input);

        //////    return output;
        //////}
        ////internal static string Decode(byte[] input)
        ////{
        ////    var output = Encoding.Default.GetString(input);
        ////    return output;
        ////}

        //////internal static File Decode(byte[] input)
        //////{
        //////    var output;// = Encoding.Default(input);

        //////    return output;
        //////}

        //////internal async static Task<byte[]> Encode<T>(T input)
        //////{
        //////    byte[] output = { };
        //////    //output = Encoding.ASCII.GetBytes(input);
        //////    var xs = new asciiSerializer(typeof(T));


        //////    return output;
        //////}

        ////internal async static Task<T> decode<T>(byte[] input, BodyType type)
        ////{
        ////    T output = (T)(object)null;
        ////    try
        ////    {
        ////        if (type == BodyType._video)
        ////        {
        ////            output = (T)(object)Bytes2String(input);

        ////            //using (FileStream fileStream = new FileStream(input, FileMode.Open))
        ////            //{
        ////            //    using (MemoryStream memoryStream = new MemoryStream())
        ////            //    {
        ////            //        await fileStream.CopyToAsync(memoryStream);
        ////            //        output = memoryStream.ToArray();
        ////            //    }
        ////            //}
        ////        }
        ////        else if (type == BodyType._statement)
        ////        {
        ////            output = (T)(object)Bytes2String(input);                    
        ////        }
        ////        else if (type == BodyType._genericString)
        ////        {
        ////            output = (T)(object)Bytes2String(input);
        ////        }
        ////        else if (type == BodyType._status)
        ////        {                    
        ////            output = (T)(object)Bytes2String(input);
        ////        }
        ////        else
        ////        {
        ////            throw new Exception();
        ////        }
        ////    }
        ////    catch (Exception ex)
        ////    {
        ////        Console.WriteLine(ex.StackTrace);
        ////    }

        ////    return output;
        ////}
        //#endregion

        //#region Header
        //public static PacketHeaderModel ParsingJsonStringToHeaderModel(string input)
        //{
        //    PacketHeaderModel output = new PacketHeaderModel();
        //    try
        //    {
        //        output = (PacketHeaderModel)JsonConvert.DeserializeObject(input);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.StackTrace);
        //    }
        //    return output;
        //}
        //public static PacketHeaderModel ParsingByteArrayToHeaderModel(byte[] input)
        //{
        //    PacketHeaderModel output = new PacketHeaderModel();
        //    string jsonString = string.Empty;
        //    try
        //    {
        //        jsonString = Bytes2String(input);
        //        output = (PacketHeaderModel)JsonConvert.DeserializeObject(jsonString);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.StackTrace);
        //    }
        //    return output;
        //}
        //public static byte[] HeaderModelToByteArray(PacketHeaderModel input)
        //{
        //    byte[] output = { };
        //    string jsonString = string.Empty;
        //    try
        //    {
        //        jsonString = JsonConvert.SerializeObject(input);
        //        output = String2Bytes(jsonString);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.StackTrace);
        //    }
        //    return output;
        //}
        //public static string HeaderModelToJsonString(PacketHeaderModel input)
        //{
        //    string output = string.Empty;
        //    output = JsonConvert.SerializeObject(input);
        //    return output;
        //}

        ////pull out header

        //#endregion

        //#endregion

        #endregion

        #region socket Handling
        /// <summary>
        /// Create sockets
        /// </summary>
        /// <returns></returns>
        //public async Task ServerSocketCreation()
        //{
        //    ServerSocket serverSocket = null;
        //    Socket socket = null;
        //    try
        //    {
        //        serverSocket = GenerateFreePort();
        //        //socket.Bind(null);
        //        socket = await serverSocket.AcceptAsync();
        //        //add socket to a library for use in sending?
        //        Task.Run(() => SocketReceiver(socket));
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("error on recieved data: " + ex.Message);
        //        Console.WriteLine("error on recieved data: " + ex.StackTrace);
        //        socket.Close();                
        //        serverSocket.Close();
        //        socket.Dispose();
        //        serverSocket.Dispose();
        //    }
        //    finally
        //    {
        //    }
        //}

        public async Task SocketCreationFactory()
        {
            ServerSocket serverSocket = null;
            try
            {
                while (true)
                {
                    // Per-iteration local. This used to be declared outside the loop, so the
                    // queued SocketReceiver closure captured a SHARED variable that the old
                    // finally block then set to null on the same iteration. The receiver could
                    // observe null, or a later iteration's socket.
                    Socket socket = null;
                    try
                    {
                        if (serverSocket == null || serverSocket.IsClosed)
                        {
                            serverSocket = WiFiP2pHelpers.GenerateFreePort();
                        }
                        socket = await serverSocket.AcceptAsync();

                        // Nagle would hold small control frames back waiting for more data.
                        // Set on the ACCEPTED socket: a Java ServerSocket only propagates
                        // receive-buffer size to its accepted connections, not TCP_NODELAY.
                        // Non-fatal if the platform refuses it.
                        try
                        {
                            socket.TcpNoDelay = true;
                        }
                        catch (Exception noDelayEx)
                        {
                            Console.WriteLine("could not set TcpNoDelay on accepted socket: " + noDelayEx.Message);
                        }

                        Socket accepted = socket;
                        Task.Run(() => SocketReceiver(accepted));

                        // The 30s Task.Delay(WiFiStaticDetails.DiscoveryLoopTimer) that used to
                        // sit here made a mid-stream reconnect wait up to half a minute to be
                        // accepted, which presents as the stream having died. It is not needed
                        // to keep the loop from spinning either, because AcceptAsync already
                        // blocks until a peer actually connects.
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("error on SocketCreationFactory Loop: " + ex.Message);
                        Console.WriteLine("error on SocketCreationFactory Loop: " + ex.StackTrace);

                        // Null-guarded. If AcceptAsync itself threw, socket was still null and
                        // the old unguarded socket.Close() raised a NullReferenceException out
                        // of this catch into the OUTER one, which exited the while loop and
                        // ended accepting for the rest of the process lifetime (the restart in
                        // the outer finally is commented out). One accept fault was terminal.
                        if (socket != null)
                        {
                            try
                            {
                                socket.Close();
                                socket.Dispose();
                            }
                            catch (Exception closeEx)
                            {
                                Console.WriteLine("error closing accepted socket: " + closeEx.Message);
                            }
                        }

                        // Only on the failure path, so a repeatedly-failing accept (for example
                        // a closed ServerSocket) backs off instead of spinning hot.
                        await Task.Delay(1000);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("error on SocketCreationFactory: " + ex.Message);
                Console.WriteLine("error on SocketCreationFactory: " + ex.StackTrace);

                if (serverSocket != null && serverSocket.IsClosed)
                {
                    serverSocket.Dispose();
                    serverSocket = null;
                }
                //else if (serverSocket!=default)
                //{
                //    serverSocket.Dispose();
                //    serverSocket = null;
                //}
                //else
                //{
                //    serverSocket = null;
                //}
                
            }
            finally
            {
                //Task.Run(() => WiFiP2pServer.wiFiP2pServer.SocketCreationFactory());
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataPacket"></param>
        /// <param name="device"></param>
        /// <returns></returns>
        /// <summary>
        /// Evict a connection from the send table ONLY if the entry still refers to the
        /// connection the caller is tearing down.
        ///
        /// WHY THE VALUE COMPARISON MATTERS. The table is keyed on the peer's IP address, and
        /// a reconnecting Companion comes back on the SAME address. A key-only TryRemove
        /// therefore deletes whatever currently sits under that key, which after a redial is
        /// the fresh connection rather than the dead one. SocketSender then throws
        /// KeyNotFoundException into a swallowing catch and reports not-sent for a link that
        /// is perfectly healthy, until the next inbound frame happens to re-register it.
        ///
        /// This is the Server-side twin of the Companion's static-socket race: same shape,
        /// same cause, "the object I am cleaning up may no longer be the current one".
        ///
        /// Uses the ICollection explicit implementation rather than the KeyValuePair overload
        /// of TryRemove, because that overload is not present across every framework this
        /// solution targets. Both perform the same atomic compare-and-remove.
        /// </summary>
        /// <summary>
        /// Live peers, read straight from the accept table. Entries whose socket has died are
        /// skipped rather than reported, because offering to pair with a dead connection would
        /// fail at the first frame and look like a pairing bug.
        /// </summary>
        public System.Collections.Generic.IReadOnlyList<string> ConnectedPeerAddresses()
        {
            var live = new List<string>();
            try
            {
                foreach (var entry in WiFiStaticDetails.ServerSocketDictionary)
                {
                    ServerSocketThread thread = entry.Value;
                    if (thread?._socket != null && thread._socket.IsConnected && !thread._socket.IsClosed)
                    {
                        live.Add(entry.Key);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ConnectedPeerAddresses failed: " + ex.Message);
            }
            return live;
        }

        private static bool EvictIfCurrent(string key, ServerSocketThread expected)
        {
            if (string.IsNullOrEmpty(key) || expected == null)
            {
                return false;
            }

            var asCollection =
                (ICollection<KeyValuePair<string, ServerSocketThread>>)WiFiStaticDetails.ServerSocketDictionary;
            return asCollection.Remove(new KeyValuePair<string, ServerSocketThread>(key, expected));
        }

        /// <summary>
        /// Connection-keyed send, for replying to a frame whose sender could NOT be resolved
        /// to a paired device. See IWiFiP2pServer for why this overload exists.
        ///
        /// Deliberately does NOT evict the dictionary entry on failure, unlike the device
        /// overload. A refused peer failing to receive its refusal is not evidence that the
        /// connection is dead, and tearing it down here would let an unauthorized frame
        /// influence the connection table, which is the whole class of problem the peer gate
        /// exists to stop.
        /// </summary>
        public async Task<bool> SocketSender(byte[] dataPacket, string clientIpAddress)
        {
            if (string.IsNullOrWhiteSpace(clientIpAddress))
            {
                return false;
            }

            try
            {
                string client = clientIpAddress.Replace(@"/", "");
                ServerSocketThread thread;
                if (!WiFiStaticDetails.ServerSocketDictionary.TryGetValue(client, out thread) || thread == null)
                {
                    return false;
                }
                if (thread._socket == null || !thread._socket.IsConnected || thread._socket.IsClosed)
                {
                    return false;
                }
                // BYPASSES THE SEND GATE, and must.
                //
                // This overload exists only for frames that are legitimately PRE-AUTHENTICATION:
                // a refusal NACK to a peer we just rejected, and the pairing request that opens a
                // numeric-comparison ceremony. Both are cases where no credential exists yet, by
                // definition.
                //
                // Without this the gate deadlocks pairing outright. A ServerSocketThread is a
                // handshake RESPONDER and starts in NotStarted, where AllowsApplicationTraffic is
                // false, and it only leaves that state on a RECEIVED frame. Pairing is
                // Server-initiated, so the Server would have to receive something before it could
                // send the very frame that starts the exchange. That is what produced "could not
                // reach that device" on device: the socket was healthy and the address correct,
                // and the gate refused the write.
                return await thread.WriteToSocket(dataPacket, isHandshakeFrame: true);
            }
            catch (Exception ex)
            {
                Console.WriteLine("SocketSender(ip) failed for " + clientIpAddress + ": " + ex.Message);
                return false;
            }
        }

        public async Task<bool> SocketSender(byte[] dataPacket, CompanionDeviceViewModel device)
        {
            bool sent = false;
            try
            {
                string client = device.WiFiIPAddress.Replace(@"/", "");

                // Hoisted out of the inner try so the catch below can value-compare against the
                // SAME instance it was working with. Evicting on a key alone from a catch block
                // is how a failed send against a stale handle used to delete the connection that
                // had already superseded it.
                ServerSocketThread _serverSocketThread = null;
                try
                {
                    _serverSocketThread = WiFiStaticDetails.ServerSocketDictionary[client];
                    //if (_serverSocketThread == null)
                    //{
                    //    //return sent;
                    //    //EventHandlerHelper._eventHandler.RestartConnectionDiscovery();
                    //    //EventHandlerHelper._eventHandler.RestartConnectionDiscovery();
                    //}
                    //else 
                    //if (_serverSocketThread._socket.IsClosed)
                    //{

                    //    //_serverSocketThread._socket.Dispose();
                    //    //WiFiStaticDetails.ServerSocketDictionary.Remove(client);
                    //    _serverSocketThread = null;                        
                    //    //EventHandlerHelper._eventHandler.RestartConnectionDiscovery();
                    //}
                    //else 
                    if (_serverSocketThread._socket.IsConnected && !_serverSocketThread._socket.IsClosed)
                    {
                        sent = await _serverSocketThread.WriteToSocket(dataPacket);
                    }
                    else
                    {
                        // MP2P-5: atomic removal. Now also VALUE-COMPARED: the key is the peer
                        // IP and a reconnecting Companion returns on the same address, so a
                        // key-only removal here would evict the fresh connection that replaced
                        // this dead one.
                        bool removed = EvictIfCurrent(client, _serverSocketThread);
                        Console.WriteLine("socket sender error has lead to client being removed and it's success was: " + removed);
                    }
                    Console.WriteLine("***************************socket sender was successful: " + sent.ToString());
                    
                    //else
                    //{
                    //    //?????
                    //}
                    //sent = await _serverSocketThread.WriteToSocket(dataPacket);
                    //sent = true;
                }
                catch (Exception ex)
                {
                    //Console.WriteLine(ex.StackTrace);
                    Console.WriteLine("Error on socket sender: " + ex.Message);
                    // MP2P-5: atomic + safe on a missing key. Value-compared for the same
                    // reason as above, so a failed send against a stale handle cannot take out
                    // the connection that superseded it.
                    EvictIfCurrent(client, _serverSocketThread);
                }
                return sent;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                ///I think this is here to space out requests so the companion divice is not overwhelmed
                ///Now that everything is async it may not be needed because it returns a sent value bool.
                //await Task.Delay(WiFiStaticDetails.DiscoveryLoopTimer);
                
                //await Task.Delay(5000);
            }
            return sent;
        }

        /// <summary>
        /// using a previsouly made socket
        /// </summary>
        /// <param name="socket"></param>
        /// <returns></returns>
        public async Task SocketReceiver(Socket socket)
        {
            ServerSocketThread _serverSocketThread = new ServerSocketThread(socket);
            string companionIPAddress = string.Empty;
            try
            {
                while (true)
                {
                    try
                    {
                        companionIPAddress = socket.InetAddress.ToString().Replace(@"/", "");

                        if (socket.IsClosed)
                        {
                            socket.Dispose();
                            // MP2P-5: atomic removal, and value-compared so this loop only ever
                            // retires its OWN entry. Without the comparison a socket closing here
                            // deletes whatever is registered under its address, which after a
                            // redial is the replacement connection.
                            if (EvictIfCurrent(companionIPAddress, _serverSocketThread))
                            {
                                Console.WriteLine("SocketReceiver error has lead to client being removed and it's success was: True");
                            }
                            break;
                        }
                        //else if(socket)

                        #region connection data
                        // MP2P-5: ConcurrentDictionary's indexer-set is atomic upsert (add or replace),
                        // which is exactly the prior intent. The old ContainsKey/Add/[]= dance had two
                        // race windows (between ContainsKey and the follow-up op).
                        WiFiStaticDetails.ServerSocketDictionary[companionIPAddress] = _serverSocketThread;
                        Console.WriteLine("\n client: " + socket);
                        #endregion

                        if (!_serverSocketThread._socket.IsClosed && _serverSocketThread._socket.IsConnected)
                        {
                            await _serverSocketThread.ReadFromSocket();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("error on recieved data: " + ex.Message);
                        socket.Close();
                        socket.Dispose();
                    }
                    finally
                    {
                        if (socket != null)
                        {
                            try
                            {
                                //socket.Close();
                                //socket.Dispose();
                            }
                            catch (IOException e)
                            {
                                //WiFiService.wifiService.RefreshReceivedMessage(e.Message);
                                EventHandlerHelper._eventHandler.RefreshReceivedMessage(e.Message);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("error on SocketReceiver loop: " + ex.Message);
                Console.WriteLine("error on SocketReceiver loop: " + ex.Message);
                socket.Close();
                socket.Dispose();
            }
        }
        #endregion

        // MP2P-9: [Historical] block preserved under a relabeled region. These were an
        // early `DatagramSocket` (UDP) attempt at the P2P transport before the TCP +
        // ServerSocketThread approach was settled. Both Java.Lang.Runnable bodies are
        // commented out; the file's original comment "I don't think they are actually
        // being used" was correct -- grep confirms zero live callers. Kept here to mark
        // the abandoned UDP path; do not revive without re-evaluating against the
        // current TCP design.
        #region [Historical - MP2P-9] DatagramSocket Runnable threads (UDP attempt, never used)
        //public Java.Lang.Runnable ClientThread = new Java.Lang.Runnable(async () =>
        //{
        //    DatagramSocket socket = null;
        //    InetAddress host = WiFiStaticDetails.HostInfo.GroupOwnerAddress;
        //    int port = WiFiStaticDetails.FebrisSocket;
        //    byte[] sendData;
        //    byte[] receiveData = new byte[1024];
        //    while (true)
        //    {
        //        sendData = WiFiService.wifiService.String2Bytes(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        //        try
        //        {
        //            if (socket == null)
        //            {
        //                socket = new DatagramSocket(port);
        //            }
        //        }
        //        catch (Java.Lang.Exception)
        //        {
        //            throw;
        //        }
        //        //Client Send
        //        try
        //        {
        //            DatagramPacket packetSend = new DatagramPacket(sendData, sendData.Length, host, port);
        //            socket.Send(packetSend);
        //        }
        //        catch (Java.Lang.Exception)
        //        {
        //            throw;
        //        }
        //        await Task.Delay(5000);
        //        //Client Receive
        //        try
        //        {
        //            DatagramPacket packetReceive = new DatagramPacket(receiveData, receiveData.Length);
        //            socket.Receive(packetReceive);
        //        }
        //        catch (Java.Lang.Exception)
        //        {
        //            throw;
        //        }
        //    }
        //});

        //public Java.Lang.Runnable ServerThread = new Java.Lang.Runnable(() =>
        //{
        //    DatagramSocket socket = null;
        //    InetAddress client = null;
        //    int port = WiFiStaticDetails.FebrisSocket;
        //    byte[] sendData = new byte[1024];
        //    byte[] receiveData = new byte[1024];
        //    while (true)
        //    {
        //        try
        //        {
        //            if (socket == null)
        //            {
        //                socket = new DatagramSocket(port);
        //            }
        //        }
        //        catch (Java.Lang.Exception)
        //        {
        //            throw;
        //        }
        //        DatagramPacket receivePacket = new DatagramPacket(receiveData, receiveData.Length);
        //        //Server Receive, have to receive first to know clinet address
        //        try
        //        {
        //            socket.Receive(receivePacket);
        //            receiveData = receivePacket.GetData();
        //            WiFiService.wifiService.RefreshReceivedMessage(WiFiService.wifiService.Bytes2String(receiveData));
        //            if (client == null)
        //                client = receivePacket.Address;
        //        }
        //        catch (Java.Lang.Exception)
        //        {
        //            throw;
        //        }
        //        //Server Send
        //        try
        //        {
        //            if (client != null)
        //            {
        //                DatagramPacket packetSend = new DatagramPacket(sendData, 0, sendData.Length, client, port);
        //                socket.Send(packetSend);
        //            }
        //        }
        //        catch (Java.Lang.Exception)
        //        {
        //            throw;
        //        }
        //    }

        //});
        #endregion
    }

    // MP2P-9: [Historical] block preserved under a region. Below is a commented-out
    // duplicate of `ServerSocketThread` that previously lived in this file before the
    // live implementation was extracted to `ServerSocketThread.cs`. The duplicate is
    // kept on disk so future readers can see where the class used to live + how the
    // earlier inline implementation differed from the extracted one. The live class
    // is the one in `ServerSocketThread.cs`; do NOT uncomment this block.
    #region [Historical - MP2P-9] Duplicate ServerSocketThread class - live impl in ServerSocketThread.cs
    //public class ServerSocketThread
    //{
    //    internal Stream _inputStream;
    //    internal Stream _outputStream;
    //    internal Socket _socket;

    //    public ServerSocketThread(Socket socket)
    //    {
    //        _socket = socket;
    //        _inputStream = socket.InputStream;
    //        _outputStream = socket.OutputStream;
    //    }

    //    public async Task WriteToSocket(byte[] dataPackage)
    //    {
    //        //var buf = new byte[1024];
    //        Stream inputStream = new MemoryStream(dataPackage);
    //        Stream outputStream = _outputStream;
    //        try
    //        {
    //            int n;
    //            while ((n = await inputStream.ReadAsync(dataPackage, 0, dataPackage.Length)) > 0)
    //            {
    //                await outputStream.WriteAsync(dataPackage, 0, n);
    //            }
    //            inputStream.Close();
    //            inputStream.Dispose();
    //        }
    //        catch (Exception ex)
    //        {
    //            Console.WriteLine("Error at WriteToSocket: " + ex.Message);
    //            Console.WriteLine("Error at WriteToSocket: " + ex.StackTrace);
    //            _socket.Close();
    //            _socket.Dispose();
    //        }
    //        finally
    //        { }
    //    }

    //    public async Task ReadFromSocket()
    //    {
    //        byte[] dataPackage = { };
    //        var buf = new byte[1024];
    //        MemoryStream outputStream = new MemoryStream();
    //        Stream inputStream = _inputStream;
    //        try
    //        {
    //            int n;
    //            #region this breaks the system
    //            //if (inputStream.IsDataAvailable())
    //            //{
    //            //    while ((n = await inputStream.ReadAsync(buf, 0, buf.Length)) > 0)
    //            //    {
    //            //        await outputStream.WriteAsync(buf, 0, n);
    //            //        //if (!inputStream.IsDataAvailable())
    //            //        //{
    //            //        //    break;
    //            //        //}
    //            //        //if (n < 1024)
    //            //        //{
    //            //        //    break;
    //            //        //}
    //            //    }
    //            //    dataPackage = outputStream.ToArray();
    //            //    //Task.Run(() => WiFiService.wifiService.ProcessDownloadedData(_socket.InetAddress.ToString().Replace(@"/", ""), dataPackage));
    //            //    Task.Run(() => EventHandlerHelper._eventHandler.ProcessDownloadedData(_socket.InetAddress.ToString().Replace(@"/", ""), dataPackage));

    //            //}
    //            //outputStream.Close();
    //            //   outputStream.Dispose();
    //            #endregion
    //            #region This works but is a little messy
    //            while ((n = await inputStream.ReadAsync(buf, 0, buf.Length)) > 0)
    //            {
    //                await outputStream.WriteAsync(buf, 0, n);
    //                if (!inputStream.IsDataAvailable())
    //                {
    //                    break;
    //                }
    //            }
    //            if (n <= 0)
    //            {
    //                inputStream.Close();
    //                //inputStream.Dispose();
    //                //EventHandlerHelper._eventHandler.RestartConnectionDiscovery();
    //                //_socket.Close();
    //                //_socket.Dispose();
    //            }
    //            dataPackage = outputStream.ToArray();
    //            Task.Run(() => EventHandlerHelper._eventHandler.ProcessDownloadedData(_socket.InetAddress.ToString().Replace(@"/", ""), dataPackage));
    //            outputStream.Close();
    //            outputStream.Dispose();
    //            #endregion
    //            //if (n <= 0)
    //            //{
    //            //    inputStream.Close();
    //            //    inputStream.Dispose();
    //            //    //_socket.Close();
    //            //    //_socket.Dispose(); 
    //            //}
    //            //if (inputStream.ReadByte() == -1)
    //            //{
    //            //    _socket.Close();
    //            //    _socket.Dispose();                    
    //            //}
    //            //dataPackage = outputStream.ToArray();
    //            ////Task.Run(() => WiFiService.wifiService.ProcessDownloadedData(_socket.InetAddress.ToString().Replace(@"/", ""), dataPackage));
    //            //Task.Run(() => EventHandlerHelper._eventHandler.ProcessDownloadedData(_socket.InetAddress.ToString().Replace(@"/", ""), dataPackage));
    //            //outputStream.Close();
    //            //outputStream.Dispose();
    //        }
    //        catch (Exception ex)
    //        {
    //            Console.WriteLine("Error at ReadFromSocket: " + ex.Message);
    //            Console.WriteLine("Error at ReadFromSocket: " + ex.StackTrace);
    //            _socket.Close();
    //            _socket.Dispose();
    //        }
    //        finally
    //        { }
    //    }
    //}
    #endregion // [Historical - MP2P-9] Duplicate ServerSocketThread class
}