using NETConnect.Shared.Packet;
using NETConnect.Shared.Packet.Headers;
using NETConnect.Shared.Peers;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace NETConnect.MyExtensions;

public static class SocketExtensions
{
    public static Dictionary<Socket, SocketBufferStorage> BufferStorage = new Dictionary<Socket, SocketBufferStorage>();
    public record SocketBufferStorage(Socket Connection)
    {
        public ArrayPool<byte> Pool = ArrayPool<byte>.Shared;

        public NetworkStream GetNetworkStream
        {
            get
            {
                if (Connection.ProtocolType != ProtocolType.Tcp || !Connection.Connected)
                {
                    GetNetworkStream = default;
                    return default;
                }

                return field ??= new NetworkStream(Connection, ownsSocket: false);
            }

            private set
            {
                if (!ReferenceEquals(field, value)) field?.Dispose();
                field = value;
            }
        }

        // THIS WILL NEED PROPER SCALING AS ITS USED PER SOCKET
        // TCP IS FINE BUT UDP SERVERS IT NEEDS SUFFICIENT BACKLOG
        public Channel<ReceivedPacket<IPacketHeaderIdentifier>> Packets = Channel.CreateBounded<ReceivedPacket<IPacketHeaderIdentifier>>(100);
       
        private byte[] RentedSingle { get; set; }
        public byte[] GetOrRentSingle(int MinimiumLength = 0)
        {
            // USE THIS FUNCTION TO CLEAR OUT DEAD SOCKETS

            if (RentedSingle is not null) return RentedSingle;
            else if (MinimiumLength > 0) return RentedSingle = Pool.Rent(MinimiumLength);
            else return Array.Empty<byte>();
        }
        public void ReturnSingle(bool clearArray = false)
        {
            if(RentedSingle is not null) Pool.Return(RentedSingle, clearArray);
        }

        public byte[] GlobalRent(int MinimiumLength) => Pool.Rent(MinimiumLength);
        public void GlobalReturn(byte[] array, bool clearArray = false) => Pool.Return(array, clearArray);


        public static SocketBufferStorage InitiateBufferStorage(Socket Connection)
        {
            // USE THIS FUNCTION TO CLEAR OUT DEAD SOCKETS

            if (!BufferStorage.TryGetValue(Connection, out SocketBufferStorage? storage))
            {
                // anything that needs to be inited here will be
                storage = new SocketBufferStorage(Connection);

                if(BufferStorage.TryAdd(Connection, storage)) return storage;
            }

            return storage;
        }
    }
    public static SocketBufferStorage GetOrCreateBufferStorage(this Socket Connection) => SocketBufferStorage.InitiateBufferStorage(Connection);

    public static async Task<ReceivedPacket<IPacketHeaderIdentifier>?> ReceiveUDPPacketAsync(this Socket Connection, bool UDPTest = false)
    {
        ReceivedPacket<IPacketHeaderIdentifier> Received = default;

        SocketBufferStorage Storage = Connection.GetOrCreateBufferStorage();

        // CREATES SMART BUFFER TO BE ABLE TO GUARENTEE SINGLE BUFFER ALLOC
        byte[] Buffer = Storage.GetOrRentSingle(10 * 1024);
        var memoryBufer = Buffer.AsMemory();

        // EndPoint to hold remote client information
        EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
        SocketReceiveFromResult result = await Connection.ReceiveFromAsync(memoryBufer, remoteEndPoint);

        int ReceivedBytes = result.ReceivedBytes;
        //if (UDPTest) Console.WriteLine($"UDP Received -> {ReceivedBytes}");
        byte[] FullPacket = Storage.GlobalRent(ReceivedBytes);
        memoryBufer.Slice(0, ReceivedBytes).CopyTo(FullPacket);

        Span<byte> Preheader = memoryBufer.Slice(0, IPacketHeaderIdentifier.PreheaderLength).Span;

        if (!IPacketHeaderIdentifier.IsValidHeader(Preheader, out (ushort Magic, byte Version, byte HeaderLength, int PayloadLength, long SentAt) info))
        {
            Console.WriteLine($"Invalid Preheader received.");
            return null;
        }

        PacketHeader Header = PacketHeader.FromBinaryHeader(memoryBufer.Span.Slice(0, info.HeaderLength));
        Received = new ReceivedPacket<IPacketHeaderIdentifier>(FullPacket, Header, true);


        if (UDPTest) Console.WriteLine($"UDP -> {Received.ToIndentedJSON()}");
        FullPacket = null;

        return Received;
    }

    public static async Task<ReceivedPacket<IPacketHeaderIdentifier>?> ReceiveTCPPacketAsync(this Socket Connection, int Timeout = 0) 
    {
        // FIND NETWORK STREAM ATTACHED TO SOCKET AND READ THAT

        SocketBufferStorage Storage = Connection.GetOrCreateBufferStorage();
        NetworkStream Stream = Storage.GetNetworkStream;

        // CREATES SMART BUFFER TO BE ABLE TO GUARENTEE SINGLE BUFFER ALLOC
        byte[] Buffer = Storage.GetOrRentSingle(10 * 1024); 

        await Stream.ReadExactlyAsync(Buffer, 0, IPacketHeaderIdentifier.PreheaderLength);
        Span<byte> Preheader = Buffer.AsSpan(0, IPacketHeaderIdentifier.PreheaderLength);

        if (!IPacketHeaderIdentifier.IsValidHeader(Preheader, out (ushort Magic, byte Version, byte HeaderLength, int PayloadLength, long SentAt) info))
        {
            Console.WriteLine("Invalid Preheader received.");
            return null;
        }

        int FullPacketLength = info.HeaderLength + info.PayloadLength;
        //Console.WriteLine($"HeaderLength -> {info.HeaderLength} - PayloadLength -> {info.PayloadLength} - PacketLength -> {FullPacketLength}");

        await Stream.ReadExactlyAsync(Buffer, IPacketHeaderIdentifier.PreheaderLength, FullPacketLength - IPacketHeaderIdentifier.PreheaderLength);
        Span<byte> FullPacketSpan = Buffer.AsSpan(0, FullPacketLength);

        // AFTER EACH PACKET IS FOUND WE MAKE A FRESH ARRAY POOL FOR STORAGE THEN PASS OWNERSHIP
        byte[] FullPacket = Storage.GlobalRent(FullPacketLength);
        FullPacketSpan.CopyTo(FullPacket);

        PacketHeader Header = PacketHeader.FromBinaryHeader(FullPacketSpan.Slice(0, info.HeaderLength)); 
        ReceivedPacket<IPacketHeaderIdentifier> Received = new ReceivedPacket<IPacketHeaderIdentifier>(FullPacket, Header, true);

        // SORT PACKAGE DIRECTION AND DECRYPTION HERE 

        FullPacket = null; 
        return Received;
    }




    public static async ValueTask<int> SendTCPPacketAsync(this Socket Connection, PacketHeader Header, byte[]? Payload = null)
    {
        Header.PayloadLength = Payload?.Length ?? 0;
        byte[] FullPacket = Header.ToBinaryHeader(Payload);

        return await Connection.SendAsync(FullPacket.AsMemory());
    }

    public static async ValueTask<int> SendUDPPacketAsync(this Socket Connection, EndPoint? RemoteEndPoint, PacketHeader Header, byte[]? Payload = null)
    {
        Header.PayloadLength = Payload?.Length ?? 0;
        byte[] FullPacket = Header.ToBinaryHeader(Payload);

        int sentBytes = -1;
        try { sentBytes = await Connection.SendToAsync(FullPacket.AsMemory(), RemoteEndPoint); }
        catch(Exception Ex) { Console.WriteLine(Ex.ToString()); }
        //Console.WriteLine($"sent UDP bytes: {sentBytes}");

        return sentBytes;
    }

    public static bool TryBindToAvailablePort(this Socket Connection, out IPEndPoint? boundEndPoint, int minRange = 55000, int maxIncrement = 100)
    {
        boundEndPoint = null;

        for (int i = minRange; i <= (minRange+maxIncrement); i++)
        {
            var canidate = new IPEndPoint(IPAddress.IPv6Any, i);
            try
            {
                Connection.Bind(canidate);
                boundEndPoint = (IPEndPoint)Connection.LocalEndPoint!;
                return true;
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressAlreadyInUse) { continue; }
            catch (SocketException) { return false; }
        }

        return false;
    }

    //#region old
    //public static async Task<ReceivedPacket<IPacketHeaderIdentifier>?> ReceiveFullPacketAsync(this Socket Connection, /*PacketHelper Helper,*/ int Timeout = 0)
    //{
    //    CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(Timeout));

    //    int readBytes = 0;

    //    // ALLOCATE PREHEADER TO GET THE AMOUNT OF BYTES OF A PREHEADER
    //    byte[] preheader = ArrayPool<byte>.Shared.Rent(IPacketHeaderIdentifier.PreheaderLength);

    //    Memory<byte> memoryBuffer = preheader.AsMemory();
    //    try
    //    {
    //        // RECEIVES DATA UNTIL IT IS BIG ENOUGH TO BE A PREHEADER 
    //        while (readBytes < IPacketHeaderIdentifier.PreheaderLength || (!cts.IsCancellationRequested && Timeout > 0 && readBytes < IPacketHeaderIdentifier.PreheaderLength))
    //        {
    //            int receivedBytes = 0;

    //            if (Timeout > 0) receivedBytes = await Connection.ReceiveAsync(memoryBuffer, cts.Token);
    //            else receivedBytes = await Connection.ReceiveAsync(memoryBuffer);
    //            //Console.WriteLine($"looping till {IPacketHeaderIdentifier.PreheaderLength}");

    //            if (receivedBytes <= 0) return null;
    //            //Console.WriteLine($"received {receivedBytes} - needs {IPacketHeaderIdentifier.PreheaderLength}");

    //            readBytes += receivedBytes;
    //            //Console.WriteLine($"reading preheader: {readBytes}");
    //        }
    //        //Console.WriteLine($"finished reading preheader: {readBytes}");


    //        if (!(readBytes == IPacketHeaderIdentifier.PreheaderLength && IPacketHeaderIdentifier.IsValidHeader(preheader, out (ushort Magic, byte Version, byte HeaderLength, int PayloadLength, long SentAt) info)))
    //        {
    //            Debug.WriteLine("if bytes arent 16 and IsValidHeader=false");
    //            return null;
    //        }

    //        int PacketLength = (info.HeaderLength + info.PayloadLength);
    //        byte[] Packet = ArrayPool<byte>.Shared.Rent(PacketLength);
    //        memoryBuffer = Packet.AsMemory();

    //        try
    //        {
    //            // POPULATE PART 1 OF THE FullPacket
    //            Array.Copy(preheader, 0, Packet, 0, IPacketHeaderIdentifier.PreheaderLength);

    //            cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
    //            while (readBytes < PacketLength || (!cts.IsCancellationRequested && Timeout > 0 && readBytes < PacketLength))
    //            {
    //                //if (Connection.Available > 0)
    //                //{
    //                //    Console.WriteLine($"📊 Socket has {Connection.Available} bytes available");
    //                //}

    //                int received = 0;

    //                //Console.WriteLine($"looping till {PacketLength}");
    //                if (Timeout > 0) received = await Connection.ReceiveAsync(memoryBuffer[IPacketHeaderIdentifier.PreheaderLength..PacketLength], cts.Token);
    //                else received = await Connection.ReceiveAsync(memoryBuffer[IPacketHeaderIdentifier.PreheaderLength..PacketLength]);
    //                //Console.WriteLine($"received {received} - needs {PacketLength}");
    //                if (received <= 0) return null;
    //                readBytes += received;
    //            }

    //            //Console.WriteLine($"finished reading FullPacket: {readBytes}");

    //            Span<byte> HeaderSpan = Packet.AsSpan(IPacketHeaderIdentifier.PreheaderLength).Slice(0, info.HeaderLength - IPacketHeaderIdentifier.PreheaderLength);

    //            var Temp = new PacketHeader();
    //            byte[] _Header = Temp.BuildFullHeader(info, HeaderSpan);
    //            var Header = PacketHeader.FromBinaryHeader(_Header.AsSpan());

    //            //Console.WriteLine(Header.ToJSON(new System.Text.Json.JsonSerializerOptions() { WriteIndented = true }));

    //            // CHECK IF WE NEED TO FORWARD THIS PACKET**
    //            // - IGNORE PACKETS WE'VE ALREADY SEEN
    //            // - ONLY CONSUME PACKET IF ITS MEANT FOR US 
    //            // - REFER TO SEND FOR COMPLETE INFORMATION

    //            //  IGNORE HERE BEFORE PROCESSING
    //            //      DECREMENT TTL

    //            //  - REJECT PACKET IF SentAt is older than 60 seconds

    //            long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    //            long age = currentTime - Header.SentAt;

    //            bool IsExpired = age > (60 * 1000);
    //            bool isDesynced = age < -10000;
    //            // PREVENT OLD PACKETS AND DESYNCED CLOCK 
    //            if (IsExpired || isDesynced)
    //            {
    //                Console.WriteLine($"expired: {IsExpired}");
    //                Console.WriteLine($"desync: {isDesynced}");
    //                return null;
    //            }
    //            //Console.WriteLine("valid age");

    //            // REJECT PACKETS WE'VE ALREADY SEEN and STORE PACKETS WE'VE JUST SEEN TO LATER REJECT
    //            if (PackerTracker.IsPacketSeen(Header.OriginPeerId, Header.PacketId, Header.SentAt)) return null;
    //            //Console.WriteLine("packet not seen");

    //            // OLD IMPLEMENTATION OF STAT TRACKING
    //            //PeerTable? Peer;
    //            //if (Helper.IsServer()) Peer = Helper.Self.TCPServer.MyPeerTable;//.NetStats.TotalBytesRead += bytesRead;
    //            //else
    //            //{
    //            //    var H = Header;
    //            //    Peer = Helper.Self.ConnectedPeers.Find(x => x.PeerId == H.OriginPeerId);
    //            //}

    //            //if (Peer is not null)
    //            //{
    //            //    Peer.NetStats.TotalBytesRead += readBytes;
    //            //    Peer.NetStats.LastUpdated = DateTime.UtcNow;
    //            //}


    //            //Console.WriteLine(Peer.ToJSON(new System.Text.Json.JsonSerializerOptions() { WriteIndented = true}));

    //            if (Header.RecipientPeerId == Guid.Empty)
    //            {
    //                // IF EMPTY AND DIRECT, ITS MEANT FOR US
    //                if (Header.Route == PacketRoute.Direct)
    //                {
    //                    Debug.WriteLine("Peer received packet meant for them [no PeerId, and Direct]");

    //                    ReceivedPacket<IPacketHeaderIdentifier> Received = new ReceivedPacket<IPacketHeaderIdentifier>(Packet, Header, true);

    //                    //if (Received is null) Console.WriteLine("rc null");
    //                    //else Console.WriteLine("rc not null");
    //                    // Signals ArrayPool Transfer
    //                    Packet = null;

    //                    return Received;
    //                }
    //                else if (Header.Route == PacketRoute.Broadcast)
    //                {
    //                    // THIS IS MEANT FOR US 
    //                    // DO NOT REBROADCAST EVEN USING TTL 
    //                    // NOTE: APPARENTLY REBROADING IS A "REAL BROADCAST",SO WE WILL DISREGARD ABOVE...
    //                    Console.WriteLine("Peer received packet meant for everyone [Broadcast]");

    //                    ReceivedPacket<IPacketHeaderIdentifier> Received = new ReceivedPacket<IPacketHeaderIdentifier>(Packet, Header, true);

    //                    // RIPPED FROM GOSSIP ROUTE [8-21-26]
    //                    // WILL NEED TO CONVERT TO A FUNCTION SOON

    //                    Console.WriteLine();
    //                    if (Header.TTL > 0)
    //                    {
    //                        byte[] BroadcastPacket = Received.GetFullPacketCopy();
    //                        Span<byte> BroadcastPacketSpan = BroadcastPacket.AsSpan();

    //                        // UPDATE PACKET TTL AND LASTHOPID SO WE CAN KEEP TRACK OF PREVIOUS SENDER

    //                        int offset = Header.HeaderLength - 33;
    //                        Helper.Self.PeerId.TryWriteBytes(BroadcastPacketSpan[offset..]);

    //                        byte newTTL = (byte)(Header.TTL - 1);
    //                        BroadcastPacketSpan[Header.HeaderLength - 1] = newTTL;

    //                        Console.WriteLine($"Message to broadcast => {BitConverter.ToString(BroadcastPacket)}");
    //                        Console.WriteLine($"forwarding to more peers -> {newTTL}");
    //                        Helper.Self.BroadcastForward(BroadcastPacket, Header, Helper.Self.PeerId);
    //                    }

    //                    // Signals ArrayPool Transfer
    //                    Packet = null;

    //                    return Received;
    //                }
    //                else if (Header.Route == PacketRoute.Gossip)
    //                {
    //                    // IF WE ARE STILL AUTHENTICATING WE ONLY WANT TO PASS THIS INFORMATION AROUND
    //                    // DO NOT CONSUME THIS UNTIL AUTHENTICATION IS COMPLETE
    //                    if (Helper.IsAuthenticating)
    //                    {
    //                        Console.WriteLine("this is still authenticating letsskip!");
    //                        return null;
    //                    }

    //                    // THIS IS ALSO MEANT FOR US 
    //                    // GOSSIP TO OTHER PEERS BASED ON TTL
    //                    Console.WriteLine("Peer received packet meant to gossip [Select based on TTL]");

    //                    // IF THIS GOSSIP HAS A RECIPIENT END THE GOSSIP THERE
    //                    // MAYBE ADD AN OPTION TO CONTINUE BUT TARGET FOR RECIPIENT 
    //                    // MAYBE EVERYONE WHO RECEIVES IT CAN GET REPORTED BACK TO THE ORIGIN PEER?

    //                    // Store reference to this ReceivedPacket as is then modify for gossip
    //                    ReceivedPacket<IPacketHeaderIdentifier> Received = new ReceivedPacket<IPacketHeaderIdentifier>(Packet, Header, true);

    //                    if (Header.TTL > 0)
    //                    {
    //                        byte[] GossipPacket = Received.GetFullPacketCopy();
    //                        Span<byte> GossipPacketSpan = GossipPacket.AsSpan();

    //                        // UPDATE PACKET TTL AND LASTHOPID SO WE CAN KEEP TRACK OF PREVIOUS SENDER

    //                        int offset = Header.HeaderLength - 33;
    //                        Helper.Self.PeerId.TryWriteBytes(GossipPacketSpan[offset..]);
    //                        //GossipPacketSpan[Header.HeaderLength - 33] = 

    //                        byte newTTL = (byte)(Header.TTL - 1);
    //                        GossipPacketSpan[Header.HeaderLength - 1] = newTTL;

    //                        Console.WriteLine($"Message to gossip => {BitConverter.ToString(GossipPacket)}");
    //                        Helper.Self.GossipForward(GossipPacket, Header, Math.Min(2, Helper.Self.ConnectedPeers.Count()), Helper.Self.PeerId);
    //                    }

    //                    // Signals ArrayPool Transfer
    //                    Packet = null;

    //                    return Received;
    //                }
    //            }
    //            else if (Header.RecipientPeerId == Helper.Self.PeerId)
    //            {
    //                Debug.WriteLine("Peer received packet meant for them [PeerId]");
    //                ReceivedPacket<IPacketHeaderIdentifier> Received = new ReceivedPacket<IPacketHeaderIdentifier>(Packet, Header, true);

    //                // Signals ArrayPool Transfer
    //                Packet = null;

    //                return Received;
    //            }
    //            else
    //            {
    //                Debug.WriteLine("Peer received packet that is included in the unkown scope");
    //                // FORWARD OUR PACKET HERE BASED ON WHATEVER ROUTING RULES WE HAVE 
    //                // OR DIRECTLY TO THE RECIPIENT ITS MEANT FOR 
    //                // BROADCAST 

    //                // NOT SURE WHAT THIS SCOPE IS FOR REALLY AS EVERYTHING ELSE IS DEFINED ABOVE

    //                return null;
    //            }

    //            //Received = new ReceivedPacket<IPacketHeaderIdentifier>(Packet, Header, true);

    //            //// SIGNALED ArrayPool Transfer
    //            //Packet = null;

    //            return null;

    //        }
    //        //catch (Exception Ex) { Console.WriteLine(Ex.ToString()); }
    //        finally { if (Packet is not null) ArrayPool<byte>.Shared.Return(Packet); }
    //    }
    //    finally { ArrayPool<byte>.Shared.Return(preheader);  }

    //    return null;


    //}
    //#endregion
}
