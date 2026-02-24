using NETConnect.Peers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
    public Guid SenderId { get; private set; } = Guid.NewGuid();


    public event Action<MulticastPacket> OnMulticastMessage;

    public Multicast(ref Peer Self, string MulticastAddress = "235.69.4.20", int Port = 50420)
    {
        this.Self = Self;
        // Sets socket to allow for reuse of Address/Port
        Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        sock.Bind(new IPEndPoint(IPAddress.Any, Port));

        Client = new UdpClient();
        Client.Client = sock;
        Token = new CancellationTokenSource();

        this.MulticastAddress = IPAddress.Parse(MulticastAddress);
        this.Port = Port;

        EndPoint = new IPEndPoint(this.MulticastAddress, Port);
        Client.JoinMulticastGroup(this.MulticastAddress);
        Console.WriteLine($"Joined Multicast group [{this.MulticastAddress}:{Port}]");
    }

    public Multicast(ref BaseTCPClient TCPClient, string MulticastAddress = "235.69.4.20", int Port = 50420)
    {
        // Sets reference to TCPClient, so it can join the other peers server upon its join
        // Connect to Peer 1
        //TcpClient client1 = new TcpClient();
        //client1.Connect("192.168.1.10", 5000);

        //// Connect to Peer 2
        //TcpClient client2 = new TcpClient();
        //client2.Connect("192.168.1.11", 5000);

        //// Now you can read/write to each independently
        //NetworkStream stream1 = client1.GetStream();
        //NetworkStream stream2 = client2.GetStream();

        // Sets socket to allow for reuse of Address/Port
        Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        sock.Bind(new IPEndPoint(IPAddress.Any, Port));

        Client = new UdpClient();
        Client.Client = sock;
        Token = new CancellationTokenSource();


        this.MulticastAddress = IPAddress.Parse(MulticastAddress);
        this.Port = Port;

        EndPoint = new IPEndPoint(this.MulticastAddress, Port);
        Client.JoinMulticastGroup(this.MulticastAddress);
        Console.WriteLine($"Joined Multicast group [{this.MulticastAddress}:{Port}]");
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

                string receivedMsg = Encoding.UTF8.GetString(received);
                if (receivedMsg.IsValidJSON(out MulticastPacket Packet) && Packet.SenderId != SenderId)
                {
                    OnMulticastMessage?.Invoke(Packet);
                    Console.WriteLine($"[{Packet.SenderId}] - {Packet.Data.ToUTF8String()}");
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
                //Console.WriteLine("join packet");
                UTF8 = Packet.Data.ToUTF8String();
                string[] Addr = UTF8.Split(":");

                // Create new client add it to client list
                BaseTCPClient Client = new BaseTCPClient();
                // After connecting to the newest peer who joined, share your list of peers for them to join (later only 1 will need to do this)
                if (Client.TryConnect(Addr[0], int.Parse(Addr[1]))) Self.Clients.Add(Client);
                break;
        }
    }

    public void SendMessage(byte[] Message, MulticastAction Action)
    {
        MulticastPacket packet = new MulticastPacket(SenderId, Message, Action);
        string JSON = System.Text.Json.JsonSerializer.Serialize(packet);

        byte[] Data = JSON.ToUTF8Byte();
        Client.Send(Data, Data.Length, EndPoint);
    }

    public void SendUTF8Message(string UTF8Message, MulticastAction Action) => SendMessage(UTF8Message.ToUTF8Byte(), Action);
}
