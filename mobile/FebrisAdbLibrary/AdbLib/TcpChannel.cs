// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
//using Febris.AdbLibrary.Interface;
//using Java.IO;
//using Java.Lang;
//using System;
//using System.Collections.Generic;
//using System.Net.Sockets;
//using System.Text;

//namespace Febris.AdbLibrary.AdbLib
//{
//    public class TcpChannel : AdbChannel
//    {

//        /** The underlying socket that this class uses to communicate with the target device. */
//        private Socket socket;

//        /**
//         * The input stream that this class uses to read from the socket.
//         */
//        private InputStream inputStream;

//        /**
//         * The output stream that this class uses to read from the socket.
//         */
//        private OutputStream outputStream;



//        public void readx(byte[] buffer, int length) ////throws IOException
//        {

//            int dataRead = 0;
//            do
//            {
//                int bytesRead = inputStream.Read(buffer, dataRead, length - dataRead);

//                if (bytesRead < 0)
//                    throw new IOException("Stream closed");
//                else
//                    dataRead += bytesRead;
//            }
//            while (dataRead < length);
//        }

//        private void writex(byte[] buffer) ////throws IOException
//        {
//            outputStream.Write(buffer);
//            outputStream.Flush();
//        }

//        //@Override
//        public void writex(AdbMessage message)// //throws IOException
//        {
//            writex(message.getMessage());
//            if (message.getPayload() != null)
//            {
//                writex(message.getPayload());
//            }
//        }

//        //@Override
//        public void close() //throws IOException
//        {
//            socket.Close();//.close();
//        }

//        public TcpChannel(Socket socket)
//        {
//            try
//            {
//                /* Disable Nagle because we're sending tiny packets */
//                socket.setTcpNoDelay(true);

//                this.socket = socket;
//                this.inputStream = socket.getInputStream();
//                this.outputStream = socket.getOutputStream();


//            }
//            catch (IOException e)
//            {
//                throw new RuntimeException(e);
//            }
//        }
//    }
//}
