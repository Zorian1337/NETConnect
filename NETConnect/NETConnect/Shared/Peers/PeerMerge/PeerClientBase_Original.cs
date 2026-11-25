using NETConnect.MyExtensions;
using NETConnect.Shared.Packet.Headers;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace NETConnect.Shared.Peers.PeerMerge;

public class PeerClientBase_Original
{
    public Peer Self { get; set; }
    public Socket TCPClient { get; set; }
    public Socket UDPClient { get; set; }
    public EndPoint RemoteEndPoint { get; set; }
    public event Action<Socket> OnTCPConnected;

    public CancellationTokenSource TCPToken { get; set; }


    public PeerClientBase_Original(ref Peer Self) => this.Self = Self;
    public void InitPeer()
    {
        if (OnTCPConnected is null) OnTCPConnected += HandleConnected;

        if (TCPClient is null)
        {
            // START AS IPv6 SO WE CAN SUPPORT ALL NETWORK TYPES
            TCPClient = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
            if (TCPClient.AddressFamily == AddressFamily.InterNetworkV6) TCPClient.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false); // with our IPv4 supported in the IPv6 we need to map to IPv4 if we have one (we will)
        }

        if (UDPClient is null)
        {
            UDPClient = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
            if (UDPClient.AddressFamily == AddressFamily.InterNetworkV6) UDPClient.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
        }

        if (TCPToken is null) TCPToken = new CancellationTokenSource();
    }

    public async Task ConnectAsync(string Host, int Port)
    {
        InitPeer();

        if (IPAddress.TryParse(Host, out IPAddress? addr))
        {
            RemoteEndPoint = new IPEndPoint(addr, Port);
            await TCPClient.ConnectAsync(RemoteEndPoint);

            if (!TCPClient.Connected) { Console.WriteLine("Client failed to connect"); return; }
            else OnTCPConnected?.Invoke(TCPClient);
        }
        else Console.WriteLine("failed to parse address");
    }

    public async Task ConnectAsync(IPAddress Address, int Port)
    {
        InitPeer();

        RemoteEndPoint = new IPEndPoint(Address, Port);
        await TCPClient.ConnectAsync(RemoteEndPoint);

        if (!TCPClient.Connected) { Console.WriteLine("Client failed to connect"); return; }
        else OnTCPConnected?.Invoke(TCPClient);
    }

    public async void HandleConnected(Socket Client)
    {
        Console.WriteLine("Client -> Connected to Server.");

        _ = Task.Run(async () => await ReceiveTCPPacket());

        PacketHeader Header = new PacketHeader(PacketType.Data, PacketAction.NONE, PacketEncoding.NONE, PacketEncryption.NONE, PacketRoute.NONE, Guid.Empty, Guid.Empty, Guid.Empty, 7);

        byte[] Payload = "testing".ToUTF8Byte();
        byte[] MSGPacket = Header.ToBinaryHeader(Payload);
        UDPClient.SendTo(MSGPacket, RemoteEndPoint);
        await EstablishSecureChannelAsync();

        string Message = String.Empty;
        var Reader = TCPClient.GetOrCreateBufferStorage().Packets.Reader;

        Header.Action = PacketAction.SYN;
        Header.Type = PacketType.Control;



        while (TCPClient is not null && !TCPToken.IsCancellationRequested)
        {

            try
            {
                if(Reader.TryRead(out var Packet))
                {
                    if (Packet.Payload.Length > 0)
                    {
                        Message = Encoding.UTF8.GetString(Packet.Payload.Span);
                        Console.WriteLine($"[FromServer] TCP Message -> \"{Message}\"");
                    }

                    Packet.Dispose();
                }

            }
            catch (Exception Ex) { Console.WriteLine(Ex.ToString()); }


            //Message = "this is a test";
            //byte[] Payload = System.Text.Encoding.ASCII.GetBytes(Message);
            //byte[] data = Header.ToBinaryHeader(Payload);
            //TCPClient.Send(data);

            await Task.Delay(100);
        }
    }


    public async Task ReceiveTCPPacket()
    {
        var Writer = TCPClient.GetOrCreateBufferStorage().Packets.Writer;

        while(TCPClient is not null && !TCPToken.IsCancellationRequested)
        {
            try
            {
                var Packet = await TCPClient.ReceiveTCPPacketAsync();
                if (Packet is null) continue;

                if (await Writer.WaitToWriteAsync())
                {
                    await Writer.WriteAsync(Packet);

                    //Console.WriteLine($"TCP Client Received -> {BitConverter.ToString(Packet.GetFullPacketCopy())}");
                }
            }
            catch (SocketException Ex) { Console.WriteLine($"SEx: {Ex.ToString()}"); }
        }
    }

    public class SecureChannelResult
    {
        public bool IsSecured { get; set; } = false;
        public bool IsSuccess { get; set; } = false;
        public string? ErrorMessage { get; set; }
    }

    public async Task<SecureChannelResult> EstablishSecureChannelAsync()
    {
        SecureChannelResult result = new SecureChannelResult();

        // SET A TIMEOUT FOR THIS AUTHENTICATION
        //   * IF TIMEOUT FAILS REJECT CONNECTION
        //   * ALLOW UNSECURED IF WE SUPPORT IT

        // TIMEOUT TIME SET TO 30s MAX 
        //   * SHOULDN'T TAKE MORE THAN 100-500ms 
        //   * ALLOW LEEWAY FOR SLOWER CONNECTIONS

        // ALLOW FOR 5 SECONDS PER RECEIVE

        return null;
    }
}
