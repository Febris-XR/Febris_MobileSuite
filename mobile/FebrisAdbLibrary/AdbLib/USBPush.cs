// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Text;

//namespace Febris.AdbLibrary.AdbLib
//{
//    public class USBPush : Java.Lang.Object
//    {
//        private AdbConnection adbConnection;

//        private String remotePath;

//        public USBPush(AdbConnection adbConnection, string remotePath)
//        {
//            this.adbConnection = adbConnection;
//            this.remotePath = remotePath;
//        }

//        public void execute()//Handler handler)// throws InterruptedException, IOException 
//        {

//            AdbStream stream = adbConnection.Open("sync:");

//            String sendId = "SEND";

//            String mode = ",33206";

//            int length = (remotePath + mode).Length;

//            //stream.Write(ByteUtils.concat(sendId.getBytes(), ByteUtils.intToByteArray(length)));

//            //stream.Write(remotePath.GetBytes());

//            //stream.Write(mode.GetBytes());

//            //byte[] buff = new byte[adbConnection.getMaxData()];
//            //InputStream is = new FileInputStream(local);

//            //long sent = 0;
//            //long total = local.length();
//            //int lastProgress = 0;
//            //while (true)
//            //{
//            //    int read = is.read(buff);
//            //    if (read < 0)
//            //    {
//            //        break;
//            //    }

//            //    stream.write(ByteUtils.concat("DATA".getBytes(), ByteUtils.intToByteArray(read)));

//            //    if (read == buff.length)
//            //    {
//            //        stream.write(buff);
//            //    }
//            //    else
//            //    {
//            //        byte[] tmp = new byte[read];
//            //        System.arraycopy(buff, 0, tmp, 0, read);
//            //        stream.write(tmp);
//            //    }

//            //    sent += read;

//            //    int progress = (int)(sent * 100 / total);
//            //    if (lastProgress != progress)
//            //    {
//            //        //handler.sendMessage(handler.obtainMessage(Message.INSTALLING_PROGRESS, Message.PUSH_PART, progress));
//            //        lastProgress = progress;
//            //    }

//            //}

//            //stream.write(ByteUtils.concat("DONE".getBytes(), ByteUtils.intToByteArray((int)System.currentTimeMillis())));

//            //byte[] res = stream.read();
//            //// TODO: test if res contains "OKEY" or "FAIL"
//            //Log.d(Const.TAG, new String(res));

//            //stream.write(ByteUtils.concat("QUIT".getBytes(), ByteUtils.intToByteArray(0)));
//        }
//    }
//}
