using System;
using System.Collections.Generic;
using System.Text;

using NETConnect.Shared.Identity;
using NETConnect.Shared.Packet.Transport;
using NETConnect.Shared.Peers.PeerMerge;

namespace NETConnect.Shared.Peers;

public class Peer
{
    public Guid GetPeerId() => Identity.PeerId;
    public PeerIdentity Identity { get; set; }
    public PeerServerBase Server { get; set; }
    public PeerClientBase Client { get; set; }
    public Multicast LAN { get; set; }
    public CancellationTokenSource PeerToken { get; set; } = new CancellationTokenSource();



    public async Task InitPeer()
    {
        var Self = this;
        Identity = PeerIdentity.GetOrCreateExistingIdentityAsync("");

        Console.WriteLine(GetPeerId());

        if (Client is null) Client = new PeerClientBase(ref Self);
        if (LAN is null) LAN = new Multicast(ref Self);
        if (Server is null) Server = new PeerServerBase(ref Self);

    }

    public async Task StartServer()
    {
        await InitPeer();
        await Server.StartServerAsync(PeerToken.Token);
    }
}
