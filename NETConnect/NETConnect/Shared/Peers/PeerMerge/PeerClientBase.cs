using NETConnect.MyExtensions;
using NETConnect.Shared.Packet;
using NETConnect.Shared.Packet.Headers;
using NETConnect.Shared.Packet.Transport;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NETConnect.Shared.Peers.PeerMerge;

public class PeerClientBase
{
    public Peer Self { get; set; }



    public ISocketTransport Tcp { get; set; }
    public ISocketTransport Udp { get; set; }
    public CancellationTokenSource TokenSource { get; set; }
    public CryptoContext Crypto { get; set; } = new();

    public  event Action? OnSecureChannelEstablished;
    public PeerClientBase(ref Peer self)
    {
        if (OnSecureChannelEstablished is null) OnSecureChannelEstablished += OnSecureConnection;

        Tcp = new TCPTransport(ref self);
        Udp = new UDPTransport(ref self);
        this.Self = self;
    }

    public async Task ConnectAsync(EndPoint endpoint, CancellationToken ct)
    {
        await Tcp.ConnectAsync(endpoint, ct);
        await Udp.ConnectAsync(endpoint, ct);
        //_ = Task.WhenAll(Tcp.StartReceiveLoopAsync(ct), Udp.StartReceiveLoopAsync(ct));

        _ = Task.Run(async () => Tcp.StartReceiveLoopAsync(ct));
        _ = Task.Run(async () => Udp.StartReceiveLoopAsync(ct));

        //var result = await Handshake.EstablishSecureChannelAsync(this, ct);
        //if (!result.IsSuccess)
        //    throw new InvalidOperationException(result.ErrorMessage);

        OnSecureChannelEstablished?.Invoke();
    }

    public async Task ConnectAsync(IPAddress Address, int Port)
    {
        if (TokenSource is null) TokenSource = new CancellationTokenSource();
        else if (TokenSource.IsCancellationRequested) TokenSource = new CancellationTokenSource();

        EndPoint endpoint = new IPEndPoint(Address, Port);
        await Tcp.ConnectAsync(endpoint, TokenSource.Token);
        await Udp.ConnectAsync(endpoint, TokenSource.Token);

        //_ = Task.Run(async () => Tcp.StartReceiveLoopAsync(TokenSource.Token));
        //_ = Task.Run(async () => Udp.StartReceiveLoopAsync(TokenSource.Token));

        //_ = Task.WhenAll(Tcp.StartReceiveLoopAsync(TokenSource.Token), Udp.StartReceiveLoopAsync(TokenSource.Token));

        await Handshake.EstablishSecureClientChannelAsync(this, TokenSource.Token);

        //OnSecureChannelEstablished?.Invoke();
    }

    public async Task ConnectAsync(string Host, int Port)
    {
        if (TokenSource is null) TokenSource = new CancellationTokenSource();
        else if (TokenSource.IsCancellationRequested) TokenSource = new CancellationTokenSource();

        if (IPAddress.TryParse(Host, out IPAddress? addr))
        {
            EndPoint endpoint = new IPEndPoint(addr, Port);
            await Tcp.ConnectAsync(endpoint, TokenSource.Token);
            await Udp.ConnectAsync(endpoint, TokenSource.Token);

            _ = Task.WhenAll(Tcp.StartReceiveLoopAsync(TokenSource.Token), Udp.StartReceiveLoopAsync(TokenSource.Token));

            //OnSecureChannelEstablished?.Invoke();
        }
    }

    public async Task SendAsync(bool Reliable, CancellationToken ct, byte[]? Payload = default, PacketType Type = PacketType.Data, PacketAction Action = PacketAction.NONE, PacketRoute? Route = default, Guid? RecipientPeerId = default) => 
        await (Reliable ? Tcp : Udp).SendAsync(ct, Payload, Type, Action, Route, RecipientPeerId);

    public async Task<ReceivedPacket<IPacketHeaderIdentifier>> ReceiveAsync(bool Reliable, CancellationToken ct)
    {
        var Reader = (Reliable ? Tcp : Udp).Incoming;

        var Packet = await Reader.ReadAsync(ct);
        if (Packet is null) return default;
        return Packet;
    }

    public record ReceivedFirstPacket(ReceivedPacket<IPacketHeaderIdentifier> Packet, bool IsTCP);
    public async Task<ReceivedFirstPacket?> ReceiveFirstAsync(CancellationToken ct)
    {
        var tcpRead = Tcp.Incoming.ReadAsync(ct).AsTask();
        var udpRead = Udp.Incoming.ReadAsync(ct).AsTask();

        var first = await Task.WhenAny(tcpRead, udpRead);
        _ = (first == tcpRead ? udpRead : tcpRead);
        bool isTCP = first == tcpRead;
        return new ReceivedFirstPacket(await first, isTCP);
    }

    //public record RecieveAllAsync()
    public async Task<(List<ReceivedPacket<IPacketHeaderIdentifier>>? TCPPackets, List<ReceivedPacket<IPacketHeaderIdentifier>>? UDPPackets)?> ReceiveAllAsync(CancellationToken ct)
    {
        Console.WriteLine(Udp.Incoming.WaitToReadAsync(ct));
        var tpList = await Tcp.Incoming.ReadAllAsync(ct).ToListAsync();
        var upList = await Udp.Incoming.ReadAllAsync(ct).ToListAsync();

        

        if (upList is null) Console.WriteLine("");
        Console.WriteLine(upList?.Count());

        foreach (var uPacket in upList)
        {
            Console.WriteLine(uPacket.ToIndentedJSON());
        }

        //return (tpList, upList);
        //_ = Task.Run(async () =>
        //{
        //    var tpList = await Tcp.Incoming.ReadAllAsync(ct).ToListAsync();
        //    tpList = tpList.OrderBy(x => ((PacketHeader)x.Header).PacketId).ToList();

        //    foreach (var tPacket in tpList)
        //    {

        //    }
        //});

        //_ = Task.Run(async () =>
        //{
        //    var upList = await Udp.Incoming.ReadAllAsync(ct).ToListAsync();
        //    upList = upList.OrderBy(x => ((PacketHeader)x.Header).PacketId).ToList();

        //    foreach (var uPacket in upList)
        //    {

        //    }
        //});
        return null;
    }

    //private Packet Pack(byte[] payload) { /* build header + encrypt */ }
    //private byte[] Unpack(Packet packet) { /* verify + decrypt */ }

    public async void OnSecureConnection()
    {
        Console.WriteLine("[CLIENT] Connection secured");

        _ = Task.Run(async () =>
        {
            while (!TokenSource.Token.IsCancellationRequested)
            {
                Console.WriteLine("[CLIENT] SENDING HELLO");
                await Tcp.SendAsync(TokenSource.Token, "HELLO FROM THE CLIENT".ToUTF8Byte());
                await Task.Delay(new Random().Next(2000, 5000));
            }
        });


        _ = Task.Run(async () =>
        {
            while (!TokenSource.Token.IsCancellationRequested)
            {
                // DO HEARTBEAT
                Console.WriteLine("[CLIENT] BEAT");

                var Packet = await ReceiveFirstAsync(TokenSource.Token);//ReceiveAsync(false, TokenSource.Token);
                if (Packet is null) continue;
                Console.WriteLine("[END] returned packet is null");
                //Console.WriteLine(Packet.ToIndentedJSON());


                // READ AND HANDLE MESSAGE
                //var Packet = await ReceiveAllAsync(TokenSource.Token);
                //Console.WriteLine(Packet.Value.UDPPackets.ToIndentedJSON());
                //Console.WriteLine(Packet.UDPPackets.ToIndentedJSON());
            }
        });

        
    }
}
