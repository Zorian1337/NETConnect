using NETConnect.Shared.Packet.Headers;
using NETConnect.Shared.Packet.Transport;
using System;
using System.Collections.Generic;
using System.Text;

namespace NETConnect.Shared.Peers.PeerMerge;

public class PeerServerClient
{
    public PeerServerBase Server { get; set; }

    public Guid ClientId { get; set; }
    public ISocketTransport Tcp { get; set; }
    public ISocketTransport Udp { get; set; }
    public CancellationToken Token { get; set; }
    public CryptoContext Crypto { get; set; } = new();

    public PeerServerClient(ref PeerServerBase Server, TCPTransport Tcp, CancellationToken Token)
    {
        this.Server = Server;
        this.Tcp = Tcp;
    }

    public async Task SendAsync(bool Reliable, CancellationToken ct, byte[]? Payload = default, PacketType Type = PacketType.Data, PacketAction Action = PacketAction.NONE, PacketRoute? Route = default, Guid? RecipientPeerId = default)
    {
        Console.WriteLine("SERVER SENDING TO CLIENT");
        await (Reliable ? Tcp : Udp).SendAsync(ClientId, ct, Payload, Type, Action, Route, RecipientPeerId);
        Console.WriteLine("SERVER SENT TO CLIENT");
    }


}
