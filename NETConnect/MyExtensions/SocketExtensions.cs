using NETConnect.Shared.Packet;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NETConnect.MyExtensions
{
    public static class SocketExtensions
    {
        public static void Send(this Socket Connection, PacketActionType ActionType, byte[] Data, ref byte[] Buffer)
        {
            // Prevent data array from being null but allow it to be Zero 
            if (Data is null) return;

            PacketHeader Header = new PacketHeader(Data.Length, ActionType, PacketEncodingType.BINARY);

            ReadOnlySpan<byte> Packet = Header.WriteTo(Buffer);
            Connection.Send(Packet);
        }


        //public static int Receive(this Socket Connection, ref byte[] Buffer, int Offset, int Size, SocketFlags socketFlags = SocketFlags.None) => Connection.Receive(Buffer, Offset, Size, socketFlags);




        public static void SendUTF8(this Socket Connection, string UTF8Message, ref byte[] Buffer)
        {
            ReadOnlySpan<byte> Data = UTF8Message.UTF8StringToUTF8Byte(Buffer);
            Connection.Send(Data);
        }


        public static void ReadAvailableData(this Socket Connection, ref Span<byte> Buffer)
        {
            if (Connection is null) return;


            //// Check for data from client if hasnt already timed out
            if (Connection.Available > 0) Connection.Receive(Buffer); // This will be changed later 
        }
    }
}
