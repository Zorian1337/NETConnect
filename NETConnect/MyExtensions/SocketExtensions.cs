using NETConnect.Shared;
using NETConnect.Shared.Packet;
using NETConnect.Shared.Packet.Headers;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace NETConnect.MyExtensions
{
    public static class SocketExtensions
    {
        public static bool IsGracefulShutdown(this Socket socket)
        {
            // Polling apparently causing lots of CPU usage, Or whatever it may be but its not too worth it
            // We will need to find another alternative to this later
            if (socket.Poll(0, SelectMode.SelectRead) && socket.Available == 0) return true;
            else return false;
        }


        //public static Stream GetSslStream(this Socket Connection, X509Certificate2 cert) //bool IsServer = true
        //{
        //    NetworkStream stream = new NetworkStream(Connection, true);

        //    var ssl = new SslStream(stream, false);

        //    ssl.AuthenticateAsServer(
        //    cert,
        //    clientCertificateRequired: false,
        //    enabledSslProtocols: SslProtocols.Tls13 | SslProtocols.Tls12,
        //    checkCertificateRevocation: false);

        //    return ssl;
        //}


        #region PacketHelper stuff...

        //public static int Send(this Socket Connection, byte[] Data, PacketActionType ActionType, PacketHeader? Header)
        //{
        //    // Added to help handle invalid data 
        //    int DataSize, DataToWrite = 0;
        //    if (Data is not null)
        //    {
        //        DataSize = Data.Length;
        //        DataToWrite = Data.Length;
        //    }
        //    else
        //    {
        //        DataSize = 0;
        //        DataToWrite = -1;
        //    }

        //    // Get needed buffer size at start
        //    byte[] safeBuffer = new byte[PacketHeader.HeaderSize + DataSize];

        //    int bytesSent = -1;

        //    PacketHeader _Header = new PacketHeader(DataToWrite, ActionType, PacketEncodingType.BINARY);

        //    if (Header is not null) _Header = Header.Value;


        //    //Console.WriteLine($"Send - {Header.ByteLength} {Header.PacketAction.ToString()} {Header.SentAt}");

        //    ReadOnlySpan<byte> Packet = _Header.WriteTo(safeBuffer);

        //    if (DataSize == 0) bytesSent = Connection.Send(Packet);
        //    else
        //    {
        //        // Uses buffer to create a span big enough to hold both packet header and packet data
        //        Span<byte> WriterSpan = new Span<byte>(safeBuffer);

        //        // Fills span with our packet data
        //        Packet.CopyTo(WriterSpan);
        //        Data.CopyTo(WriterSpan.Slice(Packet.Length, Data.Length));

        //        bytesSent = Connection.Send(WriterSpan.Slice(0, Packet.Length + Data.Length)); // Only send parts of the span that we just populated
        //    }


        //    return bytesSent;
        //}

        //public static int Send(this Socket Connection, byte[] Data, ref PacketHelper Packer, PacketActionType ActionType, PacketEncryptionType EncryptionType = PacketEncryptionType.NONE)
        //{

        //}

        /// <summary>
        /// The purpose of this is to be the one to create the header manually in a different function and pass it here 
        /// The premade header will still for validation purposes set the datas size here so you wont have to yourself
        /// just everything regarding encryption and other information prior
        /// </summary>
        /// <param name="Connection"></param>
        /// <param name="Data"></param>
        /// <param name="premadeHeader"></param>
        /// <returns></returns>
        public static int SendWithHeader(this Socket Connection, byte[] Data, PacketHeader premadeHeader) 
        {
            int bytesSent = -1;
            int DataSize, DataToWrite = 0;
            if (Data is not null)
            {
                DataSize = Data.Length;
                DataToWrite = Data.Length;
            }
            else
            {
                DataSize = 0;
                DataToWrite = -1;
            }

            // Buffer which holds our network data to send
            byte[] safeBuffer = new byte[PacketHeader.HeaderSize + DataSize];

            // Only add our data size to the premade Header - this feels so backwards but its needed
            // Its so that if we want to add custom things to the header and not have it be overriden we can do it
            premadeHeader.ByteLength = DataSize;

            // Returns -1 by default
            return bytesSent;
        }

        public static int Send(this Socket Connection, byte[] Data, PacketActionType ActionType, PacketEncryptionType EncryptionType = PacketEncryptionType.NONE)
        {
            //Console.WriteLine("SEND DEBUG");
            // Added to help handle invalid data 
            int DataSize, DataToWrite = 0;
            if (Data is not null)
            {
                DataSize = Data.Length;
                DataToWrite = Data.Length;
            }
            else
            {
                DataSize = 0;
                DataToWrite = -1;
            }


            // Get needed buffer size at start
            byte[] safeBuffer = new byte[PacketHeader.HeaderSize + DataSize]; 

            int bytesSent = -1;

            PacketHeader Header = new PacketHeader(DataToWrite, ActionType, EncryptionType);
            //Console.WriteLine($"Send - {Header.ByteLength} {Header.PacketAction.ToString()} {Header.SentAt}");

            //Console.WriteLine(Header);

            ReadOnlySpan<byte> Packet = Header.WriteTo(safeBuffer);
            ReadOnlySpan<byte> DataToSend;


            if (Data.Length == 0) {
                DataToSend = Packet;
                bytesSent = Connection.Send(Packet);
            }
            else
            {
                // Uses buffer to create a span big enough to hold both packet header and packet data
                Span<byte> WriterSpan = new Span<byte>(safeBuffer);

                // Fills span with our packet data
                Packet.CopyTo(WriterSpan);
                Data.CopyTo(WriterSpan.Slice(Packet.Length, Data.Length));
                DataToSend = WriterSpan.Slice(0, Packet.Length + Data.Length);
                bytesSent = Connection.Send(DataToSend); // Only send parts of the span that we just populated
            }

            // Output the data we send as bytes for debugging
            //Console.WriteLine($"C# data sent ->\n[{ActionType.ToString()}] - [{EncryptionType.ToString()}] -> Size: {DataToSend.Length} - DATA: {string.Join(" ", DataToSend.ToArray().Select(x => x.ToString("X2")))}"); //.Select(x => x.ToString("X2")

            return bytesSent;
        }


        //public static byte[] ReceiveValidatedPacket(this Socket Connection, ref HeartBeat KeepAlive, ref PacketHelper Packer, out PacketHeader Header)
        //{
        //    // Gets Packet and Header, and handles timeouts/shutdowns

        //    byte[] Packet = Connection.ReceivePacket(out Header);

        //    if(Header.PacketAction != PacketActionType.Empty) Console.WriteLine($"Header: {Header.ToJSON()}");


        //    // Skip validation check if its either dead data, or not encrypted
        //    //if (Header.PacketAction == PacketActionType.Empty) return Packet;




        //    //Console.WriteLine($"{Header.PacketEncryptionType}");

        //    if (Header.PacketEncryptionType == PacketEncryptionType.NONE) return Packet;
        //    else if(Header.PacketEncryptionType != PacketEncryptionType.NONE)
        //    {
        //        Console.WriteLine($"[Validated-Receive] Header.Encrypted as {Header.PacketEncryptionType}");
        //        // Verify Authenticity here - this can be used with HMAC if setup right
        //        // Check packet length, return invalid if size isnt normal for an encrypted packet

        //        //if (Packet.Length == 0) { Header.PacketAction = PacketActionType.EmptyEncryptedPacket; return default; }

        //        // Check if data is type PacketEncrypted
        //        //if (Packet.IsValidJSON(out PacketEncrypted Encrypted))
        //        //{
        //        //    Console.WriteLine("Encrypted Packet found");
        //        //}
        //        //else Console.WriteLine($"Failed to find encrypted packet");
        //    }






        //    return Packet;
        //}


        public static byte[] ReceivePacket(this Socket Connection, out PacketHeader Header)
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


        //public static bool HasValidHeader(this Socket Connection, out byte[] HeaderBytes, out int DataLength)
        //{
        //    HeaderBytes = Array.Empty<byte>();
        //    DataLength = 0;


        //    if (Connection.Available >= PacketHeader.HeaderSize)
        //    {
        //        Console.WriteLine("BufferSize valid data received");

        //        byte[] TempBuffer = new byte[PacketHeader.HeaderSize];

        //        // Peak at our data, get the length of our header and data (8 + data length)
        //        Connection.Receive(TempBuffer, PacketHeader.HeaderSize, SocketFlags.None);



        //        try
        //        {
        //            if(PacketHeader.ValidateHeader(TempBuffer, out PacketHeader Header))
        //            {
        //                HeaderBytes = TempBuffer;
        //                DataLength = Header.ByteLength;

        //                // Display our temp buffer in 
        //                Console.WriteLine($"Valid Header received: {Header.ToJSON()}");
        //            }
        //            else
        //            {
        //                Console.WriteLine($"invalid header ->\nSize: {TempBuffer.Length} - {TempBuffer.ToUTF8String()}");
        //                Console.WriteLine("Socket read invalid header data");
        //                return false;
        //            }
        //        }
        //        catch (Exception Ex) { Console.WriteLine(Ex.ToString()); Debug.WriteLine(Ex.ToString()); return default; } // If any error just return, as its probably not valid
        //    }

        //    return false;
        //}

        public static bool HasValidHeader(this Socket Connection, out PacketHeader Header)
        {
            Header = default;

            if (Connection.Available >= PacketHeader.HeaderSize)
            {
                byte[] TempBuffer = new byte[PacketHeader.HeaderSize];

                // NOTE: DISREGARD THE PEAK, THIS IS WHERE WE PULL OUR HEADER OUT OF THE STREAM
                // Peak at our data, get the length of our header and data (PacketHeader.HeaderSize + data length)
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


        //public static bool ReadForPacketV2(this Socket Connection, ReadOnlyMemory<byte> DATA, out PacketHeader[] Headers, out ReadOnlyMemory<byte>[] PacketData)
        //{
        //    // Reads the data and checks if it contains packet headers
        //    return PacketHeader.ReadFrom(DATA, out Headers, out PacketData);
        //}

        #endregion

    }
}
