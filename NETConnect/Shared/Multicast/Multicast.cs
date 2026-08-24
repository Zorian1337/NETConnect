using NETConnect.Network;
using NETConnect.Peers;
using NETConnect.Shared.Packet;
using NETConnect.Shared.Packet.Headers;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices.Swift;
using System.Text;
using System.Threading.Tasks;

namespace NETConnect.Shared.Multicast;

public class Multicast
{
    public Peer Self { get; private set; }
    public UdpClient Client { get; private set; }
    public IPAddress MulticastAddress { get; set; }
    public int Port { get; private set; }   
    public IPEndPoint EndPoint { get; private set; }
    public CancellationTokenSource Token { get; private set; }
    public Guid SenderId { get; private set; } 


    public event Action<MulticastPacket> OnMulticastMessage;
    public event Func<MulticastPacket, Task> OnMulticastMessageAsync;
    public Multicast(ref Peer Self, string MulticastAddress = "235.69.4.20", int Port = 50420)
    {
        this.Self = Self;
        SenderId = Self.PeerId;
        Self.NetStats = new Network.Info.NetworkStats(ref Self);

        //Console.WriteLine($"[CONSTRUCT] MySenderID: {SenderId}");

        // Sets socket to allow for reuse of Address/Port
        Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        var localIP = NetworkUtils.GetLocalLanIp();
        sock.Bind(new IPEndPoint(localIP, Port));
        sock.Ttl = 1;

        Client = new UdpClient();
        Client.Client = sock;
        Token = new CancellationTokenSource();

        this.MulticastAddress = IPAddress.Parse(MulticastAddress);
        this.Port = Port;

        EndPoint = new IPEndPoint(this.MulticastAddress, Port);
        Client.JoinMulticastGroup(this.MulticastAddress, localIP);
        //Console.WriteLine($"Joined Multicast group [{this.MulticastAddress}:{Port}]");
    }



    public void Wire(bool IsWire = true)
    {
        if(IsWire)
        {
            OnMulticastMessage += HandleOnMulticastMessage;
            OnMulticastMessageAsync += HandleOnMulticastMessageAsync;
        }
    }

    public async Task ReadMulticastAsync()
    {
        //Console.WriteLine("started");
        Wire();

        _ = HandleDiscoveredPeers();

        // IMPLEMENT ONLY ONE READER PER PEER 
        // RETURN IF ALREADY ACTIVE

        // CREATE SHARED ArrayPool to lower memory allocation
        // THIS IS STILL THE WRONG IMPLEMENTATION OF ArrayPool
        // WE SHOULDNT BE DISPOSING AFTER EVERY USE (PRETTY SURE?)
        byte[] buffer = ArrayPool<byte>.Shared.Rent(65536);
        var memoryBuffer = buffer.AsMemory();

        try
        {
            while (!Token.IsCancellationRequested)
            {
                //Console.WriteLine("token not canceled");

                var remoteEP = EndPoint;
                SocketReceiveFromResult result = await Client.Client.ReceiveFromAsync(memoryBuffer, remoteEP, Token.Token);

                //UdpReceiveResult result = await Client.Client.re
                //Console.WriteLine("data received");
                var sender = result.RemoteEndPoint;


                int readBytes = result.ReceivedBytes;
                //Console.WriteLine(readBytes);
                if (readBytes == 0) continue;

                byte[] packetData = new byte[readBytes];
                Array.Copy(buffer, packetData, readBytes);

                string receivedMsg = Encoding.UTF8.GetString(packetData);
                //Console.WriteLine(receivedMsg); // Disabled to clear my screen
                if (receivedMsg.IsValidJSON(out MulticastPacket Packet) && Packet.SenderId != SenderId)
                {
                    // Hiding this as it'll just spam that it received something, although it is useful its not for a bit
                    //Console.WriteLine($"[Multicast] recevied -> [{Packet.SenderId}] - {Packet.Data.ToUTF8String()}");
                    //OnMulticastMessage?.Invoke(Packet);
                    _ = OnMulticastMessageAsync?.Invoke(Packet);

                }

                // access this bool while inside the loop to disable our events (it should work but untested)
                if (Token.IsCancellationRequested) Wire(false);
            }
        }
        catch(Exception Ex) { Console.WriteLine(Ex.ToString()); }
        finally { ArrayPool<byte>.Shared.Return(buffer); }

    }

    public void ReadMulticast()
    {
        Wire();

        // Prevent multiple servers from running in the same app instance 

        // Disables loopback to prevent reading our own messages -ignored self based on sender id
        //Client.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastLoopback, false);

        Task.Run(() =>
        {

            var remoteEP = EndPoint;
            while (!Token.IsCancellationRequested)
            {
                byte[] received = Client.Receive(ref remoteEP);

                //Console.Clear();
                ////Console.WriteLine($"Connected Clients: {Clients.Count()}");
                //Console.WriteLine($"Connected Peer Clients: {Self..Count()}");

                //Console.WriteLine();

                //Console.WriteLine($"Peers I connected to: \n{String.Join("\n", Self.Clients.Select(x => x.EndPoint))}");

                //Console.WriteLine();

                string receivedMsg = Encoding.UTF8.GetString(received);
                //Console.WriteLine(receivedMsg); // Disabled to clear my screen
                if (receivedMsg.IsValidJSON(out MulticastPacket Packet) && Packet.SenderId != SenderId)
                {
                    // Hiding this as it'll just spam that it received something, although it is useful its not for a bit
                    Console.WriteLine($"[Multicast] recevied -> [{Packet.SenderId}] - {Packet.Data.ToUTF8String()}");
                    OnMulticastMessage?.Invoke(Packet);
                }

                // access this bool while inside the loop to disable our events (it should work but untested)
                if (Token.IsCancellationRequested) Wire(false);
            }
        });
    }

    private async Task HandleDiscoveredPeers()
    {
        //List<PeerTable> ConnectingPeers = new List<PeerTable>();

        PeriodicTimer timer = new PeriodicTimer(new TimeSpan(0, 0, 0, 0, 100));
        while (!Token.IsCancellationRequested && await timer.WaitForNextTickAsync())
        {
            // LOOK THROUGH PEER LIST EVERY 100MS TO SEE IF WE CAN CONNECT ANYONE

            // CHECK IF CONNECTED PEERS ARE FULL BASED ON OUR SETTINGS
            if(Self.ConnectedPeers.Count() == Self.Settings.MaxConnectionPerPeer)
            {
                Console.WriteLine("peer limit reached");

                // PEER CANNOT CONNECT TO ANY OTHER ONE
                continue;
            }

            if(Self.ConnectedPeers.Count() == 0 && Self.TCPServer.MyPeerTable.DiscoveredPeers.Count() > 0)
            {
                int rng = Random.Shared.Next(0, Self.TCPServer.MyPeerTable.DiscoveredPeers.Count() - 1);
                var peer = Self.TCPServer.MyPeerTable.DiscoveredPeers[rng];

                Console.WriteLine("attempting to connect from handle");

                if (await peer.GetClient(Self).TryConnectAsyncV2(peer.Address, peer.Port))
                {
                    Console.WriteLine($"[CLIENT] {peer.PeerId} has connected and authenticated!");
                    Self.ConnectedPeers.Add(peer);
                }


            }
            else
            {
                //// GET PEERS WHO ARENT ALREADY CONNECTED
                var NONConnectedPeers = Self.TCPServer.MyPeerTable.DiscoveredPeers.Where(x => !Self.ConnectedPeers.Any(a => x.PeerId == a.PeerId));

                var PeersToConnect = NONConnectedPeers.Take(Math.Min(Self.ConnectedPeers.Count(), Self.Settings.MaxConnectionPerPeer));

                await Parallel.ForEachAsync(PeersToConnect, async (peer, token) =>
                {
                    Console.WriteLine("attempting to connect from handle");
                    if (await peer.GetClient(Self).TryConnectAsyncV2(peer.Address, peer.Port))
                    {
                        Console.WriteLine($"[CLIENT] {peer.PeerId} has connected and authenticated!");
                        Self.ConnectedPeers.Add(peer);
                    }
                });
            }
        }

        Console.WriteLine("loop ended");
    }

    private async Task HandleOnMulticastMessageAsync(MulticastPacket Packet)
    {
        // WE DONT WANT ANY MESSAGES FROM OURSELF
        if (Packet.SenderId == Self.PeerId) return;

        string UTF8 = String.Empty;
        switch (Packet.Action)
        {
            case MulticastAction.Join:
                // ONCE A CLIENT JOINS THE MULTICAST ADD THEM TO THE DISCOVERED LIST
                // USE THE MULTICAST TO FIND PEERS TO JOIN OUR MESH

                UTF8 = Packet.Data.ToUTF8String();
                string[] Addr = UTF8.Split(":");
                string Address = Addr[0];
                int Port = int.Parse(Addr[1]);

                // ADD PEERS TO OUR DISCOVERY LIST IF THEY HAVENT BEEN DISCOVERED
                if (!Self.TCPServer.MyPeerTable.DiscoveredPeers.Any(x => x.PeerId == Packet.SenderId))
                {
                    PeerTable peer = new PeerTable(Packet.SenderId, Address, Port);
                    Self.TCPServer.MyPeerTable.DiscoveredPeers.Add(peer);

                    Console.WriteLine($"Added [{peer.PeerId}] to the Discovery\nPeer => {peer.ToJSON()}");

                    Console.WriteLine();
                    Console.WriteLine();
                    Console.WriteLine(Self.TCPServer.MyPeerTable.DiscoveredPeers.ToJSON());
                }
                // UPDATE PEER LIST IF THEY HAVE HAD AN UDPATE TO THEIR STATS?
                else
                {

                } 
                break;
        }
    }

    
    private void HandleOnMulticastMessage(MulticastPacket Packet)
    {
        string UTF8 = String.Empty;
        switch (Packet.Action)
        {
            case MulticastAction.Join:

                // WE NEED TO CHANGE THE FLOW OF THIS, 
                // MULTICAST IS THE ONLY WAY CLIENTS GET CONVERTED TO PEER
                // CLIENTS EVEN AFTER CONNECTING NEED TO WAIT FOR THE MULTICAST TO DISCOVER THEM SO THAT IT CAN CREATE A NEW CONNECTION JUST TO MAKE IT A PEER
                // I DONT EVEN THINK IT USES THE SAME CLIENT OBJECT TO CONVERT IT OVER
                // WE NEED TO MAKE SURE THAT IT CHECKS IF A CLIENT ISNT ALREADY A PEER AND A PEER ISNT ALREADY LISTED
                // INSTEAD OF JUST CHECKING FOR A CONNECTED PEER

                // Remove any peers that exist multiple times somehow
                Self.ConnectedPeers = Self.ConnectedPeers.Distinct().ToList();

                // Add only peers that havent been discovered yet
                // Include peers that have already been connected to the server just not added to the connected peer list yet (find a way to add them during our client flow)
                if (Self.ConnectedPeers.Any(x => x.PeerId == Packet.SenderId) || Self.PeerId == Packet.SenderId) // Removing this just for now Self.TCPServer.Clients.Any(x => x.Id == Packet.SenderId)
                {
                    //Console.WriteLine("Peer already disovered");
                    return;  
                }


                //Console.WriteLine("join packet");
                UTF8 = Packet.Data.ToUTF8String();
                string[] Addr = UTF8.Split(":");


                // Create new client add it to client list
                var SelfPeer = Self;
                BaseTCPClient Client = new BaseTCPClient(ref SelfPeer);
                // After connecting to the newest peer who joined, share your list of peers for them to join (later only 1 will need to do this)


                // PREVENT CONNECTION IF GUID IS LOWER AND IF MAX NUMBER OF PEERS
                if (Self.ConnectedPeers.Count == Self.Settings.MaxConnectionPerPeer) return;
                //if (!(Packet.SenderId.CompareTo(Self.PeerId) > 0)) return;

                int Port = int.Parse(Addr[1]);


                // ONLY TEMP TESTING
                //return; // PREVENT PEER FROM CONNECTING THIS WAY
                if (Client.TryConnectAsyncV2(Addr[0], Port).GetAwaiter().GetResult()) // PROBABLY WAIT UNTIL THIS IS AUTHENTICATED BEFORE GOING FORWARD AS IT CANT SEND THE MESSAGES IT NEEDS TO?
                {
                    //return;
                    //Console.WriteLine($"[Multicast] ClientJoinId: {Packet.SenderId}");
                    PeerTable newPeer = new PeerTable(Packet.SenderId, Addr[0], Port)
                    {
                        Client = Client,
                        //IsLocal = true
                    };

                    //if (Self.OperationMode == PeerState.Server) // This was allowing the server/client to connect to themselves
                    //{
                    //    PeerTable myPeer = new PeerTable(SenderId, NetworkUtils.GetLocalLanIp().ToString(), Self.TCPServer.Port);
                    //    // Send new peer old peer list (we wont have any peers right now)
                    //    Client.Packer.SendPacket(myPeer.ToJSON().ToUTF8Byte(), Shared.Packet.PacketAction.P2PInt);
                    //}
                    //else 
                    //{
                    //    PeerTable myPeer = new PeerTable(SenderId, NetworkUtils.GetLocalLanIp().ToString(), Self.TCPServer.Port);
                    //    // Send new peer old peer list (we wont have any peers right now)
                    //    Client.Packer.SendPacket(Self.Peers.ToJSON().ToUTF8Byte(), Shared.Packet.PacketAction.P2PInt);
                    //}


                    //PeerTable myPeer = new PeerTable(SenderId, NetworkUtils.GetLocalLanIp().ToString(), Self.TCPServer.Port); // THIS ORIGINALLY WASNT COMMENTED
                    //Self.TCPServer.MyPeerTable = myPeer;
                    // Send new peer old peer list (we wont have any peers right now)
                    Console.WriteLine("[MULTICAST] Sent SERVER join request from peer joining");

                    string JSON = Self.ConnectedPeers.ToArray().ToJSON();
                    Console.WriteLine($"Peers: {JSON}");
                    //Client.Packer.SendUTF8Packet(JSON, PacketAction.PeerJoin); //- THIS WAS ENABLED BUT I ENABLED IT TO DO SOME DEBUG, I CANT TELL IF ITS ACTUALLY ACTIVATING [probably an issue with the authentication]
                    
                    
                    Client.Packer.SendPacket(JSON.ToUTF8Byte(), PacketType.Peer, PacketAction.Join, PacketEncoding.NONE, PacketEncryption.NONE, PacketRoute.Direct, null);



                    // We simply just need to do Self.ConnectedPeers.Add in the PeerJoin area (I hope)
                    // Server controls the multicast so this is how it ends up populated for servers (at least it should be)
                    // Whatever peer reads this multicast becomes the peers server, and the server becomes the peer (super confusing I know)
                    // It seems we already have it there so I have no clue how to fix this
                    Self.ConnectedPeers.Add(newPeer);
                }
                break;
        }
    }

    public void SendMessage(byte[] Message, MulticastAction Action, IPEndPoint EPoint)
    {
        MulticastPacket packet = new MulticastPacket(SenderId, Message, Action);
        string JSON = System.Text.Json.JsonSerializer.Serialize(packet);

        byte[] Data = JSON.ToUTF8Byte();
        Client.Send(Data, Data.Length, EPoint);
    }
    public void SendUTF8Message(string UTF8Message, MulticastAction Action, IPEndPoint EPoint) => SendMessage(UTF8Message.ToUTF8Byte(), Action, EPoint);

    public void SendMessage(byte[] Message, MulticastAction Action)
    {
        //Console.WriteLine($"sent from : {SenderId}");
        MulticastPacket packet = new MulticastPacket(SenderId, Message, Action);
        string JSON = System.Text.Json.JsonSerializer.Serialize(packet);

        //Console.WriteLine(JSON);

        byte[] Data = JSON.ToUTF8Byte();
        Client.Send(Data, Data.Length, EndPoint);
    }

    public void SendUTF8Message(string UTF8Message, MulticastAction Action)
    {

        //Console.WriteLine($"Multicast Message: {UTF8Message}");
        SendMessage(UTF8Message.ToUTF8Byte(), Action);
    }
}
