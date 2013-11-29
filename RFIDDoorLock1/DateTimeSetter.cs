// RFID Door Control Software
// Rob Dobson 2012-2013

using System;
using Microsoft.SPOT;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace RFIDDoorLock1
{
    class DateTimeSetter
    {
        DateTimeSetter()
        {
        }

        public static void SetTimeFromNTPWhenPossible()
        {
            // Start a thread to try to get time
            Thread t = new Thread(new ThreadStart(GetNetworkTime));
            t.Start();
        }

        private static void GetNetworkTime()
        {
            IPEndPoint ep = new IPEndPoint(Dns.GetHostEntry("0.uk.pool.ntp.org").AddressList[0], 123);

            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            s.Connect(ep);

            byte[] ntpData = new byte[48]; // RFC 2030
            ntpData[0] = 0x1B;
            for (int i = 1; i < 48; i++)
                ntpData[i] = 0;

            s.Send(ntpData);
            s.Receive(ntpData);

            byte offsetTransmitTime = 40;
            ulong intpart = 0;
            ulong fractpart = 0;
            for (int i = 0; i <= 3; i++)
                intpart = 256 * intpart + ntpData[offsetTransmitTime + i];

            for (int i = 4; i <= 7; i++)
                fractpart = 256 * fractpart + ntpData[offsetTransmitTime + i];

            ulong milliseconds = (intpart * 1000 + (fractpart * 1000) / 0x100000000L);

            s.Close();

            TimeSpan timeSpan = TimeSpan.FromTicks((long)milliseconds * TimeSpan.TicksPerMillisecond);
            DateTime dateTime = new DateTime(1900, 1, 1);
            dateTime += timeSpan;

            TimeSpan offsetAmount = TimeZone.CurrentTimeZone.GetUtcOffset(dateTime);
            DateTime networkDateTime = (dateTime + offsetAmount);

            Debug.Print("DateTimeSetter setting to " + networkDateTime.ToString("MM-dd-yyyy HH:mm:ss"));

            Microsoft.SPOT.Hardware.Utility.SetLocalTime(networkDateTime);
        }
    }
}
