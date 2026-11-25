using NETConnect.MyExtensions;
using NETConnect.Shared.Packet.Headers;
using NETConnect.Shared.Peers;
using NETConnect.Shared.Peers.PeerMerge;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using static NETConnect.MyExtensions.SocketExtensions;

namespace NETConnect.Shared.Packet.Transport;

public sealed class UDPTransport : ISocketTransport
{
    /// <summary>
    /// This will only be valid for direct entities.
    /// Client and Server will have valid values, not ServerClients.
    /// </summary>
    public Peer Self { get; set; }


    public Socket _socket { get; set; }
    private SocketBufferStorage Storage { get; set; }
    public bool IsConnected => _socket.Connected;
    public EndPoint? RemoteEndPoint { get; private set; }
    private Channel<ReceivedPacket<IPacketHeaderIdentifier>> _channel { get; set; }
    public ChannelReader<ReceivedPacket<IPacketHeaderIdentifier>> Incoming => _channel.Reader;

    public UDPTransport(Socket Connection)
    {
        _socket = Connection;
        Storage = _socket.GetOrCreateBufferStorage();
        _channel = Storage.Packets;

    }
    public UDPTransport(ref Peer Self)
    {
        //_current = this;
        this.Self = Self;
        _socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
        _socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);

        Storage = _socket.GetOrCreateBufferStorage();
        _channel = Storage.Packets;
    }

    public void Init(Socket Connection)
    {
        _socket = Connection;
        Storage = _socket.GetOrCreateBufferStorage();
        _channel = Storage.Packets;
    }

    public Socket GetSocket() => _socket;

    public async Task ConnectAsync(EndPoint ep, CancellationToken ct)
    {
        RemoteEndPoint = ep;
        await _socket.ConnectAsync(ep, ct);
    }


    public async Task SendAsync(CancellationToken ct, byte[]? Payload = default, PacketType Type = PacketType.Data, PacketAction Action = PacketAction.NONE, PacketRoute? Route = default, Guid? RecipientPeerId = default)
    {
        if (Self is null) return;

        //await _socket.SendToAsync(packet, SocketFlags.None, RemoteEndPoint!, ct);

        PacketHeader Header = new PacketHeader(Type, Action);
        Header.SetPacketRouting(Self.GetPeerId(), Route, RecipientPeerId);

        await _socket.SendUDPPacketAsync(RemoteEndPoint, Header, Payload);
    }

    public async Task SendAsync(Guid PeerId, CancellationToken ct, byte[]? Payload = default, PacketType Type = PacketType.Data, PacketAction Action = PacketAction.NONE, PacketRoute? Route = default, Guid? RecipientPeerId = default)
    {
        //await _socket.SendToAsync(packet, SocketFlags.None, RemoteEndPoint!, ct);

        PacketHeader Header = new PacketHeader(Type, Action);
        Header.SetPacketRouting(PeerId, Route, RecipientPeerId);

        await _socket.SendUDPPacketAsync(RemoteEndPoint, Header, Payload);
    }

    //public async Task StartServerReceiveLoopAsync(PeerServerClient client)
    //{

    //}

    public async Task StartReceiveLoopAsync(CancellationToken ct)
    {
        var writer = _channel.Writer;

        //Console.WriteLine($"flag -> {!ct.IsCancellationRequested && _socket.Connected}");
        //Console.WriteLine($"IsNotCanceled: {!ct.IsCancellationRequested} - IsConnected: {_socket.Connected}");

        while (!ct.IsCancellationRequested) // && _socket.Connected
        {
            try
            {
                Console.WriteLine("checking for udp packets");

                var Local = ((IPEndPoint)_socket.LocalEndPoint);
                Console.WriteLine($"Address: {Local.Address} - {Local.Port}");

                var Packet = await _socket.ReceiveUDPPacketAsync(true);
                if (Packet is null) { Console.WriteLine("UDP PACKET IS NULL"); continue; }
                Console.WriteLine("udp packet not null");

                await writer.WriteAsync(Packet, ct);

            }
            catch (Exception Ex) { Console.WriteLine(Ex.ToString()); }
            finally { } //writer.TryComplete();

        }
    }
    public ValueTask DisposeAsync() { _socket.Dispose(); return ValueTask.CompletedTask; }
}
