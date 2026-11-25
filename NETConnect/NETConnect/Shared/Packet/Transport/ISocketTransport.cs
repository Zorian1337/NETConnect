using NETConnect.Shared.Packet.Headers;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;

namespace NETConnect.Shared.Packet.Transport;

public interface ISocketTransport : IAsyncDisposable
{
    bool IsConnected { get; }
    EndPoint? RemoteEndPoint { get; }
    ChannelReader<ReceivedPacket<IPacketHeaderIdentifier>> Incoming { get; }


    Socket GetSocket();
    Task ConnectAsync(EndPoint endpoint, CancellationToken ct);
    //PacketEncoding? Encoding = default, PacketEncryption? Encryption = default, PacketRoute? Route = default, Guid? OriginPeerId = default, Guid? LastHopId = default, Guid? RecipientPeerId = default, byte TTL = 7, byte[]? Payload = default, CancellationToken? ct = default
    Task SendAsync(CancellationToken ct, byte[]? Payload = default, PacketType Type = PacketType.Data, PacketAction Action = PacketAction.NONE, PacketRoute? Route = default, Guid? RecipientPeerId = default);
    Task SendAsync(Guid PeerId, CancellationToken ct, byte[]? Payload = default, PacketType Type = PacketType.Data, PacketAction Action = PacketAction.NONE, PacketRoute? Route = default, Guid? RecipientPeerId = default);
    Task StartReceiveLoopAsync(CancellationToken ct);

    
}
