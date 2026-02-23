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
    public UdpClient Client { get; private set; }
    public IPAddress MulticastAddress { get; set; }
    public int Port { get; private set; }   
    public IPEndPoint EndPoint { get; private set; }
    public CancellationTokenSource Token { get; private set; }

    public Guid SenderId { get; private set; } = Guid.NewGuid();


    public Multicast(string MulticastAddress = "235.69.4.20", int Port = 50420)
    {
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

    public void ReadMulticast()
    {
        // Disables loopback to prevent reading our own messages
        //Client.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastLoopback, false);

        Task.Run(() =>
        {

            var remoteEP = EndPoint;
            while (!Token.IsCancellationRequested)
            {
                byte[] received = Client.Receive(ref remoteEP);


                string receivedMsg = Encoding.UTF8.GetString(received);
                if (receivedMsg.IsValidJSON(out MulticastPacket Packet))
                {
                    Console.WriteLine($"[{Packet.SenderId}] - {Packet.Data.ToUTF8String()}");
                }

                
                
            }
        });

        Thread.Sleep(1000);
        SendUTF8Message("I joined the group!", MulticastAction.Join);

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
