using NETConnect.Shared.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETConnect.Peers;

// I need to make sure that the client will be able to use this aswell 

/// <summary>
/// Combines Client and Peers into one managable area
/// </summary>
public class PeerHandle
{
    public Peer Self { get; private set; }

    public PeerHandle(ref Peer Self, ref  ServerClientHandle ClientHandle)
    {
        this.Self = Self;
        this.ClientHandle = ClientHandle;
    }

    public ServerClientHandle ClientHandle { get; private set; }


    // Peer grouping related
    public IEnumerable<PeerTable> DiscoveredPeers { get; private set; }
    public IEnumerable<PeerTable> ConnectedPeers { get; private set; }
    public int MaxPeerConnections { get; private set; } = 10;





    void AddPeer(PeerTable initPeer)
    {
        // Send known peers to new client
        ClientHandle.PacketHelper.SendPacket(Self.Peers.ToArray().ToJSON().ToUTF8Byte(), PacketActionType.PeerJoin);

        // Link ServerClientHandle data to PeerId
        ClientHandle.UpdateClientId(initPeer.PeerId); // do something around here to link Self.Peers to the Server Client Connections

        var Helper = ClientHandle.PacketHelper;
        PeerTable newPeer = new PeerTable(ref Helper, initPeer.PeerId, initPeer.Address, initPeer.Port);
        Self.Peers.Add(newPeer);
    }

    void AddPeers(IEnumerable<PeerTable> initPeers, bool UseOriginalVersion = false)
    {
        // Send known peers to new client
        ClientHandle.PacketHelper.SendPacket(Self.Peers.ToArray().ToJSON().ToUTF8Byte(), PacketActionType.PeerJoin);

        if (Self.IsUniquePeers(initPeers.ToList(), out IEnumerable<PeerTable> newPeers))
        {
            var SelfPeer = Self;

            if (UseOriginalVersion)
            {
                List<PeerTable> UniquePeers = new List<PeerTable>();

                foreach (var peer in newPeers)
                {

                    BaseTCPClient connection = new BaseTCPClient(ref SelfPeer);
                    peer.Client = connection;
                    if (connection.TryConnect(peer.Address, peer.Port))
                    {
                        // Add them to unique list
                        UniquePeers.Add(peer);

                        // Add them to current peer list
                        Self.Peers.Add(peer);

                        //Console.WriteLine("found new peer");
                    }
                }

                // Update all peers on the new connections (im being lazy and not making sure that new peers are excluded, it'll help with discovery for now)
                Self.Peers.ForEach(peer => peer.Client.Packer.SendPacket(UniquePeers.ToArray().ToJSON().ToUTF8Byte(), PacketActionType.P2PInt));
            }
            else
            {
                // Discovery / Max connection system based on (uptime / ping / regions)
            }
        }
    }



    void KickPeer()
    {

    }

    void BanPeer()
    {

    }

}