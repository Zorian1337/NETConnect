using NETConnect.Network.Info;
using NETConnect.Shared;
using NETConnect.Shared.Multicast;
using NETConnect.Shared.Packet.Headers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NETConnect.Peers;

public enum PeerState { Server, Peer}

public class Peer
{
    public Guid PeerId { get; set; } = Guid.NewGuid();

    public BaseTCPServer TCPServer { get; set; }

    public List<PeerTable> ConnectedPeers { get; set; } = new List<PeerTable>();

    public Multicast Multicast { get; set; }

    /// <summary>
    /// Quick way to detect if current state is peer or server
    /// </summary>
    public PeerState OperationMode
    {
        get
        {
            if(TCPServer.Clients.Count >= 0 && ConnectedPeers.Count() == 0) return PeerState.Server;
            else if(ConnectedPeers.Count() > 0) return PeerState.Peer;
            else return PeerState.Server;
        }
    }


    public Peer(IPAddress Address, int Port)
    {
        // Init our server/client
        var Self = this;

        TCPServer = new BaseTCPServer(ref Self, Address, Port);

        // Start our server, as having multicast up and our TCPServer is the most important (client is used to connect to other Peer Servers) - might need to change some plans around later 
        TCPServer.StartServer();
    }







    // Peer grouping related
    public NetworkStats NetStats { get; set; } 

    public IEnumerable<PeerTable> DiscoveredPeers { get; private set; }
    public PeerSettings Settings { get; set; } = new PeerSettings()
    {
        // Use this to update peers settings on init
    };



    public void AddPeer(ServerClientHandle ClientHandle, PeerTable initPeer)
    {
        Console.WriteLine($"AddPeer: {initPeer.ToJSON()}");

        // Send known peers to new client
        ClientHandle.PacketHelper.SendPacket(ConnectedPeers.ToArray().ToJSON().ToUTF8Byte(), PacketActionType.PeerJoin);

        // Link ServerClientHandle data to PeerId
        ClientHandle.UpdateClientId(initPeer.PeerId); // do something around here to link Self.Peers to the Server Client Connections
        //this.TCPServer.pack

        var Helper = ClientHandle.PacketHelper;
        PeerTable newPeer = new PeerTable(ref Helper, initPeer.PeerId, initPeer.Address, initPeer.Port);
        ConnectedPeers.Add(newPeer);
    }

    public void AddPeers(ServerClientHandle ClientHandle, IEnumerable<PeerTable> initPeers, bool UseOriginalVersion = false)
    {
        // Make sure our peer list is unique, and make sure the new peer list is unique
        ConnectedPeers = ConnectedPeers.Distinct().ToList();
        initPeers = initPeers.Distinct().ToList();

        // Send known peers to new client
        ClientHandle.PacketHelper.SendPacket(ConnectedPeers.ToArray().ToJSON().ToUTF8Byte(), PacketActionType.PeerJoin);

        if (this.IsUniquePeers(initPeers.ToList(), out IEnumerable<PeerTable> newPeers))
        {
            var SelfPeer = this;

            if (UseOriginalVersion)
            {
                List<PeerTable> UniquePeers = new List<PeerTable>();

                foreach (var peer in newPeers)
                {
                    Console.WriteLine($"AddPeer: {peer.ToJSON()}");
                    BaseTCPClient connection = new BaseTCPClient(ref SelfPeer);
                    peer.Client = connection;
                    if (connection.TryConnect(peer.Address, peer.Port))
                    {
                        // Add them to unique list
                        UniquePeers.Add(peer);

                        // Add them to current peer list
                        ConnectedPeers.Add(peer);

                        //Console.WriteLine("found new peer");
                    }
                }

                // Update all peers on the new connections (im being lazy and not making sure that new peers are excluded, it'll help with discovery for now)
                ConnectedPeers.ForEach(peer => peer.Client.Packer.SendPacket(UniquePeers.ToArray().ToJSON().ToUTF8Byte(), PacketActionType.P2PInt));
            }
            else
            {
                // Discovery / Max connection system based on (uptime / ping / regions)
            }
        }
    }




}

public static class PeerUtils
{
    public static (ServerClientHandle, PeerTable)? FindPeerById(this Peer Self, Guid PeerId)
    {
        if (Self.ConnectedPeers.Any(x => x.PeerId == PeerId) && Self.TCPServer.Clients.Any(x => x.PacketHelper.ClientHandle.Id == PeerId))
        {
            PeerTable Peer = Self.ConnectedPeers.Find(x => x.PeerId == PeerId);
            ServerClientHandle Client = Self.TCPServer.Clients.Find(x => x.PacketHelper.ClientHandle.Id == PeerId);
            
            return(Client, Peer);
        }
        else return default; // No matching peers
    }


    /// <summary>
    /// Checks to see if any peers is either connected or is known in the provided list
    /// </summary>
    /// <param name="Self"></param>
    /// <param name="PeerList"></param>
    /// <param name="UniquePeers"></param>
    /// <returns></returns>
    public static bool IsUniquePeers(this Peer Self, IEnumerable<PeerTable> PeerList, out IEnumerable<PeerTable> UniquePeers)
    {
        UniquePeers = Enumerable.Empty<PeerTable>().ToList();

        try
        {
            IEnumerable<PeerTable> Peers = GetUniquePeers(Self, PeerList);

            if (Peers is null || Peers.Count() == 0) return false;
            else
            {
                UniquePeers = Peers.Distinct().ToList();
                return true;
            }
        }
        catch (Exception Ex) {  } // Populate catch later

        return false;
    }




    public static bool IsConnectedPeer()
    {
        return false;
    }

    public static bool IsKnownPeer(Guid SelfId, List<PeerTable> Peers, PeerTable newPeer)
    {


        return false;
    }

    public static IEnumerable<PeerTable> GetUniquePeers(Peer Self, IEnumerable<PeerTable> PeerList) => PeerList.Where(x => !(Self.ConnectedPeers.Any(a => x.PeerId == a.PeerId) && x.PeerId != Self.PeerId)).ToList();
}