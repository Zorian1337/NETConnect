using NETConnect.Interfaces;
using NETConnect.Network.Info;
using NETConnect.Shared;
using NETConnect.Shared.Multicast;
using NETConnect.Shared.Packet.Headers;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Swift;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NETConnect.Peers;

public enum PeerState 
{
    // If it has no clients or clients connected but no peers connected, it is a server
    Server,
    // If it has clients and peers connected, it is a Peer
    Peer
}

public class Peer
{
    public Guid PeerId { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// shared key for verifying packet data integritity
    /// </summary>
    public ECDsa PrivateSigningKey { get; set; }
    public BaseTCPServer TCPServer { get; set; }
    public List<PeerTable> ConnectedPeers { get; set; } = new List<PeerTable>();
    public Multicast Multicast { get; set; }



    // These events are further down the line compared to the Server and Client 
    // We need these here so we can access them directly rather than any other way 
    // We can still access it normally a different way but hooking it here should be way easier for us
    public event Action OnPeerConnect;
    public event Action OnPeerDisconnect;
    public event Action OnPeerDataReceived; //- IDK This is all so confusing 

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

        // Generate our packet signature key
        

        TCPServer = new BaseTCPServer(ref Self, Address, Port);

        // Start our server, as having multicast up and our TCPServer is the most important (client is used to connect to other Peer Servers) - might need to change some plans around later 
        TCPServer.StartServer();

    }



    // Peer grouping related
    [JsonIgnore] // hidden for now
    public NetworkStats NetStats { get; set; } 

    //public List<PeerTable> DiscoveredPeers { get; private set; }
    public PeerSettings Settings { get; set; } = new PeerSettings()
    {
        // Use this to update peers settings on init
    };

    // Need to setup a system where I can find the - I didnt finish making this comment months ago LOL (probably the GUID of a peer?)
    //public void SendToPeer(Guid PeerId, byte[] DataToShare, PacketAction ActionType) 
    //{
    //    PacketHeader premadeHeader = PacketHeader.GetTraversalHeader(PeerId, TCPServer.Address.ToString(), (ushort)TCPServer.Port, Guid.Empty);

    //    //SendPacketWithHeader(DataToShare, premadeHeader, true);
    //}


    public void Gossip(string Payload, PacketType Type, PacketAction Action) => Gossip(Payload.ToUTF8Byte(), Type, Action);
    public void Gossip(byte[] Payload, PacketType Type, PacketAction Action)
    {
        // SENT LIKE A BROADCAST BUT FOR GOSSIP

        Parallel.ForEach(ConnectedPeers, x =>
        {
            x.Client.Packer.SendPacket(Payload, Type, Action, PacketEncoding.NONE, PacketEncryption.ChaCha20Poly1305, PacketRoute.Gossip, null); // this one works for broadcast
            //int bytesSent = x.PacketHelper.SendPacket(Payload, Type, Action, PacketEncoding.NONE, PacketEncryption.NONE, PacketRoute.Broadcast, null);
            //x.GetPacketHelper().SendPacket(Payload, Type, Action, PacketEncoding.NONE, PacketEncryption.ChaCha20Poly1305, PacketRoute.Gossip, null);
        });
    }

    public void GossipForward(byte[] Packet, PacketHeader Header, int Fanout, Guid LastPeer)
    {
        var GossipTarget = ConnectedPeers.Where(p => p.PeerId != Header.OriginPeerId && p.PeerId != LastPeer).Take(Fanout);

        Parallel.ForEach(GossipTarget, x =>
        {
            //x.Client.Packer.SendPacket(Packet, Header.Type, Header.Action, PacketEncoding.NONE, PacketEncryption.NONE, PacketRoute.Gossip, null);
            x.Client.Packer.GossipForward(Packet);
        });
    }


    public void Broadcast(string Payload, PacketType Type, PacketAction Action) => Broadcast(Payload.ToUTF8Byte(), Type, Action);
    public void Broadcast(byte[] Payload, PacketType Type, PacketAction Action)
    {

        // [8-8-26] commented out old version 1 stuff
        //// this function needs corrected, when sent it sends as -1 bytes

        //// Im going to just put the senderip and port to the tcp server and assume that that is right
        //PacketHeader premadeHeader = PacketHeader.GetTraversalHeader(PeerId, TCPServer.Address.ToString(), (ushort)TCPServer.Port, Guid.Empty);
        //premadeHeader.PacketAction = ActionType;

        //// having issue with send while using SendPacketWithHeader - basically custom header packet probably sends it wrong
        //Parallel.ForEach(TCPServer.Clients, x =>
        //{
        //    //int bytesSent = x.PacketHelper.SendPacket(DataToShare); // this works properly but SendPacketWithHeader doesnt
        //    int bytesSent = x.PacketHelper.SendPacketWithHeader(DataToShare, premadeHeader, true);

        //    //TCPServer.InvokeDebugMessage($"PacketHeader: {premadeHeader.ToJSON()}\n\nbytesSent: {bytesSent}");
        //});
        ////Parallel.ForEach(TCPServer.Clients, x => x.PacketHelper.SendPacketWithHeader(DataToShare, premadeHeader, true));



        Parallel.ForEach(ConnectedPeers, x =>
        {
            x.Client.Packer.SendPacket(Payload, Type, Action, PacketEncoding.NONE, PacketEncryption.NONE, PacketRoute.Broadcast, null);
            //int bytesSent = x.PacketHelper.SendPacket(Payload, Type, Action, PacketEncoding.NONE, PacketEncryption.NONE, PacketRoute.Broadcast, null);
            //x.GetPacketHelper().SendPacket("testing".ToUTF8Byte(), PacketType.Data, PacketAction.NONE, PacketEncoding.NONE, PacketEncryption.ChaCha20Poly1305, PacketRoute.Broadcast, null);
        }); 

    }


    public void AddPeer(ServerClientHandle ClientHandle, PeerTable initPeer)
    {
        Console.WriteLine($"AddPeer: {initPeer.ToJSON()}"); 

        // Send known peers to new client
        //ClientHandle.PacketHelper.SendPacket(ConnectedPeers.ToArray().ToJSON().ToUTF8Byte(), PacketAction.PeerJoin); - 

        // Link ServerClientHandle data to PeerId
        ClientHandle.UpdateClientId(initPeer.PeerId); // do something around here to link Self.Peers to the Server Client Connections
        //this.TCPServer.pack

        var Helper = ClientHandle.PacketHelper;
        PeerTable newPeer = new PeerTable(ref Helper, initPeer.PeerId, initPeer.Address, initPeer.Port);
        newPeer.PacketHelper = ClientHandle.PacketHelper;

        int ConnectionLimit = Settings.MaxConnectionPerPeer;

        // if connected peers under 2 - connect otherwise just add to peer list and broadcast it
        // [seems to be an area where we limit the amount of clients connected to one server to split the load] - value need dynmic update support later (same with in multicast.cs)
        if (ConnectedPeers.Count() <= ConnectionLimit) // updated to 10 just because,this limit doesnt really matter while testing (its gonna be used later in the settings) - [the other AddPeers needs it too]
        {
            ConnectedPeers.Add(newPeer);
            TCPServer.Clients.Add(ClientHandle);

            // Send this peer my full peer list - look into just sending my peertable - this seems broken why am I sending the new peer to newpeer?
            //newPeer.GetPacketHelper().SendUTF8Packet(TCPServer.MyPeerTable.DiscoveredPeers.ToJSON(), PacketAction.PeerJoin);

            // Add peers to my peertable
            //TCPServer.MyPeerTable.DiscoveredPeers.Add(newPeer);

            // Seeing if this makes it easier - peer broadcast was missing from here
            TCPServer.InvokeOnPeerConnected(ClientHandle, initPeer); // I dont think I really have anything registered for this
        }

        // Add peer to my discovered peers
        TCPServer.MyPeerTable.DiscoveredPeers.Add(newPeer);

        // Send everyone my updated peerlist - including the new peer, this is where it discovers everyone else
        //Broadcast(TCPServer.MyPeerTable.DiscoveredPeers.ToJSON(), PacketAction.PeerJoin); 
        Broadcast(TCPServer.MyPeerTable.DiscoveredPeers.ToJSON(), PacketType.Peer, PacketAction.Join); // [8-17-26] new and untested 
    }

    private readonly HashSet<Guid> _pendingConnections = new HashSet<Guid>();
    public void AddPeers(ServerClientHandle ClientHandle, IEnumerable<PeerTable> initPeers, bool UseOriginalVersion = false) // This was set to false, but I didnt finish setting up what it was gonna be used for
    {
        Console.WriteLine($"AddPeers: {initPeers.ToJSON()} - intro");

        // Make sure our peer list is unique, and make sure the new peer list is unique
        ConnectedPeers = ConnectedPeers.Distinct().ToList();
        initPeers = initPeers.Distinct().ToList(); 

        // Send known peers to new client
        //ClientHandle.PacketHelper.SendPacket(ConnectedPeers.ToArray().ToJSON().ToUTF8Byte(), PacketAction.PeerJoin); 

        if (this.IsUniquePeers(initPeers.ToList(), out IEnumerable<PeerTable> newPeers))
        {
            var SelfPeer = this;

            List<PeerTable> UniquePeers = new List<PeerTable>();

            //// VERSION 3 OF AddPeers
            int ConnectionLimit = Settings.MaxConnectionPerPeer;
            //Console.WriteLine($"[Peer.cs] - AddPeers() [V3]");

            //// MAKE TWO LOCAL LISTS THAT WE WILL MANAGE WITHIN HERE
            //// MERGE INTO MAIN LIST WITH ONE LOCK AT THE END
            //List<PeerTable> LocalPeers = new List<PeerTable>();
            //List<ServerClientHandle> LocalClients = new List<ServerClientHandle>();
            //IEnumerable<PeerTable> Peers;

            //int Combined = ConnectedPeers.Count() + newPeers.Count();
            //if (Combined <= ConnectionLimit) Peers = newPeers;
            //else
            //{
            //    // GET AMOUNT OF PEERS THAT WE CAN ADD BASED ON LIMIT
            //    int Overflow = Combined - ConnectionLimit;

            //    int Maximum = newPeers.Count() - Overflow;
            //    Console.WriteLine($"Maxium Peers toadd: {Maximum}");
            //    // TAKE ONLY THAT AMOUNT AND ADD THEM
            //    Peers = newPeers.Take(Maximum);

            //    int LeftOver = newPeers.Count() - Maximum;
            //    // PUT THE REST IN THE DISCOVERED
            //    Console.WriteLine($"PeersLeftOver: {LeftOver}");
            //}

            //if(Peers is not null && Peers.Count() > 0)
            //{
            //    Console.WriteLine($"Peers to add -> {Peers.Count()}");
            //    // ABLE TO ADD ALL NEW PEERS
            //    Parallel.ForEach(newPeers, x =>
            //    {

            //    });
            //}





            if (UseOriginalVersion)
            {
                foreach (var peer in newPeers.Where(x => x.PeerId != SelfPeer.PeerId))
                {
                    Console.WriteLine($"[foreach] AddPeer: {peer.ToJSON()}");
                    BaseTCPClient connection = new BaseTCPClient(ref SelfPeer);
                    peer.Client = connection;
                    if (connection.TryConnect(peer.Address, peer.Port))
                    {
                        // Add them to unique list
                        UniquePeers.Add(peer);

                        peer.PacketHelper = ClientHandle.PacketHelper;

                        // Add them to current peer list
                        ConnectedPeers.Add(peer);
                        TCPServer.Clients.Add(ClientHandle);

                        //Console.WriteLine("found new peer");

                        // Seeing if this makes it easier
                        TCPServer.InvokeOnPeerConnected(ClientHandle, peer);
                    }
                }


            }
            else
            {
                Console.WriteLine($"[Peer.cs] - AddPeers() - Not using original");
                // Discovery / Max connection system based on (uptime / ping / regions)

                foreach (var peer in newPeers.Where(x => x.PeerId != SelfPeer.PeerId))
                {
                    if (ConnectedPeers.Count() <= ConnectionLimit)
                    {
                        Console.WriteLine($"[foreach] AddPeer: {peer.ToJSON()}");
                        BaseTCPClient connection = new BaseTCPClient(ref SelfPeer);
                        peer.Client = connection;

                        if (connection.TryConnect(peer.Address, peer.Port))
                        {
                            // Add them to unique list
                            UniquePeers.Add(peer);

                            peer.PacketHelper = ClientHandle.PacketHelper;

                            // Add them to current peer list
                            ConnectedPeers.Add(peer);
                            TCPServer.Clients.Add(ClientHandle);

                            //Console.WriteLine("found new peer");

                            // Seeing if this makes it easier
                            TCPServer.InvokeOnPeerConnected(ClientHandle, peer);
                        }
                    }

                    // Add peer to peer collection
                    TCPServer.MyPeerTable.DiscoveredPeers.Add(peer);
                }
            }

            // Update all peers on the new connections (im being lazy and not making sure that new peers are excluded, it'll help with discovery for now)
            // I dont know what P2PInt was meant to do, it was close to starting up the peer for the first time but maybe I moved it into PeerJoined
            //ConnectedPeers.ForEach(peer => peer.Client.Packer.SendPacket(UniquePeers.ToArray().ToJSON().ToUTF8Byte(), PacketAction.PeerJoin));
            //Broadcast(UniquePeers.ToArray().ToJSON().ToUTF8Byte(), PacketAction.PeerJoin); - broadcast all instead of just the unique ones 
            //Broadcast(TCPServer.MyPeerTable.DiscoveredPeers.ToJSON(), PacketAction.PeerJoin);
            Broadcast(TCPServer.MyPeerTable.DiscoveredPeers.ToJSON(), PacketType.Peer, PacketAction.Join); // [8-17-26] new and untested 
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