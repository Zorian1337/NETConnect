using NETConnect.Shared;
using NETConnect.Shared.Packet;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

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

        /// <summary>
        /// Gets network data, based on existing buffer size, using Spans and returns as ReadOnlyMemory
        /// </summary>
        /// <param name="Connection"></param>
        /// <param name="Buffer"></param>
        /// <param name="WaitTillSizeAvailable"></param>
        /// <returns></returns>
        public static ReadOnlyMemory<byte> Receive(this Socket Connection, ref byte[] Buffer, int WaitTillSizeAvailable)
        {

            // Make sure connect is valid before any errors - needs implemented
            if (Connection.Available > WaitTillSizeAvailable)
            {

                // Use span to capture our data, Then use the base bytes to write to memory
                Span<byte> SpanBuffer = new Span<byte>(Buffer);

                // Attempt to retrieve our custom packet from this connection
                int ReceivedLength = Connection.Receive(SpanBuffer);

                return Buffer.AsMemory<byte>(0, ReceivedLength);
            }

            return default;
        }


        //public static bool ReadForPacketV1(this Socket Connection, Span<byte> Buffer, out PacketHeader Header, out ReadOnlySpan<byte> Data)
        //{
        //    Header = default;
        //    Data = Span<byte>.Empty;

        //    // Make sure connect is valid before any errors - needs implemented

        //    if (Connection.Available > 4)
        //    {
        //        // Attempt to retrieve our custom packet from this connection
        //        int ReceivedLength = Connection.Receive(Buffer);


        //        ReadOnlySpan<byte> DATA = Buffer.Slice(0, ReceivedLength);

        //        // Read the first 4 bytes 
        //        Header = PacketHeader.ReadFrom(DATA, out _);

        //        Console.WriteLine($"ActionType: {Header.PacketAction} - PacketLength: {Header.ByteLength}");
        //        return true;
        //    }

        //    return false;
        //}

        public static bool ReadForPacketV2(this Socket Connection, ReadOnlyMemory<byte> DATA, out PacketHeader[] Headers, out ReadOnlyMemory<byte>[] PacketData)
        {
            return PacketHeader.ReadFrom(DATA, out Headers, out PacketData);
        }

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
