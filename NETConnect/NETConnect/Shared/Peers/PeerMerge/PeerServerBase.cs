using NETConnect.MyExtensions;
using NETConnect.Shared.Packet.Headers;
using NETConnect.Shared.Packet.Transport;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using static NETConnect.Shared.Peers.PeerMerge.PeerClientBase;

namespace NETConnect.Shared.Peers.PeerMerge;

public class PeerServerBase
{
    public Peer Self { get; set; }
    public ISocketTransport Tcp { get; set; }
    public ISocketTransport Udp { get; set; }
    public Dictionary<Guid, PeerServerClient> ConnectedClients = new Dictionary<Guid, PeerServerClient>();

    public event Action<PeerServerClient>? OnSecureChannelEstablished;

    public PeerServerBase(ref Peer self)
    {
        this.Self = self; 
        if (OnSecureChannelEstablished is null) OnSecureChannelEstablished += OnSecureConnection;

        Tcp = new TCPTransport(ref self);
        Udp = new UDPTransport(ref self);
    }

    public async Task SendAsync(Guid PeerId, bool Reliable, CancellationToken ct, byte[]? Payload = default, PacketType Type = PacketType.Data, PacketAction Action = PacketAction.NONE, PacketRoute? Route = default, Guid? RecipientPeerId = default)
    {
        Console.WriteLine($"[SERVER] [CLIENT] [SEND]");
        PeerServerClient Client = ConnectedClients[PeerId];

        //Client.

        // THIS FUNCTION HOOKS DIRECTLY INTO THE SERVER AND WE DONT WANT THAT
        //await (Reliable ? Tcp : Udp).SendAsync(ct, Payload, Type, Action, Route, RecipientPeerId);
    }
    


    public async Task StartServerAsync(CancellationToken ServerToken, int Backlog = 0)
    {
        var _socket = Tcp.GetSocket();

        IPAddress Address = Network.NetworkUtils.GetLocalLanIp();
        if (!_socket.TryBindToAvailablePort(out IPEndPoint local))
        {
            Debug.WriteLine("Failed to bind to ports using port range scanner");
            return;
        }

        var UDPServer = Udp.GetSocket();
        UDPServer.Bind(local);
        if (Backlog > 0) _socket.Listen(Backlog);
        else _socket.Listen();

        var LAN = Self.LAN;
        _ = LAN.ReadMulticastAsync();

        _ = Task.Run(async () =>
        {

            // ADVERTISE SERVER ON LAN AFTER ITS STARTS ON TCP
            while (_socket is not null && !ServerToken.IsCancellationRequested && !Self.LAN.Token.IsCancellationRequested)
            {
                //Console.WriteLine("looping");

                await LAN.Join(Address, local.Port); // send as an event not just constantly*
                await Task.Delay(new Random().Next(5000, 10000));
            }
        });

        _ = Task.Run(async () =>
        {
            _ = Task.Run(async () => Self.Server.Udp.StartReceiveLoopAsync(ServerToken));

            while (UDPServer is not null && !ServerToken.IsCancellationRequested)
            {
                var Packet = await Self.Server.Udp.Incoming.ReadAsync(ServerToken);
                if (Packet is null) continue;

                Console.WriteLine($"[SERVER] [UDP] -> {Packet.ToIndentedJSON()}");
            }
        });

        while (_socket is not null && !ServerToken.IsCancellationRequested)
        {
            var _Self = this;
            Socket Client = await _socket.AcceptAsync();

            
            _ = Task.Run(async () =>
            {
                await Handshake.EstablishSecureServerChannelAsync(this, Client, ServerToken);

                // AUTHENTICATE CLIENT / VERIFY PEER ID
                PeerServerClient _Client = new PeerServerClient(ref _Self, new TCPTransport(Client), ServerToken);




                //OnSecureChannelEstablished?.Invoke(_Client);
            });
        }
    }




    public async void OnSecureConnection(PeerServerClient Client)
    {
        _ = Task.Run(async () => await Client.Tcp.StartReceiveLoopAsync(Self.PeerToken.Token));

        Console.WriteLine("[SERVER] Connection secured");

        // HOLDS TCP CLIENT HERE AND LOOPS THROUGH EVERYTHING, WE ALSO NEED 

        while (Client.Tcp.IsConnected)
        {
            var Packet = await Client.Tcp.Incoming.ReadAsync();
            if (Packet is null) continue;

            Console.WriteLine($"[SERVER] [TCP] -> {Packet.ToIndentedJSON()}");
        }
    }
}
