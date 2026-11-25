using NETConnect.MyExtensions;
using NETConnect.Shared.Packet.Headers;
using NETConnect.Shared.Peers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using static NETConnect.MyExtensions.SocketExtensions;

namespace NETConnect.Shared.Packet.Transport;

public sealed class TCPTransport : ISocketTransport
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


    public TCPTransport(Socket Connection)
    {
        _socket = Connection;
        Storage = _socket.GetOrCreateBufferStorage();
        _channel = Storage.Packets;
    }

    public TCPTransport(ref Peer Self)
    {
        //_current = this;
        this.Self = Self;
        _socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
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

        PacketHeader Header = new PacketHeader(Type, Action);
        Header.SetPacketRouting(Self.GetPeerId(), Route, RecipientPeerId);
        await _socket.SendTCPPacketAsync(Header, Payload);
    }

    public async Task SendAsync(Guid SenderId, CancellationToken ct, byte[]? Payload = default, PacketType Type = PacketType.Data, PacketAction Action = PacketAction.NONE, PacketRoute? Route = default, Guid? RecipientPeerId = default)
    {
        PacketHeader Header = new PacketHeader(Type, Action);
        Header.SetPacketRouting(SenderId, Route, RecipientPeerId);
        await _socket.SendTCPPacketAsync(Header, Payload);
    }

    // TCP: length-prefix reassembly, one packet per frame
    public async Task StartReceiveLoopAsync(CancellationToken ct)
    {
        var writer = _channel.Writer;
        try
        {
            while (!ct.IsCancellationRequested && _socket.Connected)
            {
                var Packet = await _socket.ReceiveTCPPacketAsync();
                if (Packet is null) continue;
                await writer.WriteAsync(Packet, ct);
            }
        }
        catch (OperationCanceledException) { }
        catch (SocketException) { }
        finally { writer.TryComplete(); }
    }

    public ValueTask DisposeAsync() { _socket.Dispose(); return ValueTask.CompletedTask; }
}
