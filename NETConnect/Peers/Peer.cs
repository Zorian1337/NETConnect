using NETConnect.Shared.Multicast;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NETConnect.Peers;

public class Peer
{
    public Guid PeerId
    {
        get => Multicast.SenderId;
    }

    // So each peer is a server and a client

    // Locally clients will use udp multicast groups to look for groups

    // List of clients inside the peer so that each peer can be connected to others
    public List<BaseTCPClient> Clients { get; set; } = new List<BaseTCPClient>();

    public List<PeerTable> Peers { get; set; } = new List<PeerTable>();

    
    public BaseTCPServer TCPServer { get; set; }
    public Multicast Multicast { get; set; }

    /// <summary>
    /// Quick way to detect if current state is peer or server
    /// </summary>
    public PeerState OperationMode
    {
        get
        {
            if(TCPServer.Clients.Count >= 0 && Peers.Count() == 0) return PeerState.Server;
            else if(Peers.Count() > 0) return PeerState.Peer;
            else return PeerState.Server;
        }
    }


    public Peer(IPAddress Address, int Port)
    {
        // Join multicast group immediately, then later scout for information (peer related)
        //Multicast = new Multicast();
        //Multicast.ReadMulticast(); // Scout for other peers on the network for our TCPClient to connect to (data exchange) - might need to rework some stuff later regarding this

        // Init our server/client
        //TCPClient = new BaseTCPClient();  
        var Self = this;
        TCPServer = new BaseTCPServer(ref Self, Address, Port);

        // Start our server, as having multicast up and our TCPServer is the most important (client is used to connect to other Peer Servers) - might need to change some plans around later 
        TCPServer.StartServer();
    }


public static class PeerUtils
{
    public static (ServerClientHandle, PeerTable) FindPeerById(this Peer Self, Guid PeerId)
    {
        if (Self.Peers.Any(x => x.PeerId == PeerId) && Self.TCPServer.Clients.Any(x => x.PacketHelper.ClientHandle.Id == PeerId))
        {
            PeerTable Peer = Self.Peers.Find(x => x.PeerId == PeerId);
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
    public static bool IsUniquePeers(this Peer Self, List<PeerTable> PeerList, out List<PeerTable> UniquePeers)
    {
        UniquePeers = Enumerable.Empty<PeerTable>().ToList();

        try
        {
            List<PeerTable> Peers = GetUniquePeers(Self, PeerList);

            if (Peers is null || Peers.Count == 0) return false;
            else
            {
                UniquePeers = Peers;
                return true;
            }
        }
        catch (Exception Ex) {  } // Populate catch later


    //    // Host a server on the local port 

    //    //
    //}


}


    public static List<PeerTable> GetUniquePeers(Peer Self, List<PeerTable> PeerList) => PeerList.Where(x => !(Self.Peers.Any(a => x.PeerId == a.PeerId) && x.PeerId != Self.PeerId)).ToList();
}