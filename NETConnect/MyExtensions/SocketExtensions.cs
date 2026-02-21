using NETConnect.Shared;
using NETConnect.Shared.Packet;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
//using static System.Runtime.InteropServices.JavaScript.JSType;

namespace NETConnect.MyExtensions
{
    public static class SocketExtensions
    {
        #region PacketHelper stuff...


        public static int Send(this Socket Connection, ref byte[] Buffer, ReadOnlySpan<byte> Data, PacketActionType ActionType)
        {
            int bytesSent = -1;

            PacketHeader Header = new PacketHeader(Data.Length, ActionType, PacketEncodingType.BINARY);
            ReadOnlySpan<byte> Packet = Header.WriteTo(Buffer);


            if (Data.Length == 0) bytesSent = Connection.Send(Packet);
            else
            {
                // Uses buffer to create a span big enough to hold both packet header and packet data
                Span<byte> WriterSpan = new Span<byte>(Buffer);

                // Fills span with our packet data
                Packet.CopyTo(WriterSpan);
                Data.CopyTo(WriterSpan.Slice(Packet.Length, Data.Length));

                bytesSent = Connection.Send(WriterSpan.Slice(0, Packet.Length + Data.Length)); // Only send parts of the span that we just populated
            }


            return bytesSent;
        }

        public static int Send(this Socket Connection, ref byte[] Buffer, byte[] Data, PacketActionType ActionType)
        {
            int bytesSent = -1;

            // Prevent data array from being null but allow it to be Zero 
            if (Data is null) return bytesSent;

            PacketHeader Header = new PacketHeader(Data.Length, ActionType, PacketEncodingType.BINARY);
            ReadOnlySpan<byte> Packet = Header.WriteTo(Buffer);


            if (Data.Length == 0) bytesSent = Connection.Send(Packet);
            else
            {
                ReadOnlySpan<byte> DataSpan = Data.AsSpan();

                // Uses buffer to create a span big enough to hold both packet header and packet data
                Span<byte> WriterSpan = new Span<byte>(Buffer);

                // Fills span with our packet data
                Packet.CopyTo(WriterSpan);
                DataSpan.CopyTo(WriterSpan.Slice(Packet.Length, DataSpan.Length));

                bytesSent = Connection.Send(WriterSpan.Slice(0, Packet.Length + DataSpan.Length)); // Only send parts of the span that we just populated
            }


            return bytesSent;
        }


        /// <summary>
        /// Gets network data, based on existing buffer size, using Spans and returns as ReadOnlyMemory
        /// </summary>
        /// <param name="Connection"></param>
        /// <param name="Buffer"></param>
        /// <param name="WaitTillSizeAvailable"></param>
        /// <returns></returns>
        public static ReadOnlyMemory<byte> Receive(this Socket Connection, ref byte[] Buffer, ref Span<byte> SpanBuffer, int WaitTillSizeAvailable)
        {

            // Make sure connect is valid before any errors - needs implemented
            if (Connection.Available > WaitTillSizeAvailable)
            {

                // Use span to capture our data, Then use the base bytes to write to memory
                //Span<byte> SpanBuffer = new Span<byte>(Buffer); - removed due to being reallocated every time

                // Attempt to retrieve our custom packet from this connection
                int ReceivedLength = Connection.Receive(SpanBuffer);

                return Buffer.AsMemory<byte>(0, ReceivedLength);
            }

            return default;
        }


        public static bool ReadForPacketV2(this Socket Connection, ReadOnlyMemory<byte> DATA, out PacketHeader[] Headers, out ReadOnlyMemory<byte>[] PacketData)
        {
            // Reads the data and checks if it contains packet headers
            return PacketHeader.ReadFrom(DATA, out Headers, out PacketData);
        }

        #endregion

    }
}
