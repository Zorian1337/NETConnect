using NETConnect.Shared;
using NETConnect.Shared.Packet;
using NETConnect.Shared.Packet.Headers;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;

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
        // This is example 1 of using ArrayPool while our receives are being moved over to async 
        public static ReceivedPacket<IPacketHeaderIdentifier>? ReceivedPacket(this Socket Connection, ref PacketHelper Helper)
        {
            int bytesRead = -1;

            if (Connection.IsGracefulShutdown()) return null;
            //Console.WriteLine($"receiving -> {Connection.Available}");

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
                bytesRead = Connection.Receive(Packet, 0, PacketLength, SocketFlags.None);
                if (bytesRead == 0 || bytesRead != PacketLength) return null;
                Console.WriteLine($"PACKET DOWNLOADED -> {bytesRead}");

                Console.WriteLine($"Received => {BitConverter.ToString(Packet)} - im here!!!");
                Span<byte> HeaderSpan = Packet.AsSpan(8).Slice(0, info.HeaderLength - 8);
                //Console.WriteLine($"[DEBUG] -> {BitConverter.ToString(HeaderSpan.ToArray())}");
                //Console.WriteLine("after span");
                var Temp = new PacketHeader();
                byte[] _Header = Temp.BuildFullHeader(info, HeaderSpan);
                //Console.WriteLine("after build");
                //Console.WriteLine($"FullPacket => {BitConverter.ToString(_Header)}");

                var Header = PacketHeader.FromBinaryHeader(_Header.AsSpan());
                //Console.WriteLine("after frombinary");
                Console.WriteLine($"Header => {Header.ToJSON(new System.Text.Json.JsonSerializerOptions() { WriteIndented = true })}");
                //ReceivedPacket<IPacketHeaderIdentifier> Received;

                // CHECK IF WE NEED TO FORWARD THIS PACKET**
                // - IGNORE PACKETS WE'VE ALREADY SEEN
                // - ONLY CONSUME PACKET IF ITS MEANT FOR US 
                // - REFER TO SEND FOR COMPLETE INFORMATION

                //  IGNORE HERE BEFORE PROCESSING
                //      DECREMENT TTL

                if (Header.RecipientPeerId == Guid.Empty)
                {
                    // IF EMPTY AND DIRECT, ITS MEANT FOR US
                    if(Header.Route == PacketRoute.Direct)
                    {
                        Console.WriteLine("Peer received packet meant for them [no PeerId, and Direct]");

                        ReceivedPacket<IPacketHeaderIdentifier> Received = new ReceivedPacket<IPacketHeaderIdentifier>(Packet, Header, true);

                        // Signals ArrayPool Transfer
                        Packet = null;

                        return Received;
                    }
                    else if (Header.Route == PacketRoute.Broadcast)
                    {
                        // THIS IS MEANT FOR US 
                        // DO NOT REBROADCAST EVEN USING TTL
                        Console.WriteLine("Peer received packet meant for everyone [Broadcast]");



                        return null;
                    }
                    else if (Header.Route == PacketRoute.Gossip)
                    {
                        // THIS IS ALSO MEANT FOR US 
                        // GOSSIP TO OTHER PEERS BASED ON TTL
                        Console.WriteLine("Peer received packet meant to gossip [Select based on TTL]");



                        return null;
                    }
                }
                else if(Header.RecipientPeerId == Helper.Self.PeerId)
                {
                    Console.WriteLine("Peer received packet meant for them [PeerId]");
                    ReceivedPacket<IPacketHeaderIdentifier> Received = new ReceivedPacket<IPacketHeaderIdentifier>(Packet, Header, true);

                    // Signals ArrayPool Transfer
                    Packet = null;

                    return Received;
                }
                else
                {
                    Console.WriteLine("Peer received packet that is included in the unkown scope");
                    // FORWARD OUR PACKET HERE BASED ON WHATEVER ROUTING RULES WE HAVE 
                    // OR DIRECTLY TO THE RECIPIENT ITS MEANT FOR 
                    // BROADCAST 

                    // NOT SURE WHAT THIS SCOPE IS FOR REALLY AS EVERYTHING ELSE IS DEFINED ABOVE

                    return null;
                }

                //Received = new ReceivedPacket<IPacketHeaderIdentifier>(Packet, Header, true);

                //// SIGNALED ArrayPool Transfer
                //Packet = null;

                return null;
            }
            catch (Exception Ex) { Console.WriteLine($"[DEBUG]:ReceivedPacket Exception -> {Ex}"); return null; }
            finally
            {
                if(Packet is not null)
                {
                    ArrayPool<byte>.Shared.Return(Packet);
                }
            }
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
