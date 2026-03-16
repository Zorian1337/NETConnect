using NETConnect.Network;
using NETConnect.Peers;
using NETConnect.Shared.Packet.Headers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
        }
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
                Console.WriteLine(receivedMsg);
                if (receivedMsg.IsValidJSON(out MulticastPacket Packet) && Packet.SenderId != SenderId)
                {
                    OnMulticastMessage?.Invoke(Packet);
                    //Console.WriteLine($"[{Packet.SenderId}] - {Packet.Data.ToUTF8String()}");
                }

                // access this bool while inside the loop to disable our events (it should work but untested)
                if (Token.IsCancellationRequested) Wire(false);
            }
        });
    }

    private void HandleOnMulticastMessage(MulticastPacket Packet)
    {
        string UTF8 = String.Empty;
        switch (Packet.Action)
        {
            case MulticastAction.Join:
                // Remove any peers that exist multiple times somehow
                Self.ConnectedPeers = Self.ConnectedPeers.Distinct().ToList();


                //Console.WriteLine($"[SELF] MySenderId: {Self.PeerId}, PacketSenderId: {Packet.SenderId}");

                // Add only peers that havent been discovered yet
                if (Self.ConnectedPeers.Any(x => x.PeerId == Packet.SenderId) || Self.PeerId == Packet.SenderId)
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


                int Port = int.Parse(Addr[1]);
                if (Client.TryConnect(Addr[0], Port))
                {
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
                    //    Client.Packer.SendPacket(myPeer.ToJSON().ToUTF8Byte(), Shared.Packet.PacketActionType.P2PInt);
                    //}
                    //else 
                    //{
                    //    PeerTable myPeer = new PeerTable(SenderId, NetworkUtils.GetLocalLanIp().ToString(), Self.TCPServer.Port);
                    //    // Send new peer old peer list (we wont have any peers right now)
                    //    Client.Packer.SendPacket(Self.Peers.ToJSON().ToUTF8Byte(), Shared.Packet.PacketActionType.P2PInt);
                    //}


                    //PeerTable myPeer = new PeerTable(SenderId, NetworkUtils.GetLocalLanIp().ToString(), Self.TCPServer.Port); // THIS ORIGINALLY WASNT COMMENTED
                    //Self.TCPServer.MyPeerTable = myPeer;
                    // Send new peer old peer list (we wont have any peers right now)
                    Client.Packer.SendUTF8Packet(Self.ConnectedPeers.ToArray().ToJSON(), PacketActionType.PeerJoin);
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
