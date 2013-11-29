// RFID Door Control Software
// Rob Dobson 2012-2013

using System;
using Microsoft.SPOT;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace RFIDDoorLock1
{
    class BroadcastBellRing
    {
        const int ROB_BROADCAST_PORT = 34343;
        static IPAddress broadcast_ipaddress = IPAddress.Parse("255.255.255.255");

        public static void Send()
        {
            try
            {
                Socket broadsock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                IPEndPoint ip = new IPEndPoint(broadcast_ipaddress, ROB_BROADCAST_PORT);
                byte[] bytes = StringToByte("Door Bell");
                broadsock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
                broadsock.SendTo(bytes, ip);
                broadsock.Close();
            }
            catch(Exception excp)
            {
                Debug.Print("Exception in BroadcastBellRing::Send " + excp.Message);
            }
        }

        private static byte[] StringToByte(string s)
        {
            char[] charArray = s.ToCharArray();
            byte[] byteArray = new byte[s.Length];
            for (int i = 0; i < s.Length; i++)
                byteArray[i] = (byte)charArray[i];
            return byteArray;
        }
    }
}
