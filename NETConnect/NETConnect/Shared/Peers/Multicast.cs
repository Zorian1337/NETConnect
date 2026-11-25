using NETConnect.MyExtensions;
using NETConnect.Shared.Packet.Headers;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NETConnect.Shared.Peers;

public enum MulticastAction
{
    Join, Leave, Data
}

public class MulticastPacket
{
    public int Version { get; set; }
    public Guid SenderId { get; set; }
    public byte[] Data { get; set; }
    public MulticastAction Action { get; set; }

    public MulticastPacket(int Version, Guid SenderId, byte[] data, MulticastAction action)
    {
        this.Version = Version;
        this.SenderId = SenderId;
        this.Data = data;
        this.Action = action;
    }
}

public class Multicast
{
    public Peer Self { get; set; }
    public Socket Client { get; set; }
    public IPAddress MulticastAddress { get; set; }
    public int Port { get; private set; }
    public IPEndPoint EndPoint { get; private set; }
    public CancellationTokenSource Token { get; private set; }

    //public event Action<IPAddress, int> OnLANJoin;

    public Multicast(ref Peer Self, string MulticastAddress = "235.69.4.20", int Port = 50420)
    {
        this.Self = Self;
        if(Client is null)
        {
            Client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        }

        IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, Port);
        Client.Bind(endpoint);
        Client.Ttl = 7;

        Token = new CancellationTokenSource();

        this.MulticastAddress = IPAddress.Parse(MulticastAddress);
        this.Port = Port;

        EndPoint = new IPEndPoint(this.MulticastAddress, Port);
        Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(this.MulticastAddress)); //

        Console.WriteLine($"Joined Multicast group [{this.MulticastAddress}:{Port}]");
    }

    public async Task ReadMulticastAsync()
    {
        //List<string> Addresses = new List<string>();
        List<Guid> ConnectedPeers = new List<Guid>();
        while (Client is not null && !Token.IsCancellationRequested)
        {
            using (var Packet = await Client.ReceiveUDPPacketAsync())
            {
                if (Packet is null) continue;
                PacketHeader Header = (PacketHeader)Packet.Header;

                if (Header.OriginPeerId == Self.GetPeerId()) continue;
                //Console.WriteLine($"{Header.OriginPeerId} => {Self.GetPeerId()}");


                #region replace with this eventually...
                //// ADD | REMOVE PEERS TO DISCOVERY WHEN THEY ANNOUNCE JOIN | LEAVE HERE.
                //if (Header.Action == PacketAction.Join)
                //{

                //}
                //else if (Header.Action == PacketAction.Leave)
                //{

                //}
                #endregion

                var Payload = Packet.GetPayloadSpan();
                //if (Header.Type == PacketType.Data)
                //{
                //    string Information = Payload.ToUTF8String();
                //    if (Information.IsNullWhiteSpaceOrEmpty()) continue;
                //    Console.WriteLine($"UDP Data -> {Information}");
                //}


                if (Payload.IsValidJSON<MulticastPacket>(out var mPacket))
                {
                    //Console.WriteLine($"\nMulticast -> \n{mPacket.ToIndentedJSON()}\n");

                    if (mPacket.Action == MulticastAction.Join)
                    {
                        if (ConnectedPeers.Any(x => x == Header.OriginPeerId)) continue;


                        // CHECK FOR VALID DATA, IF ITS NOT THEN SKIP
                        string Information = mPacket.Data.ToUTF8String();
                        if (Information.IsNullWhiteSpaceOrEmpty()) continue;

                        string[] info = Information.Split(",");

                        if (IPAddress.TryParse(info[0], out IPAddress? Address) && int.TryParse(info[1], out int Port))
                        {
                            Console.WriteLine($"Multicast:Join -> {mPacket.ToIndentedJSON()} - {info[0]}:{info[1]}");

                            ConnectedPeers.Add(Header.OriginPeerId);

                            _ = Task.Run(async () => await Self.Client.ConnectAsync(Address, Port));
                        }
                    }
                }


                //Console.WriteLine($"Multicast -> {BitConverter.ToString(Packet.GetFullPacketCopy())}");
            }
        }
    }


    public int Message(string data) => Message(Encoding.UTF8.GetBytes(data));
    public int Message(byte[] Payload)
    {
        PacketHeader Header = new PacketHeader(PacketType.Data, PacketAction.NONE);
        return Client.SendTo(Header.ToBinaryHeader(Payload), EndPoint); //Payload.Length, SocketFlags.None
    }

    public async ValueTask<int> Join(IPAddress Address, int Port)
    {

        int bytesSent = -1;
        try
        {
            PacketHeader Header = new PacketHeader(PacketType.Data, PacketAction.NONE);
            Header.OriginPeerId = Self.GetPeerId();

            //Console.WriteLine(Self.GetPeerId());

            byte[] data = Encoding.UTF8.GetBytes($"{Address.MapToIPv6()},{Port}");
            MulticastPacket MP = new MulticastPacket(2, Header.OriginPeerId, data, MulticastAction.Join);

            string JSON = System.Text.Json.JsonSerializer.Serialize(MP);
            byte[] Payload = Encoding.UTF8.GetBytes(JSON);

            //bytesSent = Client.SendTo(Header.ToBinaryHeader(Payload), EndPoint); ;
            bytesSent = await Client.SendUDPPacketAsync(EndPoint, Header, Payload);
            //Client.SendUDPPacketAsync()
            //Console.WriteLine($"4 -> {bytesSent}");
        }
        catch (Exception Ex) { Console.WriteLine(Ex.ToString()); }


        return bytesSent;
    }

    //public void SendMessage(byte[] Message, MulticastAction Action)
    //{
    //    //Console.WriteLine($"sent from : {SenderId}");
    //    MulticastPacket packet = new MulticastPacket(Self.Version, SenderId, Message, Action);
    //    string JSON = System.Text.Json.JsonSerializer.Serialize(packet);

    //    //Console.WriteLine(JSON);

    //    byte[] Data = JSON.ToUTF8Byte();
    //    Client.Send(Data, Data.Length, EndPoint);
    //}

    //public void SendMessage(byte[] Message, MulticastAction Action, IPEndPoint EPoint)
    //{
    //    MulticastPacket packet = new MulticastPacket(Self.Version, SenderId, Message, Action);
    //    string JSON = System.Text.Json.JsonSerializer.Serialize(packet);

    //    byte[] Data = JSON.ToUTF8Byte();
    //    Client.Send(Data, Data.Length, EPoint);
    //}
}
