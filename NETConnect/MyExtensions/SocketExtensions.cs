using NETConnect.Peers;
using NETConnect.Shared;
using NETConnect.Shared.Packet;
using NETConnect.Shared.Packet.Headers;
using System.Buffers;
using System.Diagnostics;
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


        public static bool IsSocketConnected(this Socket socket)
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

        //public class PacketResult
        //{
        //    public IPacketHeaderIdentifier? Header { get; set; }
        //    public byte[]? Data { get; set; }
        //    public int TotalSize { get; set; }
        //    public bool IsSuccess { get; set; }
        //    public string? ErrorMessage { get; set; }
        //}

        public static async Task<ReceivedPacket<IPacketHeaderIdentifier>?> ReceiveFullPacketAsync(this Socket Connection, PacketHelper Helper, int Timeout = 0)
        {
            CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(Timeout));

            int readBytes = 0;

            // ALLOCATE PREHEADER TO GET THE AMOUNT OF BYTES OF A PREHEADER
            byte[] preheader = ArrayPool<byte>.Shared.Rent(IPacketHeaderIdentifier.PreheaderLength);

            Memory<byte> memoryBuffer = preheader.AsMemory();
            try
            {
                while (readBytes < IPacketHeaderIdentifier.PreheaderLength || (!cts.IsCancellationRequested && Timeout > 0 && readBytes < IPacketHeaderIdentifier.PreheaderLength))
                {
                    int receivedBytes = 0;

                    if (Timeout > 0) receivedBytes = await Connection.ReceiveAsync(memoryBuffer, cts.Token);
                    else receivedBytes = await Connection.ReceiveAsync(memoryBuffer);
                    //Console.WriteLine($"looping till {IPacketHeaderIdentifier.PreheaderLength}");

                    if (receivedBytes <= 0) return null;
                    //Console.WriteLine($"received {receivedBytes} - needs {IPacketHeaderIdentifier.PreheaderLength}");

                    readBytes += receivedBytes;
                    //Console.WriteLine($"reading preheader: {readBytes}");
                }
                //Console.WriteLine($"finished reading preheader: {readBytes}");


                if (!(readBytes == IPacketHeaderIdentifier.PreheaderLength && IPacketHeaderIdentifier.IsValidHeader(preheader, out (ushort Magic, byte Version, byte HeaderLength, int PayloadLength, long SentAt) info)))
                {
                    Debug.WriteLine("if bytes arent 16 and IsValidHeader=false");
                    return null;
                }

                int PacketLength = (info.HeaderLength + info.PayloadLength);
                byte[] Packet = ArrayPool<byte>.Shared.Rent(PacketLength);
                memoryBuffer = Packet.AsMemory();

                try
                {
                    // POPULATE PART 1 OF THE FullPacket
                    Array.Copy(preheader, 0, Packet, 0, IPacketHeaderIdentifier.PreheaderLength);

                    cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                    while (readBytes < PacketLength || (!cts.IsCancellationRequested && Timeout > 0 && readBytes < PacketLength))
                    {
                        //if (Connection.Available > 0)
                        //{
                        //    Console.WriteLine($"📊 Socket has {Connection.Available} bytes available");
                        //}

                        int received = 0;

                        //Console.WriteLine($"looping till {PacketLength}");
                        if(Timeout > 0) received = await Connection.ReceiveAsync(memoryBuffer[IPacketHeaderIdentifier.PreheaderLength..PacketLength], cts.Token);
                        else received = await Connection.ReceiveAsync(memoryBuffer[IPacketHeaderIdentifier.PreheaderLength..PacketLength]);
                        //Console.WriteLine($"received {received} - needs {PacketLength}");
                        if (received <= 0) return null;
                        readBytes += received;
                    }

                    //Console.WriteLine($"finished reading FullPacket: {readBytes}");

                    Span<byte> HeaderSpan = Packet.AsSpan(IPacketHeaderIdentifier.PreheaderLength).Slice(0, info.HeaderLength - IPacketHeaderIdentifier.PreheaderLength);

                    var Temp = new PacketHeader();
                    byte[] _Header = Temp.BuildFullHeader(info, HeaderSpan);
                    var Header = PacketHeader.FromBinaryHeader(_Header.AsSpan());

                    //Console.WriteLine(Header.ToJSON(new System.Text.Json.JsonSerializerOptions() { WriteIndented = true }));

                    // CHECK IF WE NEED TO FORWARD THIS PACKET**
                    // - IGNORE PACKETS WE'VE ALREADY SEEN
                    // - ONLY CONSUME PACKET IF ITS MEANT FOR US 
                    // - REFER TO SEND FOR COMPLETE INFORMATION

                    //  IGNORE HERE BEFORE PROCESSING
                    //      DECREMENT TTL

                    //  - REJECT PACKET IF SentAt is older than 60 seconds

                    long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    long age = currentTime - Header.SentAt;

                    bool IsExpired = age > (60 * 1000);
                    bool isDesynced = age < -10000;
                    // PREVENT OLD PACKETS AND DESYNCED CLOCK 
                    if (IsExpired || isDesynced)
                    {
                        Console.WriteLine($"expired: {IsExpired}");
                        Console.WriteLine($"desync: {isDesynced}");
                        return null;
                    }
                    //Console.WriteLine("valid age");

                    // REJECT PACKETS WE'VE ALREADY SEEN and STORE PACKETS WE'VE JUST SEEN TO LATER REJECT
                    if (PackerTracker.IsPacketSeen(Header.OriginPeerId, Header.PacketId, Header.SentAt)) return null;
                    //Console.WriteLine("packet not seen");

                    PeerTable? Peer;
                    if (Helper.IsServer()) Peer = Helper.Self.TCPServer.MyPeerTable;//.NetStats.TotalBytesRead += bytesRead;
                    else
                    {
                        var H = Header;
                        Peer = Helper.Self.ConnectedPeers.Find(x => x.PeerId == H.OriginPeerId);
                    }

                    if (Peer is not null)
                    {
                        Peer.NetStats.TotalBytesRead += readBytes;
                        Peer.NetStats.LastUpdated = DateTime.UtcNow;
                    }


                    //Console.WriteLine(Peer.ToJSON(new System.Text.Json.JsonSerializerOptions() { WriteIndented = true}));

                    if (Header.RecipientPeerId == Guid.Empty)
                    {
                        // IF EMPTY AND DIRECT, ITS MEANT FOR US
                        if (Header.Route == PacketRoute.Direct)
                        {
                            Debug.WriteLine("Peer received packet meant for them [no PeerId, and Direct]");

                            ReceivedPacket<IPacketHeaderIdentifier> Received = new ReceivedPacket<IPacketHeaderIdentifier>(Packet, Header, true);

                            //if (Received is null) Console.WriteLine("rc null");
                            //else Console.WriteLine("rc not null");
                            // Signals ArrayPool Transfer
                            Packet = null;

                            return Received;
                        }
                        else if (Header.Route == PacketRoute.Broadcast)
                        {
                            // THIS IS MEANT FOR US 
                            // DO NOT REBROADCAST EVEN USING TTL 
                            // NOTE: APPARENTLY REBROADING IS A "REAL BROADCAST",SO WE WILL DISREGARD ABOVE...
                            Console.WriteLine("Peer received packet meant for everyone [Broadcast]");

                            ReceivedPacket<IPacketHeaderIdentifier> Received = new ReceivedPacket<IPacketHeaderIdentifier>(Packet, Header, true);

                            // RIPPED FROM GOSSIP ROUTE [8-21-26]
                            // WILL NEED TO CONVERT TO A FUNCTION SOON

                            Console.WriteLine();
                            if (Header.TTL > 0)
                            {
                                byte[] BroadcastPacket = Received.GetFullPacketCopy();
                                Span<byte> BroadcastPacketSpan = BroadcastPacket.AsSpan();

                                // UPDATE PACKET TTL AND LASTHOPID SO WE CAN KEEP TRACK OF PREVIOUS SENDER

                                int offset = Header.HeaderLength - 33;
                                Helper.Self.PeerId.TryWriteBytes(BroadcastPacketSpan[offset..]);

                                byte newTTL = (byte)(Header.TTL - 1);
                                BroadcastPacketSpan[Header.HeaderLength - 1] = newTTL;

                                Console.WriteLine($"Message to broadcast => {BitConverter.ToString(BroadcastPacket)}");
                                Console.WriteLine($"forwarding to more peers -> {newTTL}");
                                Helper.Self.BroadcastForward(BroadcastPacket, Header, Helper.Self.PeerId);
                            }

                            // Signals ArrayPool Transfer
                            Packet = null;

                            return Received;
                        }
                        else if (Header.Route == PacketRoute.Gossip)
                        {
                            // IF WE ARE STILL AUTHENTICATING WE ONLY WANT TO PASS THIS INFORMATION AROUND
                            // DO NOT CONSUME THIS UNTIL AUTHENTICATION IS COMPLETE
                            if (Helper.IsAuthenticating)
                            {
                                Console.WriteLine("this is still authenticating letsskip!");
                                return null;
                            }

                            // THIS IS ALSO MEANT FOR US 
                            // GOSSIP TO OTHER PEERS BASED ON TTL
                            Console.WriteLine("Peer received packet meant to gossip [Select based on TTL]");

                            // IF THIS GOSSIP HAS A RECIPIENT END THE GOSSIP THERE
                            // MAYBE ADD AN OPTION TO CONTINUE BUT TARGET FOR RECIPIENT 
                            // MAYBE EVERYONE WHO RECEIVES IT CAN GET REPORTED BACK TO THE ORIGIN PEER?

                            // Store reference to this ReceivedPacket as is then modify for gossip
                            ReceivedPacket<IPacketHeaderIdentifier> Received = new ReceivedPacket<IPacketHeaderIdentifier>(Packet, Header, true);

                            if (Header.TTL > 0)
                            {
                                byte[] GossipPacket = Received.GetFullPacketCopy();
                                Span<byte> GossipPacketSpan = GossipPacket.AsSpan();

                                // UPDATE PACKET TTL AND LASTHOPID SO WE CAN KEEP TRACK OF PREVIOUS SENDER

                                int offset = Header.HeaderLength - 33;
                                Helper.Self.PeerId.TryWriteBytes(GossipPacketSpan[offset..]);
                                //GossipPacketSpan[Header.HeaderLength - 33] = 

                                byte newTTL = (byte)(Header.TTL - 1);
                                GossipPacketSpan[Header.HeaderLength - 1] = newTTL;

                                Console.WriteLine($"Message to gossip => {BitConverter.ToString(GossipPacket)}");
                                Helper.Self.GossipForward(GossipPacket, Header, Math.Min(2, Helper.Self.ConnectedPeers.Count()), Helper.Self.PeerId);
                            }

                            // Signals ArrayPool Transfer
                            Packet = null;

                            return Received;
                        }
                    }
                    else if (Header.RecipientPeerId == Helper.Self.PeerId)
                    {
                        Debug.WriteLine("Peer received packet meant for them [PeerId]");
                        ReceivedPacket<IPacketHeaderIdentifier> Received = new ReceivedPacket<IPacketHeaderIdentifier>(Packet, Header, true);

                        // Signals ArrayPool Transfer
                        Packet = null;

                        return Received;
                    }
                    else
                    {
                        Debug.WriteLine("Peer received packet that is included in the unkown scope");
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
                //catch (Exception Ex) { Console.WriteLine(Ex.ToString()); }
                finally { if (Packet is not null) ArrayPool<byte>.Shared.Return(Packet); }
            }
            finally { ArrayPool<byte>.Shared.Return(preheader); }

            return null;


        }

        public static async Task ReceivePacket(this Socket Connection, (ushort Magic, byte Version, byte HeaderLength, int PayloadLength, long SentAt) info, TimeSpan Timeout)
        {
            CancellationTokenSource cts = new CancellationTokenSource(Timeout);



            while (!cts.IsCancellationRequested)
            {

            }
        }

        //public static async Task<ReceivedPacket<IPacketHeaderIdentifier>?> ReceivedPacketAsync(this Socket Connection, PacketHeader Header)
        //{
        //    //Header = default;

        //    int bytesRead = -1;

        //    if (!Connection.IsSocketConnected()) return null;
        //    //Console.WriteLine($"receiving -> {Connection.Available}");

        //    Span<byte> preheader = stackalloc byte[IPacketHeaderIdentifier.PreheaderLength];
        //    if (!(Connection.Available > IPacketHeaderIdentifier.PreheaderLength)) return null;
        //    int receivedBytes = Connection.Receive(preheader, SocketFlags.Peek);
        //    //Console.WriteLine($"peaked at bytes\nReceived:Peaked -> {receivedBytes}");




        //    // THIS SHOULD BE INCLUDED IN ALL FUTURE VERSIONS OF HEADERS UNLESS THERE IS A PREHEADER CHANGE
        //    if (!(receivedBytes == IPacketHeaderIdentifier.PreheaderLength && IPacketHeaderIdentifier.IsValidHeader(preheader, out (ushort Magic, byte Version, byte HeaderLength, int PayloadLength, long SentAt) info)))
        //    {
        //        Debug.WriteLine("if bytes arent 16 and IsValidHeader=false");
        //        return null;
        //    }
        //    //Console.WriteLine("RECEIVED => 8 BYTES, PREHEADER VALID");

        //    // SUPPORT FRAGMENTATION LATER (IDC ABOUT IT RIGHT NOW BUT WE'LL NEED IT FOR FORMATS THAT CANT SEND HUGE AMOUNTS OF DATA)

        //    int PacketLength = (info.HeaderLength + info.PayloadLength);

        //    // SEE IF FULL PAYLOAD IS THERE FOR NOW, IF NOT RETURN EMPTY, AS WE WANT IT ALL AT ONCE
        //    if (!(Connection.Available >= PacketLength)) return null;
        //    //byte[] Packet = new byte[PacketLength];
        //    byte[] Packet = ArrayPool<byte>.Shared.Rent(PacketLength);
        //    //Console.WriteLine("PACKET READY FOR DOWNLOAD");

        //    try
        //    {
        //        bytesRead = Connection.Receive(Packet, 0, PacketLength, SocketFlags.None);
        //        if (bytesRead == 0 || bytesRead != PacketLength) return null;

        //        //Helper.Self.TCPServer.MyPeerTable.NetStats.TotalBytesRead += bytesRead;
        //        //Helper.Self.TCPServer.MyPeerTable.NetStats.LastUpdated = DateTime.UtcNow;

        //        Debug.WriteLine($"PACKET DOWNLOADED -> {bytesRead}");

        //        //Console.WriteLine($"Received => {BitConverter.ToString(Packet)} - im here!!!");
        //        Span<byte> HeaderSpan = Packet.AsSpan(IPacketHeaderIdentifier.PreheaderLength).Slice(0, info.HeaderLength - IPacketHeaderIdentifier.PreheaderLength);
        //        //Console.WriteLine($"[DEBUG] -> {BitConverter.ToString(HeaderSpan.ToArray())}");
        //        //Console.WriteLine("after span");
        //        var Temp = new PacketHeader();
        //        byte[] _Header = Temp.BuildFullHeader(info, HeaderSpan);
        //        //Console.WriteLine("after build");
        //        //Console.WriteLine($"FullPacket => {BitConverter.ToString(_Header)}");

        //        Header = PacketHeader.FromBinaryHeader(_Header.AsSpan());
        //        //Console.WriteLine("after frombinary");
        //        Console.WriteLine($"Header => {Header.ToJSON(new System.Text.Json.JsonSerializerOptions() { WriteIndented = true })}");

        //        // CHECK IF WE NEED TO FORWARD THIS PACKET**
        //        // - IGNORE PACKETS WE'VE ALREADY SEEN
        //        // - ONLY CONSUME PACKET IF ITS MEANT FOR US 
        //        // - REFER TO SEND FOR COMPLETE INFORMATION

        //        //  IGNORE HERE BEFORE PROCESSING
        //        //      DECREMENT TTL

        //        //  - REJECT PACKET IF SentAt is older than 60 seconds

        //        long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        //        long age = currentTime - Header.SentAt;

        //        // PREVENT OLD PACKETS AND DESYNCED CLOCK 
        //        if (age > 60000 || age < -10000) return null;

        //        // REJECT PACKETS WE'VE ALREADY SEEN and STORE PACKETS WE'VE JUST SEEN TO LATER REJECT
        //        if (PackerTracker.IsPacketSeen(Header.OriginPeerId, Header.PacketId, Header.SentAt)) return null;


        //        PeerTable? Peer;
        //        if (Helper.IsServer()) Peer = Helper.Self.TCPServer.MyPeerTable;//.NetStats.TotalBytesRead += bytesRead;
        //        else
        //        {
        //            var H = Header;
        //            Peer = Helper.Self.ConnectedPeers.Find(x => x.PeerId == H.OriginPeerId);
        //        }

        //        if (Peer is not null)
        //        {
        //            Peer.NetStats.TotalBytesRead += bytesRead;
        //            Peer.NetStats.LastUpdated = DateTime.UtcNow;
        //        }


        //        //Console.WriteLine(Peer.ToJSON(new System.Text.Json.JsonSerializerOptions() { WriteIndented = true}));

        //        if (Header.RecipientPeerId == Guid.Empty)
        //        {
        //            // IF EMPTY AND DIRECT, ITS MEANT FOR US
        //            if (Header.Route == PacketRoute.Direct)
        //            {
        //                Debug.WriteLine("Peer received packet meant for them [no PeerId, and Direct]");

        //                ReceivedPacket<IPacketHeaderIdentifier> Received = new ReceivedPacket<IPacketHeaderIdentifier>(Packet, Header, true);

        //                // Signals ArrayPool Transfer
        //                Packet = null;

        //                return Received;
        //            }
        //            else if (Header.Route == PacketRoute.Broadcast)
        //            {
        //                // THIS IS MEANT FOR US 
        //                // DO NOT REBROADCAST EVEN USING TTL
        //                Console.WriteLine("Peer received packet meant for everyone [Broadcast]");

        //                ReceivedPacket<IPacketHeaderIdentifier> Received = new ReceivedPacket<IPacketHeaderIdentifier>(Packet, Header, true);

        //                // Signals ArrayPool Transfer
        //                Packet = null;

        //                return Received;
        //            }
        //            else if (Header.Route == PacketRoute.Gossip)
        //            {
        //                // IF WE ARE STILL AUTHENTICATING WE ONLY WANT TO PASS THIS INFORMATION AROUND
        //                // DO NOT CONSUME THIS UNTIL AUTHENTICATION IS COMPLETE
        //                if (Helper.IsAuthenticating)
        //                {
        //                    Console.WriteLine("this is still authenticating letsskip!");
        //                    return null;
        //                }

        //                // THIS IS ALSO MEANT FOR US 
        //                // GOSSIP TO OTHER PEERS BASED ON TTL
        //                Console.WriteLine("Peer received packet meant to gossip [Select based on TTL]");

        //                // IF THIS GOSSIP HAS A RECIPIENT END THE GOSSIP THERE
        //                // MAYBE ADD AN OPTION TO CONTINUE BUT TARGET FOR RECIPIENT 
        //                // MAYBE EVERYONE WHO RECEIVES IT CAN GET REPORTED BACK TO THE ORIGIN PEER?

        //                // Store reference to this ReceivedPacket as is then modify for gossip
        //                ReceivedPacket<IPacketHeaderIdentifier> Received = new ReceivedPacket<IPacketHeaderIdentifier>(Packet, Header, true);

        //                if (Header.TTL > 0)
        //                {
        //                    byte[] GossipPacket = Received.GetFullPacketCopy();
        //                    Span<byte> GossipPacketSpan = GossipPacket.AsSpan();

        //                    // UPDATE PACKET TTL AND LASTHOPID SO WE CAN KEEP TRACK OF PREVIOUS SENDER

        //                    int offset = Header.HeaderLength - 33;
        //                    Helper.Self.PeerId.TryWriteBytes(GossipPacketSpan[offset..]);
        //                    //GossipPacketSpan[Header.HeaderLength - 33] = 

        //                    byte newTTL = (byte)(Header.TTL - 1);
        //                    GossipPacketSpan[Header.HeaderLength - 1] = newTTL;

        //                    Console.WriteLine($"Message to gossip => {BitConverter.ToString(GossipPacket)}");
        //                    Helper.Self.GossipForward(GossipPacket, Header, Math.Min(2, Helper.Self.ConnectedPeers.Count()), Helper.Self.PeerId);

        //                }

        //                // Signals ArrayPool Transfer
        //                Packet = null;

        //                return Received;
        //            }
        //        }
        //        else if (Header.RecipientPeerId == Helper.Self.PeerId)
        //        {
        //            Debug.WriteLine("Peer received packet meant for them [PeerId]");
        //            ReceivedPacket<IPacketHeaderIdentifier> Received = new ReceivedPacket<IPacketHeaderIdentifier>(Packet, Header, true);

        //            // Signals ArrayPool Transfer
        //            Packet = null;

        //            return Received;
        //        }
        //        else
        //        {
        //            Debug.WriteLine("Peer received packet that is included in the unkown scope");
        //            // FORWARD OUR PACKET HERE BASED ON WHATEVER ROUTING RULES WE HAVE 
        //            // OR DIRECTLY TO THE RECIPIENT ITS MEANT FOR 
        //            // BROADCAST 

        //            // NOT SURE WHAT THIS SCOPE IS FOR REALLY AS EVERYTHING ELSE IS DEFINED ABOVE

        //            return null;
        //        }

        //        //Received = new ReceivedPacket<IPacketHeaderIdentifier>(Packet, Header, true);

        //        //// SIGNALED ArrayPool Transfer
        //        //Packet = null;

        //        return null;
        //    }
        //    catch (Exception Ex) { Debug.WriteLine($"[DEBUG]:ReceivedPacket Exception -> {Ex}"); return null; }
        //    finally
        //    {
        //        if (Packet is not null)
        //        {
        //            ArrayPool<byte>.Shared.Return(Packet);
        //        }
        //    }
        //}


        // THIS NEEDS TO USE ArrayPool SO THAT IT CAN SCALE WITH TIME
        // This is example 1 of using ArrayPool while our receives are being moved over to async 
        public static ReceivedPacket<IPacketHeaderIdentifier>? ReceivedPacket(this Socket Connection, ref PacketHelper Helper, out PacketHeader Header)
        {
            Header = default;

            int bytesRead = -1;

            if (Connection.IsGracefulShutdown()) return null;
            //Console.WriteLine($"receiving -> {Connection.Available}");

            Span<byte> preheader = stackalloc byte[IPacketHeaderIdentifier.PreheaderLength];
            if (!(Connection.Available > IPacketHeaderIdentifier.PreheaderLength)) return null;
            int receivedBytes = Connection.Receive(preheader, SocketFlags.Peek);
            //Console.WriteLine($"peaked at bytes\nReceived:Peaked -> {receivedBytes}");

            // THIS SHOULD BE INCLUDED IN ALL FUTURE VERSIONS OF HEADERS UNLESS THERE IS A PREHEADER CHANGE
            if (!(receivedBytes == IPacketHeaderIdentifier.PreheaderLength && IPacketHeaderIdentifier.IsValidHeader(preheader, out (ushort Magic, byte Version, byte HeaderLength, int PayloadLength, long SentAt) info)))
            {
                Debug.WriteLine("if bytes arent 16 and IsValidHeader=false");
                return null;
            }
            //Console.WriteLine("RECEIVED => 8 BYTES, PREHEADER VALID");

            // SUPPORT FRAGMENTATION LATER (IDC ABOUT IT RIGHT NOW BUT WE'LL NEED IT FOR FORMATS THAT CANT SEND HUGE AMOUNTS OF DATA)

            int PacketLength = (info.HeaderLength + info.PayloadLength);

            // SEE IF FULL PAYLOAD IS THERE FOR NOW, IF NOT RETURN EMPTY, AS WE WANT IT ALL AT ONCE
            if (!(Connection.Available >= PacketLength)) return null;
            //byte[] Packet = new byte[PacketLength];
            byte[] Packet = ArrayPool<byte>.Shared.Rent(PacketLength);
            //Console.WriteLine("PACKET READY FOR DOWNLOAD");

            try
            {
                bytesRead = Connection.Receive(Packet, 0, PacketLength, SocketFlags.None);
                if (bytesRead == 0 || bytesRead != PacketLength) return null;

                //Helper.Self.TCPServer.MyPeerTable.NetStats.TotalBytesRead += bytesRead;
                //Helper.Self.TCPServer.MyPeerTable.NetStats.LastUpdated = DateTime.UtcNow;

                Debug.WriteLine($"PACKET DOWNLOADED -> {bytesRead}");

                //Console.WriteLine($"Received => {BitConverter.ToString(Packet)} - im here!!!");
                Span<byte> HeaderSpan = Packet.AsSpan(IPacketHeaderIdentifier.PreheaderLength).Slice(0, info.HeaderLength - IPacketHeaderIdentifier.PreheaderLength);
                //Console.WriteLine($"[DEBUG] -> {BitConverter.ToString(HeaderSpan.ToArray())}");
                //Console.WriteLine("after span");
                var Temp = new PacketHeader();
                byte[] _Header = Temp.BuildFullHeader(info, HeaderSpan);
                //Console.WriteLine("after build");
                //Console.WriteLine($"FullPacket => {BitConverter.ToString(_Header)}");

                Header = PacketHeader.FromBinaryHeader(_Header.AsSpan());
                //Console.WriteLine("after frombinary");
                Console.WriteLine($"Header => {Header.ToJSON(new System.Text.Json.JsonSerializerOptions() { WriteIndented = true })}");

                // CHECK IF WE NEED TO FORWARD THIS PACKET**
                // - IGNORE PACKETS WE'VE ALREADY SEEN
                // - ONLY CONSUME PACKET IF ITS MEANT FOR US 
                // - REFER TO SEND FOR COMPLETE INFORMATION

                //  IGNORE HERE BEFORE PROCESSING
                //      DECREMENT TTL

                //  - REJECT PACKET IF SentAt is older than 60 seconds

                long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                long age = currentTime - Header.SentAt;

                // PREVENT OLD PACKETS AND DESYNCED CLOCK 
                if (age > 60000 || age < -10000) return null;

                // REJECT PACKETS WE'VE ALREADY SEEN and STORE PACKETS WE'VE JUST SEEN TO LATER REJECT
                if (PackerTracker.IsPacketSeen(Header.OriginPeerId, Header.PacketId, Header.SentAt)) return null;


                PeerTable? Peer;
                if (Helper.IsServer()) Peer = Helper.Self.TCPServer.MyPeerTable;//.NetStats.TotalBytesRead += bytesRead;
                else
                {
                    var H = Header;
                    Peer = Helper.Self.ConnectedPeers.Find(x => x.PeerId == H.OriginPeerId);
                }

                if(Peer is not null)
                {
                    Peer.NetStats.TotalBytesRead += bytesRead;
                    Peer.NetStats.LastUpdated = DateTime.UtcNow;
                }


                //Console.WriteLine(Peer.ToJSON(new System.Text.Json.JsonSerializerOptions() { WriteIndented = true}));

                if (Header.RecipientPeerId == Guid.Empty)
                {
                    // IF EMPTY AND DIRECT, ITS MEANT FOR US
                    if (Header.Route == PacketRoute.Direct)
                    {
                        Debug.WriteLine("Peer received packet meant for them [no PeerId, and Direct]");

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

                        ReceivedPacket<IPacketHeaderIdentifier> Received = new ReceivedPacket<IPacketHeaderIdentifier>(Packet, Header, true);

                        // Signals ArrayPool Transfer
                        Packet = null;

                        return Received;
                    }
                    else if (Header.Route == PacketRoute.Gossip)
                    {
                        // IF WE ARE STILL AUTHENTICATING WE ONLY WANT TO PASS THIS INFORMATION AROUND
                        // DO NOT CONSUME THIS UNTIL AUTHENTICATION IS COMPLETE
                        if (Helper.IsAuthenticating)
                        {
                            Console.WriteLine("this is still authenticating letsskip!");
                            return null;
                        }

                        // THIS IS ALSO MEANT FOR US 
                        // GOSSIP TO OTHER PEERS BASED ON TTL
                        Console.WriteLine("Peer received packet meant to gossip [Select based on TTL]");

                        // IF THIS GOSSIP HAS A RECIPIENT END THE GOSSIP THERE
                        // MAYBE ADD AN OPTION TO CONTINUE BUT TARGET FOR RECIPIENT 
                        // MAYBE EVERYONE WHO RECEIVES IT CAN GET REPORTED BACK TO THE ORIGIN PEER?

                        // Store reference to this ReceivedPacket as is then modify for gossip
                        ReceivedPacket<IPacketHeaderIdentifier> Received = new ReceivedPacket<IPacketHeaderIdentifier>(Packet, Header, true);

                        if (Header.TTL > 0)
                        {
                            byte[] GossipPacket = Received.GetFullPacketCopy();
                            Span<byte> GossipPacketSpan = GossipPacket.AsSpan();

                            // UPDATE PACKET TTL AND LASTHOPID SO WE CAN KEEP TRACK OF PREVIOUS SENDER

                            int offset = Header.HeaderLength - 33;
                            Helper.Self.PeerId.TryWriteBytes(GossipPacketSpan[offset..]);
                            //GossipPacketSpan[Header.HeaderLength - 33] = 

                            byte newTTL = (byte)(Header.TTL - 1);
                            GossipPacketSpan[Header.HeaderLength - 1] = newTTL;

                            Console.WriteLine($"Message to gossip => {BitConverter.ToString(GossipPacket)}");
                            Helper.Self.GossipForward(GossipPacket, Header, Math.Min(2, Helper.Self.ConnectedPeers.Count()), Helper.Self.PeerId);

                        }

                        // Signals ArrayPool Transfer
                        Packet = null;

                        return Received;
                    }
                }
                else if (Header.RecipientPeerId == Helper.Self.PeerId)
                {
                    Debug.WriteLine("Peer received packet meant for them [PeerId]");
                    ReceivedPacket<IPacketHeaderIdentifier> Received = new ReceivedPacket<IPacketHeaderIdentifier>(Packet, Header, true);

                    // Signals ArrayPool Transfer
                    Packet = null;

                    return Received;
                }
                else
                {
                    Debug.WriteLine("Peer received packet that is included in the unkown scope");
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
            catch (Exception Ex) { Debug.WriteLine($"[DEBUG]:ReceivedPacket Exception -> {Ex}"); return null; }
            finally
            {
                if(Packet is not null)
                {
                    ArrayPool<byte>.Shared.Return(Packet);
                }
            }
        }


        // THIS NEEDS TO USE ArrayPool SO THAT IT CAN SCALE WITH TIME 
        [Obsolete]
        public static byte[] ReceivePacket(this Socket Connection, ref PacketHelper Helper, out PacketHeader Header)
        {
            Header = new PacketHeader();
            int bytesRead = -1;

            if (Connection.IsGracefulShutdown()) return Array.Empty<byte>();
            //Console.WriteLine($"receiving -> {Connection.Available}");

            // CHECK FOR OUR PREHEADER - IF NOT THERE RETURN EMPTY
            // use span for this small amount of data, then when we read it all use ArrayPool (im not used to using this)
            Span<byte> preheader = stackalloc byte[16];
            //Console.WriteLine($"available -> {Connection.Available}");
            if (!(Connection.Available > 16)) return Array.Empty<byte>();    // stop using available and IsGracefulShutdown() eventually
            int receivedBytes = Connection.Receive(preheader, SocketFlags.Peek);
            //Console.WriteLine($"peaked at bytes\nReceived:Peaked -> {receivedBytes}");

            // THIS SHOULD BE INCLUDED IN ALL FUTURE VERSIONS OF HEADERS UNLESS THERE IS A PREHEADER CHANGE
            if (!(receivedBytes == 16 && IPacketHeaderIdentifier.IsValidHeader(preheader, out (ushort Magic, byte Version, byte HeaderLength, int PayloadLength, long SentAt) info)))
            {
                //Console.WriteLine("if bytes arent 8 and IsValidHeader=false");
                return Array.Empty<byte>();
            }
            //Console.WriteLine("RECEIVED => 8 BYTES, PREHEADER VALID");


            // SUPPORT FRAGMENTATION LATER (IDC ABOUT IT RIGHT NOW BUT WE'LL NEED IT FOR FORMATS THAT CANT SEND HUGE AMOUNTS OF DATA)

            int PacketLength = (info.HeaderLength + info.PayloadLength);

            // SEE IF FULL PAYLOAD IS THERE FOR NOW, IF NOT RETURN EMPTY, AS WE WANT IT ALL AT ONCE
            if (!(Connection.Available >= PacketLength)) return Array.Empty<byte>();
            //byte[] Packet = new byte[PacketLength];
            byte[] Packet = ArrayPool<byte>.Shared.Rent(PacketLength);
            //Console.WriteLine("PACKET READY FOR DOWNLOAD");

            try
            {
                bytesRead = Connection.Receive(Packet, 0, PacketLength, SocketFlags.None);
                if (bytesRead == 0 || bytesRead != PacketLength) return Array.Empty<byte>();
                //Console.WriteLine($"PACKET DOWNLOADED -> {bytesRead}");

                //Console.WriteLine($"Received => {BitConverter.ToString(Packet)}");
                Span<byte> HeaderSpan = Packet.AsSpan(16).Slice(0, info.HeaderLength-16);
                //Console.WriteLine($"[DEBUG] -> {BitConverter.ToString(HeaderSpan.ToArray())}");
                //Console.WriteLine("after span");
                byte[] _Header = Header.BuildFullHeader(info, HeaderSpan);
                //Console.WriteLine("after build");
                //Console.WriteLine($"FullPacket => {BitConverter.ToString(_Header)}");

                Header = PacketHeader.FromBinaryHeader(_Header.AsSpan());
                //Console.WriteLine("after frombinary");
                //Console.WriteLine($"Header => {Header.ToJSON(new System.Text.Json.JsonSerializerOptions() { WriteIndented = true })}");

                return Packet.AsSpan(info.HeaderLength, info.PayloadLength).ToArray();
            }
            catch { return Array.Empty<byte>(); }
            finally { ArrayPool<byte>.Shared.Return(Packet); }

        }
        #endregion

    }
}
