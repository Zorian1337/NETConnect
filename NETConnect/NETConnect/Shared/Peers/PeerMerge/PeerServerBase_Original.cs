using NETConnect.MyExtensions;
using NETConnect.Shared.Packet.Headers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NETConnect.Shared.Peers.PeerMerge;

public class PeerServerBase_Original
{
    public Peer Self { get; set; }
    public IPEndPoint EndPoint { get; set; }
    public Socket TCPServer { get; set; }
    public Socket UDPServer { get; set; }
    public CancellationTokenSource ServerToken { get; set; }

    public event Action<Socket> OnTCPClientConnected;

    public PeerServerBase_Original(ref Peer Self) => this.Self = Self;

    public void InitPeer(int Port = 0)
    {
        if (OnTCPClientConnected is null) OnTCPClientConnected += HandleOnTCPClientConnected;

        if (TCPServer is null)
        {
            // START AS IPv6 SO WE CAN SUPPORT ALL NETWORK TYPES
            TCPServer = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
            if (TCPServer.AddressFamily == AddressFamily.InterNetworkV6) TCPServer.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false); // with our IPv4 supported in the IPv6 we need to map to IPv4 if we have one (we will)
        }

        if (UDPServer is null)
        {
            UDPServer = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
            if (UDPServer.AddressFamily == AddressFamily.InterNetworkV6) UDPServer.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
        }

        if (ServerToken is null) ServerToken = new CancellationTokenSource();
        //EndPoint = new IPEndPoint(IPAddress.IPv6Any, Port);
    }

    public void StartServerAsync(int Port = 0, int Backlog = 0)
    {
        IPAddress Address = Network.NetworkUtils.GetLocalLanIp();

        InitPeer();
        
        if(!TCPServer.TryBindToAvailablePort(out IPEndPoint EndPoint))
        {
            Debug.WriteLine("Failed to bind to ports using port range scanner");
            return;
        }

        // CAPTURE BOUND PORT FOR TRACKING LATER
        int assignedPort = ((IPEndPoint)TCPServer.LocalEndPoint).Port;
        Port = assignedPort;

        TCPServer.Listen(Backlog);
        UDPServer.Bind(EndPoint);

        var LAN = Self.LAN;
        _ = LAN.ReadMulticastAsync();

        _ = Task.Run(async () =>
        {
            
            // ADVERTISE SERVER ON LAN AFTER ITS STARTS ON TCP
            while(!ServerToken.IsCancellationRequested && !Self.LAN.Token.IsCancellationRequested)
            {
                //Console.WriteLine("looping");

                await LAN.Join(Address, Port); // send as an event not just constantly*
                await Task.Delay(new Random().Next(5000, 10000));
            }
        });

        _ = Task.Run(async () =>
        {
            while (UDPServer is not null && !ServerToken.IsCancellationRequested)
            {
                var UDPStorage = UDPServer.GetOrCreateBufferStorage();
                var Writer = UDPStorage.Packets.Writer;

                var Packet = await UDPServer.ReceiveUDPPacketAsync();
                if (Packet is null) { Console.WriteLine("UDP Packet is null"); continue; }
                
                // WE'LL WRITE TO THE PACKETS BUT WE'LL HANDLE ALL UDP REQUESTS IN THE TCP CLIENT HANDLE
                // UDP SHOULD ONLY BE CONNECTED AFTER WE CONNECT OUR TCP CLIENT
                // ALL UDP PACKETS WILL BE LINKED TO THE SAME TCP THROUGH PEER ID 
                if (await Writer.WaitToWriteAsync())
                {
                    // ASSIGN TO PACKET QUEUE
                    // USE READER TO DEQUEUE AND READ PACKETS
                    await Writer.WriteAsync(Packet);

                    Console.WriteLine($"[FromClient] UDP -> {BitConverter.ToString(Packet.GetFullPacketCopy())}\n{Packet.Payload.Span.ToUTF8String()}");
                }
            }
        });

        _ = Task.Run(async () =>
        {
            while (TCPServer is not null && !ServerToken.IsCancellationRequested)
            {
                Socket Client = await TCPServer.AcceptAsync();
                OnTCPClientConnected?.Invoke(Client);
            }
        });
    }

    public async void HandleOnTCPClientConnected(Socket TCPClient)
    {
        Console.WriteLine($"Client [TCP] connected -> {TCPClient.RemoteEndPoint.ToString()}");
        CancellationTokenSource TCPToken = new CancellationTokenSource();

        bool IsValidPacketCheckStatement = TCPClient is not null && !TCPToken.IsCancellationRequested;
       _ = Task.Run(async () => ReceiveTCPPacket(TCPClient, TCPToken, IsValidPacketCheckStatement)); // remove ability to read messages sent to server for now

        //_ = Task.Run(async () =>
        //{
        //    while (true)
        //    {
        //        var Packet = await TCPClient.ReceiveUDPPacketAsync();
        //        if (Packet is null) continue;

        //        Console.WriteLine($"[UDP] {Packet.ToIndentedJSON()}");
        //    }
        //});

        //Console.WriteLine("Test1");
        while (IsValidPacketCheckStatement)
        {
            Console.WriteLine("[SERVER] BEAT");
            // REEVALUATE OUR STATEMENT EACH TIME, CHECKS FOR SOCKET VALIDITITY
            IsValidPacketCheckStatement = TCPClient is not null && !TCPToken.IsCancellationRequested;

            var Reader = TCPClient.GetOrCreateBufferStorage().Packets.Reader;

            PacketHeader PacketHeader = new PacketHeader();

            //await TCPClient.SendTCPPacketAsync(PacketHeader, "this is a test".ToUTF8Byte());
            //Console.WriteLine("sent tcp message");

            //await UDPServer.SendUDPPacketAsync(TCPClient.RemoteEndPoint, PacketHeader, "HELLO FROM THE SERVER".ToUTF8Byte());
            //Console.WriteLine("sent UDP message");

            // HANDLE ANY RECEIVED PACKETS HERE
            using (var Packet = await Reader.ReadAsync(TCPToken.Token))
            {
                if(Packet.Payload.Length > 0)
                {
                    string Message = Encoding.UTF8.GetString(Packet.Payload.Span);
                    Console.WriteLine($"[FromClient] TCP Message -> \"{Message}\"");
                }
            }


            await Task.Delay(100);
        }
    }

    public async Task ReceiveTCPPacket(Socket TCPClient, CancellationTokenSource TCPToken, bool IsValidPacketCheckStatement)
    {
        var Writer = TCPClient.GetOrCreateBufferStorage().Packets.Writer;

        while (IsValidPacketCheckStatement)
        {
            try
            {
                Task.Run(async () =>
                {
                    Console.WriteLine("reading for udp packet");
                    var uPacket = await UDPServer.ReceiveUDPPacketAsync();
                    if (uPacket is null) { Console.WriteLine("returned packet is null"); }

                    Console.WriteLine($"[SERVER] [UDP] -> {uPacket.ToIndentedJSON()}");
                    PacketHeader PacketHeader = new PacketHeader();
                    await UDPServer.SendUDPPacketAsync(TCPClient.RemoteEndPoint, PacketHeader, "HELLO FROM THE SERVER".ToUTF8Byte());
                });


                var Packet = await TCPClient.ReceiveTCPPacketAsync();
                if (Packet is null) continue;

                if (await Writer.WaitToWriteAsync())
                {
                    await Writer.WriteAsync(Packet);

                    //Console.WriteLine($"TCP Server Received -> {BitConverter.ToString(Packet.GetFullPacketCopy())}");
                }
            }
            catch(SocketException Ex) { Console.WriteLine($"SEx: {Ex.ToString()}"); }

            // REEVALUATE OUR STATEMENT EACH TIME, CHECKS FOR SOCKET VALIDITITY
            IsValidPacketCheckStatement = TCPClient is not null && !TCPToken.IsCancellationRequested;
        }
    }
}
