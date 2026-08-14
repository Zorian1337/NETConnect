using NETConnect.Shared;
using NETConnect.Shared.Packet;
using NETConnect.Shared.Packet.Headers;
using System.Buffers;
using System.Net.Sockets;

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
        public static bool IsSocketConnected(Socket socket)
        {
            try
            {
                // Make a non-blocking, zero-byte send call.
                // If it works or throws WAEWOULDBLOCK (10035), the socket is still connected.
                socket.Send(new byte[0], 0, 0, SocketFlags.None);
                return true;
            }
            catch (SocketException ex)
            {
                // Error codes indicating connection issues.
                if (ex.NativeErrorCode == 10054 ||      // Connection reset by peer.
                    ex.NativeErrorCode == 10053)        // Connection aborted.
                {
                    return false;
                }
                // For other errors, the connection might still be valid.
                return true;
            }
        }



        #region PacketHelper stuff...
        public static async Task ReadMessage(this Socket Connection)
        {
            Console.WriteLine("reading messages");
            var Pool = ArrayPoolBuffer.GetNewOrExistingArrayPool(Connection);
            await Pool.ReceiveAsync();
        }
        
        

        // THIS NEEDS TO USE ArrayPool SO THAT IT CAN SCALE WITH TIME
        public static ReceivedPacket<IPacketHeaderIdentifier>? ReceivedPacket(this Socket Connection, ref PacketHelper Helper)
        {
            int bytesRead = -1;

            if (Connection.IsGracefulShutdown()) return null;
            Console.WriteLine($"receiving -> {Connection.Available}");

            Span<byte> preheader = stackalloc byte[8];
            if (!(Connection.Available > 8)) return null;
            int receivedBytes = Connection.Receive(preheader, SocketFlags.Peek);
            Console.WriteLine($"peaked at bytes\nReceived:Peaked -> {receivedBytes}");

            // THIS SHOULD BE INCLUDED IN ALL FUTURE VERSIONS OF HEADERS UNLESS THERE IS A PREHEADER CHANGE
            if (!(receivedBytes == 8 && IPacketHeaderIdentifier.IsValidHeader(preheader, out (ushort Magic, byte Version, byte HeaderLength, int PayloadLength) info)))
            {
                Console.WriteLine("if bytes arent 8 and IsValidHeader=false");
                return null;
            }
            Console.WriteLine("RECEIVED => 8 BYTES, PREHEADER VALID");

            // SUPPORT FRAGMENTATION LATER (IDC ABOUT IT RIGHT NOW BUT WE'LL NEED IT FOR FORMATS THAT CANT SEND HUGE AMOUNTS OF DATA)

            int PacketLength = (info.HeaderLength + info.PayloadLength);

            // SEE IF FULL PAYLOAD IS THERE FOR NOW, IF NOT RETURN EMPTY, AS WE WANT IT ALL AT ONCE
            if (!(Connection.Available >= PacketLength)) return null;
            //byte[] Packet = new byte[PacketLength];
            byte[] Packet = ArrayPool<byte>.Shared.Rent(PacketLength);
            Console.WriteLine("PACKET READY FOR DOWNLOAD");

            try
            {

            }
            catch {  }
            finally { ArrayPool<byte>.Shared.Return(Packet); } // Return shared buffer

            return null; // default return type
        }


        // THIS NEEDS TO USE ArrayPool SO THAT IT CAN SCALE WITH TIME 
        public static byte[] ReceivePacket(this Socket Connection, ref PacketHelper Helper, out PacketHeader Header)
        {
            Header = new PacketHeader();
            int bytesRead = -1;

            if (Connection.IsGracefulShutdown()) return Array.Empty<byte>();
            //Console.WriteLine($"receiving -> {Connection.Available}");

            // CHECK FOR OUR PREHEADER - IF NOT THERE RETURN EMPTY
            // use span for this small amount of data, then when we read it all use ArrayPool (im not used to using this)
            Span<byte> preheader = stackalloc byte[8];
            //Console.WriteLine($"available -> {Connection.Available}");
            if (!(Connection.Available > 8)) return Array.Empty<byte>();    // stop using available and IsGracefulShutdown() eventually
            int receivedBytes = Connection.Receive(preheader, SocketFlags.Peek);
            Console.WriteLine($"peaked at bytes\nReceived:Peaked -> {receivedBytes}");

            // THIS SHOULD BE INCLUDED IN ALL FUTURE VERSIONS OF HEADERS UNLESS THERE IS A PREHEADER CHANGE
            if (!(receivedBytes == 8 && IPacketHeaderIdentifier.IsValidHeader(preheader, out (ushort Magic, byte Version, byte HeaderLength, int PayloadLength) info)))
            {
                Console.WriteLine("if bytes arent 8 and IsValidHeader=false");
                return Array.Empty<byte>();
            }
            Console.WriteLine("RECEIVED => 8 BYTES, PREHEADER VALID");


            // SUPPORT FRAGMENTATION LATER (IDC ABOUT IT RIGHT NOW BUT WE'LL NEED IT FOR FORMATS THAT CANT SEND HUGE AMOUNTS OF DATA)

            int PacketLength = (info.HeaderLength + info.PayloadLength);

            // SEE IF FULL PAYLOAD IS THERE FOR NOW, IF NOT RETURN EMPTY, AS WE WANT IT ALL AT ONCE
            if (!(Connection.Available >= PacketLength)) return Array.Empty<byte>();
            //byte[] Packet = new byte[PacketLength];
            byte[] Packet = ArrayPool<byte>.Shared.Rent(PacketLength);
            Console.WriteLine("PACKET READY FOR DOWNLOAD");

            try
            {
                bytesRead = Connection.Receive(Packet, 0, PacketLength, SocketFlags.None);
                if (bytesRead == 0 || bytesRead != PacketLength) return Array.Empty<byte>();
                Console.WriteLine($"PACKET DOWNLOADED -> {bytesRead}");

                Console.WriteLine($"Received => {BitConverter.ToString(Packet)} - im here!!!");
                Span<byte> HeaderSpan = Packet.AsSpan(8).Slice(0, info.HeaderLength-8);
                //Console.WriteLine($"[DEBUG] -> {BitConverter.ToString(HeaderSpan.ToArray())}");
                //Console.WriteLine("after span");
                byte[] _Header = Header.BuildFullHeader(info, HeaderSpan);
                //Console.WriteLine("after build");
                //Console.WriteLine($"FullPacket => {BitConverter.ToString(_Header)}");

                Header = PacketHeader.FromBinaryHeader(_Header.AsSpan());
                //Console.WriteLine("after frombinary");
                Console.WriteLine($"Header => {Header.ToJSON(new System.Text.Json.JsonSerializerOptions() { WriteIndented = true })}");

                return Packet.AsSpan(info.HeaderLength, info.PayloadLength).ToArray();
            }
            catch { return Array.Empty<byte>(); }
            finally { ArrayPool<byte>.Shared.Return(Packet); }

        }
        #endregion

    }
}
