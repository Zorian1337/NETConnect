using NETConnect.Shared;
using NETConnect.Shared.Packet;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NETConnect.MyExtensions
{
    public static class SocketExtensions
    {
        public static bool IsGracefulShutdown(this Socket socket)
        {
            if (socket.Poll(0, SelectMode.SelectRead) && socket.Available == 0) return true;
            else return false;
        }


        #region PacketHelper stuff...
        public static int Send(this Socket Connection, byte[] Data, PacketActionType ActionType)
        {
            // Get needed buffer size at start
            byte[] safeBuffer = new byte[PacketHeader.HeaderSize + Data.Length]; 

            int bytesSent = -1;

            PacketHeader Header = new PacketHeader(Data.Length, ActionType, PacketEncodingType.BINARY);
            //Console.WriteLine($"Send - {Header.ByteLength} {Header.PacketAction.ToString()} {Header.SentAt}");

            ReadOnlySpan<byte> Packet = Header.WriteTo(safeBuffer);

            if (Data.Length == 0) bytesSent = Connection.Send(Packet);
            else
            {
                // Uses buffer to create a span big enough to hold both packet header and packet data
                Span<byte> WriterSpan = new Span<byte>(safeBuffer);

                // Fills span with our packet data
                Packet.CopyTo(WriterSpan);
                Data.CopyTo(WriterSpan.Slice(Packet.Length, Data.Length));

                bytesSent = Connection.Send(WriterSpan.Slice(0, Packet.Length + Data.Length)); // Only send parts of the span that we just populated
            }


            return bytesSent;
        }


        public static ReadOnlyMemory<byte> Receive(this Socket Connection, ref HeartBeat KeepAlive, byte[] Buffer, int WaitTillSizeAvailable)
        {
            // Handles connection heartbeat/timeout - sending/receiving data (should automatically update the beats if the data goes through)
            if (KeepAlive.IsTimeout() || Connection.IsGracefulShutdown()) return default;

            if (Connection.Available >= WaitTillSizeAvailable)
            {
                KeepAlive.SetLastBeat(); // Valid data is considered a beat

                // Use span to capture our data, Then use the base bytes to write to memory
                Span<byte> SpanBuffer = new Span<byte>(Buffer);

                // Attempt to retrieve our custom packet from this connection
                int ReceivedLength = Connection.Receive(SpanBuffer);

                // Make a copy of the buffer to prevent overwrite
                byte[] safeData = Buffer.SafeBufferCopy(ReceivedLength);

                // Return as ReadOnlyMemory<byte>
                return new ReadOnlyMemory<byte>(safeData);
            }

            return default;
        }

        /// <summary>
        /// Gets network data, based on existing buffer size, using Spans and returns as ReadOnlyMemory
        /// </summary>
        /// <param name="Connection"></param>
        /// <param name="Buffer"></param>
        /// <param name="WaitTillSizeAvailable"></param>
        /// <returns></returns>
        public static ReadOnlyMemory<byte> Receive(this Socket Connection, ref HeartBeat KeepAlive, ref byte[] Buffer, int WaitTillSizeAvailable)
        {
            // Handles connection heartbeat/timeout - sending/receiving data (should automatically update the beats if the data goes through)
            if (KeepAlive.IsTimeout() || Connection.IsGracefulShutdown()) return default;

            // Make sure connect is valid before any errors - needs implemented
            if (Connection.Available > WaitTillSizeAvailable)
            {
                KeepAlive.SetLastBeat(); // Valid data is considered a beat

                // Use span to capture our data, Then use the base bytes to write to memory
                Span<byte> SpanBuffer = new Span<byte>(Buffer); 

                // Attempt to retrieve our custom packet from this connection
                int ReceivedLength = Connection.Receive(SpanBuffer);

                // Make a copy of the buffer to prevent overwrite
                byte[] safeData = Buffer.SafeBufferCopy(ReceivedLength);

                // Return as ReadOnlyMemory<byte>
                return new ReadOnlyMemory<byte>(safeData);
            }

            return default;
        }


        public static byte[] ReceivePacket(this Socket Connection, ref HeartBeat KeepAlive)
        {
            int bytesRead = -1;

            // Handles connection heartbeat/timeout - sending/receiving data (should automatically update the beats if the data goes through)
            if (KeepAlive.IsTimeout() || Connection.IsGracefulShutdown()) return Array.Empty<byte>();

            if (Connection.HasValidHeader(out byte[] HeaderBytes, out int DataLength))
            {
                byte[] Buffer = HeaderBytes.SafeBufferCopy(PacketHeader.HeaderSize + DataLength);

                bytesRead = Connection.Receive(Buffer, PacketHeader.HeaderSize, DataLength, SocketFlags.None);

                if (bytesRead > 0) return Buffer;
                else return Array.Empty<byte>();
            }
            else return Array.Empty<byte>();
        }

        public static byte[] ReceivePacket(this Socket Connection, ref HeartBeat KeepAlive, out PacketHeader Header)
        {
            Header = default;
            int bytesRead = -1;

            // Handles connection heartbeat/timeout - sending/receiving data (should automatically update the beats if the data goes through)
            if (KeepAlive.IsTimeout() || Connection.IsGracefulShutdown()) return Array.Empty<byte>();

            if (Connection.HasValidHeader(out Header))
            {
                byte[] Buffer = new byte[Header.ByteLength];

                bytesRead = Connection.Receive(Buffer, 0, Header.ByteLength, SocketFlags.None);

                //Console.WriteLine($"ReceivePacket - {Header.ByteLength} {Header.PacketAction.ToString()} {Header.SentAt}");

                if (bytesRead > 0) return Buffer;
                else return Array.Empty<byte>();
            }
            else return Array.Empty<byte>();
        }

        public static byte[] ReceivePacket(this Socket Connection,  out PacketHeader Header)
        {
            Header = default;
            int bytesRead = -1;

            // Handles connection heartbeat/timeout - sending/receiving data (should automatically update the beats if the data goes through)
            if (Connection.IsGracefulShutdown()) return Array.Empty<byte>();

            if (Connection.HasValidHeader(out Header))
            {
                byte[] Buffer = new byte[Header.ByteLength];

                bytesRead = Connection.Receive(Buffer, 0, Header.ByteLength, SocketFlags.None);

                //Console.WriteLine($"ReceivePacket - {Header.ByteLength} {Header.PacketAction.ToString()} {Header.SentAt}");

                if (bytesRead > 0) return Buffer;
                else return Array.Empty<byte>();
            }
            else return Array.Empty<byte>();
        }


        public static bool HasValidHeader(this Socket Connection, out byte[] HeaderBytes, out int DataLength)
        {
            HeaderBytes = Array.Empty<byte>();
            DataLength = 0;


            if (Connection.Available >= PacketHeader.HeaderSize)
            {
                byte[] TempBuffer = new byte[PacketHeader.HeaderSize];

                // Peak at our data, get the length of our header and data (8 + data length)
                Connection.Receive(TempBuffer, PacketHeader.HeaderSize, SocketFlags.None);

                try
                {
                    if(PacketHeader.ValidateHeader(TempBuffer, out PacketHeader Header))
                    {
                        HeaderBytes = TempBuffer;
                        DataLength = Header.ByteLength;
                    }
                }
                catch (Exception Ex) { Console.WriteLine(Ex.ToString()); Debug.WriteLine(Ex.ToString()); return default; } // If any error just return, as its probably not valid
            }

            return false;
        }

        public static bool HasValidHeader(this Socket Connection, out PacketHeader Header)
        {
            Header = default;

            if (Connection.Available >= PacketHeader.HeaderSize)
            {
                byte[] TempBuffer = new byte[PacketHeader.HeaderSize];

                // Peak at our data, get the length of our header and data (8 + data length)
                Connection.Receive(TempBuffer, PacketHeader.HeaderSize, SocketFlags.None);

                try
                {
                    if (PacketHeader.ValidateHeader(TempBuffer, out Header))
                    {
                        //Console.WriteLine("valid header size");
                        //Console.WriteLine($"HasValidHeader - {Header.ByteLength} {Header.PacketAction.ToString()} {Header.SentAt}");
                        return true;
                    }
                }
                catch (Exception Ex) { Console.WriteLine(Ex.ToString()); Debug.WriteLine(Ex.ToString()); return default; } // If any error just return, as its probably not valid
            }

            return false;
        }


        public static bool ReadForPacketV2(this Socket Connection, ReadOnlyMemory<byte> DATA, out PacketHeader[] Headers, out ReadOnlyMemory<byte>[] PacketData)
        {
            // Reads the data and checks if it contains packet headers
            return PacketHeader.ReadFrom(DATA, out Headers, out PacketData);
        }

        #endregion

    }
}
