using NETConnect.CustomConsole;
using NETConnect.Interfaces;
using NETConnect.MyExtensions;
using NETConnect.MyExtensions.Encryption;
using NETConnect.Network;
using NETConnect.Peers;
using NETConnect.Shared;
using NETConnect.Shared.Multicast;
using NETConnect.Shared.Packet;
using NETConnect.Shared.Packet.Headers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace NETConnect;

public enum ServerState
{
    Stopped,
    Starting, 
    Running, 
    Stopping
}

// Each instance of this class should be able to create a new server running on a different port.
public class BaseTCPServer : BaseServerProperties
{
    public Peer Self { get; set; }
    public IPAddress Address { get; set; }
    public Socket SocketServer { get; set; }
    public string ServerAddress { get; set; }
    public ServerState State { get; set; }
    public int Port { get; set; }
    public PeerTable MyPeerTable { get; set; } 
    public CancellationTokenSource ServerToken { get; set; }


    /// <summary>
    /// Event for the first connect between the server and a client
    /// At this point the client isnt a peer, nor has it setup any encryption
    /// </summary>
    public event Action<Socket> OnClientConnected;
    public event Func<Socket, Task>? OnClientConnectedAsync;

    public event Action<Socket> OnClientDisconnected;


    public event Action<Socket, PacketHelper> OnAuthenticationRequested;
    public event Func<ServerClientHandle, ReceivedPacket<IPacketHeaderIdentifier>, Task>? OnAuthenticationRequestedAsync;
    public event Action<ServerClientHandle, PacketHeader, ReadOnlySpan<byte>> OnDataReceived;
    public event Action<PacketHelper, ReceivedPacket<IPacketHeaderIdentifier>> OnPacketReceived;

    // Peer related - this can probably hold the clienthandle and the peer side 
    public event Action<ServerClientHandle, PeerTable> OnPeerConnected;

    public event Action<string> OnDebugMessage;

    /// <summary>
    /// We only want to use this for the clients themselves, if we want anything Peer related use Connected Clients
    /// </summary>
    public List<ServerClientHandle> Clients { get; set; } = new List<ServerClientHandle>();

    public BaseTCPServer(IPAddress Address, int Port)
    {
        this.Address = Address;
        this.Port = Port;
    }

    // Add support for multicast hooking
    public BaseTCPServer(ref Peer Self, IPAddress Address, int Port)
    {
        this.Self = Self;
        this.Address = Address;
        this.Port = Port;
    }

    public void InvokeDebugMessage(string Message) => OnDebugMessage?.Invoke(Message);

    public async Task StartServerAsync()
    {
        Console.WriteLine("started async");
        if (SocketServer is not null) return;

        if(SocketServer is null)
        {
            // Creates instance of socket server if not existing
            SocketServer = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
            
            if (SocketServer.AddressFamily == AddressFamily.InterNetworkV6)
            {
                // Allows IPv4 and IPv6 to connect - supports multiplatform (DualMode doesnt)
                SocketServer.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, 0);
                Console.WriteLine("allows all network adapaters");
            }

            // Wire up our base events
            //OnClientConnected += HandleClientConnected;
            OnClientConnectedAsync += HandleClientConnectedAsync;
            OnAuthenticationRequestedAsync += HandleAuthenticationAsync;

            OnDataReceived += HandleDataReceived;

            ServerToken = new CancellationTokenSource();

            State = ServerState.Starting;
        }

        // Verify Address and Port arent null 

        SocketServer.Bind(new IPEndPoint(Address, Port));
        SocketServer.Listen();
        Console.WriteLine("listening");
        State = ServerState.Running;

        // Grabbed this just to assign it directly to the server class
        int assignedPort = ((IPEndPoint)SocketServer.LocalEndPoint).Port;
        Port = assignedPort;

        //Console.WriteLine($"Server listening at {SocketServer.LocalEndPoint.ToString()}\n");

        if (Self is not null)
        {
            //OnDebugMessage?.Invoke("Self is not null");

            // Transmit server IP and Port to connect to it over local net
            IPEndPoint Point = ((IPEndPoint)SocketServer.LocalEndPoint);//.ToString();
                                                                        //OnDebugMessage?.Invoke("Self is not null");
                                                                        // This is where the server can join the multicast
            var SelfPeer = Self;

            // Create Peer Table here - Might not update the peerId with it as its set within multiicast but will need change anyway later, as thats not a good way to set it
            MyPeerTable = new PeerTable(ref SelfPeer, NetworkUtils.GetLocalLanIp().ToString(), Point.Port); // this might need fixed, port here and port in multicast are conflicting
                                                                                                            //(idr if I want this to list the server connection) im pretty sure I do, as I need to have clients connect to the server (so having the server informationa as the peer makes sense)

            //OnDebugMessage?.Invoke("Self is not null");
            Self.Multicast = new Multicast(ref SelfPeer);
            //Self.Multicast.ReadMulticast(); // blocks
            _ = Self.Multicast.ReadMulticastAsync();

            ServerAddress = $"{NetworkUtils.GetLocalLanIp()}:{Point.Port}";

            // This breaks when we are using it in something other than a console
            //if (Environment.UserInteractive && Console.Title != null) Console.Title = $"Server: [{ServerAddress}]";


            //if (Self.TCPServer.ServerToken is null) OnDebugMessage?.Invoke($"ServerToken is null");
            //else OnDebugMessage?.Invoke($"ServerToken is not null");

            // STARTS SENDING MULTICAST ANNOUNCEMENTS ASYNCHRONOUSLY 
            _ = SendMulticastAnnouncement();
        }

        _ = AcceptPeerConnectionAsync();
    }

    public void StartServer()
    {
        // Prevent multiple servers from running in the same app instance 

        // CHANGE THIS TO START ASYNC LATER
        Task.Run(async () =>
        {
            // reinit later when trying to test audio module 
            //NETConnect.Audio.Audio.Init();

            try
            {
                //Console.WriteLine("Starting TCP Server!\n");

                // Init stuff if it doesnt already exist

                if (SocketServer is null)
                {
                    // Creates instance of socket server if not existing
                    SocketServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                    // Wire up our base events
                    OnClientConnected += HandleClientConnected;

                    OnAuthenticationRequested += HandleAuthentication;

                    OnDataReceived += HandleDataReceived;

                    ServerToken = new CancellationTokenSource();
                }

                if (SocketServer.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    // Allows IPv4 and IPv6 to connect - supports multiplatform (DualMode doesnt)
                    SocketServer.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, 0);
                }

                // Verify Address and Port arent null 
                SocketServer.Bind(new IPEndPoint(Address, Port));
                SocketServer.Listen();

                // Grabbed this just to assign it directly to the server class
                int assignedPort = ((IPEndPoint)SocketServer.LocalEndPoint).Port;
                Port = assignedPort;

                //Console.WriteLine($"Server listening at {SocketServer.LocalEndPoint.ToString()}\n");

                // Check if multicast is valid then send announcement
                if(Self is not null)
                {
                    //OnDebugMessage?.Invoke("Self is not null");

                    // Transmit server IP and Port to connect to it over local net
                    IPEndPoint Point = ((IPEndPoint)SocketServer.LocalEndPoint);//.ToString();
                    //OnDebugMessage?.Invoke("Self is not null");
                    // This is where the server can join the multicast
                    var SelfPeer = Self;
                    //OnDebugMessage?.Invoke("Self is not null");
                    Self.Multicast = new Multicast(ref SelfPeer);
                    //Self.Multicast.ReadMulticast(); // blocks
                    _ = Self.Multicast.ReadMulticastAsync(); // ALLOWS THIS TO CONTINUE TO RUN IN BACKGROUND
                    //Task.Run(async () => await Self.Multicast.ReadMulticastAsync());


                    ServerAddress = $"{NetworkUtils.GetLocalLanIp()}:{Point.Port}";
                    
                    // This breaks when we are using it in something other than a console
                    //if (Environment.UserInteractive && Console.Title != null) Console.Title = $"Server: [{ServerAddress}]";

                    // Create Peer Table here - Might not update the peerId with it as its set within multiicast but will need change anyway later, as thats not a good way to set it
                    MyPeerTable = new PeerTable(ref SelfPeer, NetworkUtils.GetLocalLanIp().ToString(), Point.Port); // this might need fixed, port here and port in multicast are conflicting
                    //(idr if I want this to list the server connection) im pretty sure I do, as I need to have clients connect to the server (so having the server informationa as the peer makes sense)


                    //if (Self.TCPServer.ServerToken is null) OnDebugMessage?.Invoke($"ServerToken is null");
                    //else OnDebugMessage?.Invoke($"ServerToken is not null");

                    // Handles advertising the server to the multicast in another thread (so it doesnt block the main thread)
                    await Task.Run(async () =>
                    {
                        // [8-17-26] CREATE ADAPTIVE ANNOUNCEMENTS TO LIMIT NETWORK CONGESTION AND STILLBE ABLE TO FIND PEERS FAST OVER LAN

                        while (!Self.TCPServer.ServerToken.IsCancellationRequested)
                        {
                            //OnDebugMessage?.Invoke($"Advertising to the multicast as {ServerAddress}");
                            Self.Multicast.SendUTF8Message(ServerAddress, MulticastAction.Join);

                            int PeerCount = Self.ConnectedPeers.Count();
                            // ANNOUNCE OURSELF ON THE MULTICAST WHEN WE HAVE ROOM FOR MORE CONNECTIONS
                            if (PeerCount < Self.Settings.MaxConnectionPerPeer)
                            {
                                if(PeerCount == 0) await Task.Delay(500);
                                else if (PeerCount <= 2) await Task.Delay(1000);
                                else if (PeerCount <= 5) await Task.Delay(3000);
                            }
                            else await Task.Delay(10 * 1000);
                        }
                    });

                   //Using this for informational purposes related to peers

                   //Task.Run(async () => await UpdatePeerDisplay());
                }

                // Handles searching for clients in another thread...
                await Task.Run(() =>
                {
                    // Searches for clients until the token is set to get ready to cancel
                    while (!ServerToken.IsCancellationRequested)
                    {
                        //OnDebugMessage?.Invoke($"ServerToken is not null or cancel requested");
                        //Console.WriteLine("waiting on new clients");
                        //Console.WriteLine("Waiting on new client connections...\n");
                        Socket Client = SocketServer.Accept();

                        //Console.WriteLine("new client to connect");
                        // Find client then immediately handle it elsewhere for performance
                        OnClientConnected?.Invoke(Client);
                    }
                });

            }
            catch (Exception ex) { Console.WriteLine(ex.ToString()); }
        });
    }

    public async Task SendMulticastAnnouncement()
    {
        // [8-17-26] CREATE ADAPTIVE ANNOUNCEMENTS TO LIMIT NETWORK CONGESTION AND STILLBE ABLE TO FIND PEERS FAST OVER LAN

        while (!Self.TCPServer.ServerToken.IsCancellationRequested)
        {
            //OnDebugMessage?.Invoke($"Advertising to the multicast as {ServerAddress}");
            Self.Multicast.SendUTF8Message(ServerAddress, MulticastAction.Join);

            int PeerCount = Self.ConnectedPeers.Count();
            // ANNOUNCE OURSELF ON THE MULTICAST WHEN WE HAVE ROOM FOR MORE CONNECTIONS
            if (PeerCount < Self.Settings.MaxConnectionPerPeer)
            {
                if (PeerCount == 0) await Task.Delay(500, Self.TCPServer.ServerToken.Token);
                else if (PeerCount <= 2) await Task.Delay(1000, Self.TCPServer.ServerToken.Token);
                else if (PeerCount <= 5) await Task.Delay(3000, Self.TCPServer.ServerToken.Token);
            }
            else await Task.Delay(10 * 1000, Self.TCPServer.ServerToken.Token);
        }
    }

    public async Task AcceptPeerConnectionAsync()
    {
        // Searches for clients until the token is set to get ready to cancel
        while (!ServerToken.IsCancellationRequested)
        {
            //OnDebugMessage?.Invoke($"ServerToken is not null or cancel requested");
            //Console.WriteLine("waiting on new clients");
            //Console.WriteLine("Waiting on new client connections...\n");
            Socket Client = await SocketServer.AcceptAsync(ServerToken.Token);

            if(Client is not null) _ = OnClientConnectedAsync?.Invoke(Client);

            //Console.WriteLine("new client to connect");
            // Find client then immediately handle it elsewhere for performance
            //OnClientConnected?.Invoke(Client);
        }
    }

    public async Task UpdatePeerDisplay()
    {

        string LastOut = "";
        string NewOut = "";

        PeriodicTimer timer = new PeriodicTimer(new TimeSpan(0, 0, 30));
        while (!ServerToken.IsCancellationRequested && await timer.WaitForNextTickAsync())
        {
            //Thread.Sleep(5000);

            //Clear console for a clean display while doing peer mapping
            //Console.Clear(); 

            //Console.WriteLine($"PeerId: {Self.PeerId} - OperationMode: {Self.OperationMode}\n");
            NewOut += $"PeerId: {Self.PeerId} - OperationMode: {Self.OperationMode}\n";

            if (Self.OperationMode == PeerState.Peer && Self.ConnectedPeers.Count() > 0)
            {
                // This should be implemented into it by default (add it if it isnt already)
                // Check if there are any disconnected clients and remove them from our peer list
                if(Self.ConnectedPeers.Any(x => x.Client.Token.IsCancellationRequested))
                {
                    List<PeerTable> expiredPeers = Self.ConnectedPeers.FindAll(x => x.Client.Token.IsCancellationRequested);

                    Self.ConnectedPeers.RemoveAll(x => x.Client.Token.IsCancellationRequested);
                    Self.TCPServer.Clients.RemoveAll(x => x.ClientToken.IsCancellationRequested);

                    Console.WriteLine("Some peers have expired tokens!");
                    //NewOut += "Some peers have expired tokens!\n";
                }

                Console.WriteLine();
                string Server = $"TCPServer: \n" +
                    $"Address: {Self.TCPServer.ServerAddress}\n" +
                    $"Peers: {Self.ConnectedPeers.Count()}\n" +
                    $"{Self.ConnectedPeers.ToJSON(new System.Text.Json.JsonSerializerOptions() { WriteIndented = true})}";
                Console.WriteLine(Server);
                //Console.WriteLine(Self.ToJSON(new System.Text.Json.JsonSerializerOptions() { WriteIndented = true}));
                //Console.WriteLine($"Connected Peers: {Self.ConnectedPeers.Count()}\n");
                //NewOut += $"Connected Peers: {Self.ConnectedPeers.Count()}\n";

                //Console.WriteLine($"Peers: \n{String.Join("\n", Self.ConnectedPeers.Select(x => $"{x.PeerId} [{x.Address}:{x.Port}]"))}\n");
                //NewOut += $"Peers: \n{String.Join("\n", Self.ConnectedPeers.Select(x => $"{x.PeerId} [{x.Address}:{x.Port}]"))}\n";

                //Console.WriteLine($"MyStats: {Self.NetStats.ToJSON(new System.Text.Json.JsonSerializerOptions() { WriteIndented = true})}");
                //NewOut += $"MyStats: {Self.NetStats.ToJSON(new System.Text.Json.JsonSerializerOptions() { WriteIndented = true})}\n";

                //Console.WriteLine($"MyTable: \n{Self.TCPServer.MyPeerTable.ToJSON(new System.Text.Json.JsonSerializerOptions() { WriteIndented = true })}");
                //NewOut += $"MyTable: \n{Self.TCPServer.MyPeerTable.ToJSON(new System.Text.Json.JsonSerializerOptions() { WriteIndented = true })}\n";
            }
            else if (Self.OperationMode == PeerState.Server && Self.TCPServer.Clients.Count() > 0)
            {
                Console.WriteLine($"Connected Clients: {Self.TCPServer.Clients.Count()}\n");
                //NewOut += $"Connected Clients: {Self.TCPServer.Clients.Count()}\n";

                Console.WriteLine($"Clients: \n{String.Join("\n", Self.TCPServer.Clients.Select(x => $"[{x.Id}]"))}");
                //NewOut += $"Clients: \n{String.Join("\n", Self.TCPServer.Clients.Select(x => $"[{x.Id}]"))}\n";
            }

            //if(NewOut != LastOut) // this doesnt work
            //{
            //    //Console.Clear();
            //    Console.WriteLine(NewOut);
            //    LastOut = NewOut;
            //    NewOut = "";
            //}
        }
    }


    public async Task ReadMessageAsync(ServerClientHandle ClientHandle) //Socket Client, PacketHelper Packer, CancellationTokenSource ClientToken
    {
        var Client = ClientHandle.Connection;
        var ClientToken = ClientHandle.ClientToken;
        var Packer = ClientHandle.PacketHelper;
        while (!ClientToken.IsCancellationRequested && Client.Connected && !Client.IsGracefulShutdown())
        {
            //Console.WriteLine("[SERVER] -> looping receive messages");

            using (var received = await Client.ReceiveFullPacketAsync(Packer))
            {
                //Console.WriteLine("received packet");
                if (received is null) break; //Console.WriteLine("null packet"); 

                // RECORD ANY PACKET BEING RECEIVED
                OnPacketReceived?.Invoke(Packer, received);

                Span<byte> Packet = received.GetPayloadSpan();
                PacketHeader Header = (PacketHeader)received.Header;


                //Console.WriteLine($"HeaderInfo: {Header.ToJSON(new System.Text.Json.JsonSerializerOptions() { WriteIndented = true})}");
                if (Header.Type != PacketType.NONE)
                {
                    //Console.WriteLine($"C++ data received ->\nSize: {Packet.Length} - DATA: {string.Join(" ", Packet.Select(x => x.ToString("X2")))}");
                    //Console.WriteLine($"HeaderInfo: {Header.ToJSON(new System.Text.Json.JsonSerializerOptions() { WriteIndented = true })}");
                    //Console.WriteLine($"data as string {Packet.ToUTF8String()}");
                    // Prevent replay attacks here*

                    //Console.WriteLine($"[Server] [{Header.PacketAction}]: {Packet.ToUTF8String()}");

                    if (Header.Encryption != PacketEncryption.NONE && Packet.IsValidJSON(out PacketEncrypted encrypted) && encrypted.TryDecrypt(Packer, out byte[] Decrypted))
                    {
                        Packet = Decrypted;

                        //if(Header.PacketAction == PacketAction.Ping || Header.PacketAction)

                        //Console.WriteLine($"[Server] [{Header.PacketAction}] [Auto-Decrypted]: {Decrypted.ToUTF8String()}");
                        //Console.WriteLine($"[Server] [{Header.PacketAction}]: {Packet.ToUTF8String()}");
                        OnDataReceived.Invoke(ClientHandle, Header, Packet);
                    }
                    else if (Header.Encryption == PacketEncryption.NONE) { OnDataReceived.Invoke(ClientHandle, Header, Packet); }

                    // Invalid packet - either send it back to the client or handle it here in the peer log idk right now
                    else { }
                }


            }
        }
    }

    public class ClientHandler
    {

    }



    public void HandleClientConnected(Socket client)
    {
        Task.Run(async () =>
        {
            //bool IsAuthenticating = false;
            //bool IsAuthenticated = false;

            string ClientEndPoint = client.RemoteEndPoint.ToString();

            // This server broadcasts itself over Multicast [or however it is planned to work for discovery over the internet]
            // This handles all peer related code below, as the server has the majority of control of the "peer" 
            Console.WriteLine($"[SERVER] Client connected to the server [{ClientEndPoint}]");

            CancellationTokenSource _ServerToken = ServerToken;
            CancellationTokenSource ClientToken = new CancellationTokenSource();

            // Keeps a valid buffer span to reuse
            var Client = client;
            NetworkBuffer Buffers = new NetworkBuffer();


            var SelfPeer = Self;

            HeartBeat heartBeat = new HeartBeat(ref SelfPeer);
            //heartBeat.IsEnabled = false; // Disabled due to c++ not having support yet
            //IsAuthenticated = false; // Enabled so we can skip auth for c++ - disabled now so we can build the auth system

            // Creates a client handle 
            ServerClientHandle ClientHandle = new ServerClientHandle(client, Buffers, DateTime.UtcNow, ref ClientToken);
            PacketHelper Packer = new PacketHelper(ref Client, ref SelfPeer, ref ClientHandle, ref _ServerToken);
            ClientHandle.AddPacketHelper(ref Packer);
            ClientHandle.AddHeartBeat(ref heartBeat);
            Clients.Add(ClientHandle); // Remember to update client Id


            //Console.WriteLine($"[Server] Client has connected to me [{ClientEndPoint}]"); //{NetworkUtils.GetLocalLanIp()}:{((IPEndPoint)Client.RemoteEndPoint).Port}
            Console.WriteLine($"[Server] Client: {ClientEndPoint} - Me: {NetworkUtils.GetLocalLanIp()}:{((IPEndPoint)Client.LocalEndPoint).Port} - ServerId: {Self.PeerId}");
            // Handles client while token is still valid and the client hasnt timed out


            //_ = ReadMessages(client, Packer);


            //return;
            while (!ClientToken.IsCancellationRequested || Client.IsGracefulShutdown())
            {
                //Thread.Sleep(5); // Handles client data at a certain time per loop
                await Task.Delay(5, ClientToken.Token);
                // Continue until authentication is complete

                if (Packer.IsAuthenticating) continue;
                if (!Packer.IsAuthenticated)
                {
                    Packer.IsAuthenticating = true;
                    OnAuthenticationRequested.Invoke(Client, Packer);
                }


                //Check for ping every so often - client doesnt need to respond but they at least need to receive the data (if they dont respond to ping we cant check the latency)
                if (!heartBeat.TrySendHeartBeat(ref Packer, out bool IsDisconnected) && IsDisconnected && !heartBeat.FirstBeat)
                {
                    ClientToken.Cancel();

                    if (Self.OperationMode == PeerState.Peer)
                    {
                        var PeerInfo = Self.FindPeerById(ClientHandle.Id);

                        if (PeerInfo is not null)
                        {
                            Clients.Remove(PeerInfo.Value.Item1);
                            Self.ConnectedPeers.Remove(PeerInfo.Value.Item2);

                            // Announce to all other clients that peer disconnected
                            //Self.ConnectedPeers.ForEach(Peer => Peer.PacketHelper.SendPacket(PeerInfo.Value.Item2.ToJSON().ToUTF8Byte(), PacketAction.PeerLeave));
                            Self.Broadcast(PeerInfo.Value.Item2.ToJSON().ToUTF8Byte(), PacketType.Peer, PacketAction.Leave);
                            
                        }
                    }
                    else Clients.Remove(ClientHandle);



                    // Figure out why our client now times out**** - idk why this is here (why would I question why it timed out)
                    Console.WriteLine("[Server] Client Timed out");
                    return;
                }



                // Receive regular data as a test from c++
                //int bytesRead = Client.Receive(tempBuffer, 0, tempBuffer.Length, SocketFlags.None);
                //Console.WriteLine($"C++ data received ->\nSize: {bytesRead} - DATA: {string.Join(" ", tempBuffer.Select(x => x.ToString("X2")))}");

                // Automatically read messages, and decrypt everything here if encrypted
                // V3
                //var received = Client.ReceiveFullPacketAsync(Packer).GetAwaiter().GetResult();
                //if (received is null) continue;
                //Span<byte> Packet = received.GetPayloadSpan();
                //var Header = (PacketHeader)received.Header;

                // V1
                //byte[] Packet = Client.ReceivePacket(ref Packer, out PacketHeader Header);
                // V2
                var received = Client.ReceivedPacket(ref Packer, out PacketHeader Header);
                if (received is null) continue;
                Span<byte> Packet = received.GetPayloadSpan();

                OnPacketReceived?.Invoke(Packer, received);

                //Console.WriteLine($"HeaderInfo: {Header.ToJSON(new System.Text.Json.JsonSerializerOptions() { WriteIndented = true})}");
                if (Header.Type != PacketType.NONE)
                {
                    //Console.WriteLine($"C++ data received ->\nSize: {Packet.Length} - DATA: {string.Join(" ", Packet.Select(x => x.ToString("X2")))}");
                    //Console.WriteLine($"HeaderInfo: {Header.ToJSON(new System.Text.Json.JsonSerializerOptions() { WriteIndented = true })}");
                    //Console.WriteLine($"data as string {Packet.ToUTF8String()}");
                    // Prevent replay attacks here*

                    //Console.WriteLine($"[Server] [{Header.PacketAction}]: {Packet.ToUTF8String()}");

                    if (Header.Encryption != PacketEncryption.NONE && Packet.IsValidJSON(out PacketEncrypted encrypted) && encrypted.TryDecrypt(Packer, out byte[] Decrypted))
                    {
                        Packet = Decrypted;

                        //if(Header.PacketAction == PacketAction.Ping || Header.PacketAction)

                        //Console.WriteLine($"[Server] [{Header.PacketAction}] [Auto-Decrypted]: {Decrypted.ToUTF8String()}");
                        //Console.WriteLine($"[Server] [{Header.PacketAction}]: {Packet.ToUTF8String()}");
                        OnDataReceived.Invoke(ClientHandle, Header, Packet);
                    }
                    else if (Header.Encryption == PacketEncryption.NONE) { OnDataReceived.Invoke(ClientHandle, Header, Packet); }

                    // Invalid packet - either send it back to the client or handle it here in the peer log idk right now
                    else { }
                }

                // This is to test the encryption/decryption
                //Packer.SendUTF8Packet("I am server!");


                // Everything past here needs to be authenticated with ChaCha
                //byte[] Packet = Client.ReceiveValidatedPacket(ref heartBeat, ref Packer, out PacketHeader Header);


                ////heartBeat.SetLastBeat();



                //if (Header.PacketAction != PacketAction.Empty) continue;



            }
        });
    }



    public class SecureChannelResult
    {
        public bool IsSecured { get; set; } = false;
        public bool IsSuccess { get; set; } = false;
        public string? ErrorMessage { get; set; }
    }

    public async Task<SecureChannelResult> EstablishSecureChannelAsync(ServerClientHandle ClientHandle)
    {
        SecureChannelResult result = new SecureChannelResult();

        // SET A TIMEOUT FOR THIS AUTHENTICATION
        //   * IF TIMEOUT FAILS REJECT CONNECTION
        //   * ALLOW UNSECURED IF WE SUPPORT IT

        // TIMEOUT TIME SET TO 30s MAX 
        //   * SHOULDN'T TAKE MORE THAN 100-500ms 
        //   * ALLOW LEEWAY FOR SLOWER CONNECTIONS

        // ALLOW FOR 10 SECONDS PER RECEIVE
        //  ; Exception is first SYN to generate keys.

        PacketAuthentication Auth;
        var Packer = ClientHandle.PacketHelper;
        int Loop = 0;
        // SECURE OUT CONNECTION TO THE CLIENT
        while (!ClientHandle.PacketHelper.IsAuthenticated)
        {
            if (Loop >= 6)
            {
                // AUTHENTICATION TIMEDOUT
                Console.WriteLine("30s has passed and hasnt completed authentication");
                result.ErrorMessage = "Failed to complete authentication under 30 seconds.";
                return result;
            }

            int Timeout = 60;
            Console.WriteLine("[SERVER] -> waiting for authentication packets");

            // DEBUG CHECK TO SEE IF THE SOCKET IS STILL CONNECTED 
            //if (ClientHandle.Connection.IsSocketConnected()) Console.WriteLine("connected");
            try
            {
                // GIVES CLIENT TIME TO SEND SYN BUT AFTER THAT EXPECT FASTER REPLIES
                if (Loop > 0 && Timeout == 60) Timeout = 10;

                using (var received = await ClientHandle.Connection.ReceiveFullPacketAsync(ClientHandle.PacketHelper))
                {
                    if (received is null) continue;

                    Span<byte> Packet = received.GetPayloadSpan();
                    PacketHeader Header = (PacketHeader)received.Header;

                    // AUTHENTICATION IS UNDER PACKET TYPE CONTROL
                    // USING ACTION AS HOW TO HANDLE IT
                    if (Header.Type != PacketType.Control) continue;

                    Console.WriteLine($"[Server] received authentication [{Header.Action}] packet from => [{Header.OriginPeerId}]");

                    // CREATE REUSABLE FUNCTION FOR HANDLING OUR PACKETS 
                    // SO WE CAN MODIFY EVERYTHING ALL AT ONCE
                    // WE ONLY HAVE MULTIPLE TO EXPIREMENT WITH THE AUTHENTICATION FLOW.

                    switch (Header.Action)
                    {
                        case PacketAction.SYN:
                            if (Packet.IsValidJSON(out PacketSYN SYN))
                            {
                                if (SYN.Authentication.EncryptionType != PacketEncryption.RSA) break;

                                // UPDATES OUR CLIENT PEER ID BASED ON PACKET 
                                ClientHandle.Id = Header.OriginPeerId;

                                // HANDLE CLIENT SYN WITH DEVICE DATA 
                                //Console.WriteLine(SYN.ToIndentedJSON());
                                Packer.EncryptionKeys.SetRemoteRSAKey(SYN.Authentication.KeyData);

                                // SEND SERVER RSA KEY 
                                if (Packer.EncryptionKeys is null) Console.WriteLine("EncryptionKeys is null");
                                else if (Packer.EncryptionKeys.LocalRSAKeys is null) Console.WriteLine("RSAKeys are null");

                                Auth = new PacketAuthentication()
                                {
                                    EncryptionType = PacketEncryption.RSA,
                                    KeyData = Packer.EncryptionKeys.LocalRSAKeys.PublicKey
                                };

                                int sentSYNAck = Packer.SendPacket(Auth.ToJSON().ToUTF8Byte(), PacketType.Control, PacketAction.SYNACK, PacketEncoding.NONE, PacketEncryption.NONE, PacketRoute.Direct, null);
                                Console.WriteLine($"[Server] sent SYNAck: {sentSYNAck}");
                            }
                            break;
                        case PacketAction.ACK:
                            if (Packet.IsValidJSON(out PacketEncrypted encrypted))
                            {
                                if (encrypted.EncryptionType != PacketEncryption.RSA) break;

                                if (encrypted.TryDecryptInto(Packer.EncryptionKeys.LocalRSAKeys.PrivateKey, out Auth))
                                {
                                    //Console.WriteLine($"[SERVER] => \n{Auth.ToIndentedJSON()}");

                                    if (Auth.EncryptionType != PacketEncryption.ChaCha20Poly1305) break;

                                    Packer.EncryptionKeys.ChaChaKey = Auth.KeyData;

                                    Packer.SendPacket(Auth.ToJSON().ToUTF8Byte(), PacketType.Control, PacketAction.ACK, PacketEncoding.NONE, PacketEncryption.ChaCha20Poly1305, PacketRoute.Direct, null);
                                    Console.WriteLine("[Server] sent encrypted auth with chachakey");
                                }

                            }
                            break;
                        case PacketAction.READY:
                            Console.WriteLine($"[Server]:{Self.PeerId} [Ready] Connection authenticated with [{ClientHandle.Id}]");
                            Packer.SendPacket([], PacketType.Control, PacketAction.READY, PacketEncoding.NONE, PacketEncryption.NONE, PacketRoute.Direct, null);
                            Packer.IsAuthenticated = true;
                            Packer.IsAuthenticating = false;
                            result.IsSuccess = true;
                            return result;
                    }
                }
            }
            catch { }//(Exception Ex) { Console.WriteLine(Ex.Message); }


            Loop++;
        }


        //Console.WriteLine(ClientHandle.PacketHelper.IsAuthenticated);
        //Console.WriteLine("no longer authenticating");
        return result;
    }

    public async Task HandleClientConnectedAsync(Socket client)
    {
        string ClientEndPoint = client.RemoteEndPoint.ToString();

        // This server broadcasts itself over Multicast [or however it is planned to work for discovery over the internet]
        // This handles all peer related code below, as the server has the majority of control of the "peer" 
        Console.WriteLine($"[SERVER] Client connected to the server [{ClientEndPoint}]");

        CancellationTokenSource _ServerToken = ServerToken;
        CancellationTokenSource ClientToken = new CancellationTokenSource();

        // Keeps a valid buffer span to reuse
        var Client = client;
        NetworkBuffer Buffers = new NetworkBuffer();

        // CREATE HEARTBEAT INSTANCE TO TRACK PING/PONGS
        var SelfPeer = Self;
        HeartBeat heartBeat = new HeartBeat(ref SelfPeer);

        // Creates a client handle 
        ServerClientHandle ClientHandle = new ServerClientHandle(client, Buffers, DateTime.UtcNow, ref ClientToken);
        PacketHelper Packer = new PacketHelper(ref Client, ref SelfPeer, ref ClientHandle, ref _ServerToken);
        ClientHandle.AddPacketHelper(ref Packer);
        ClientHandle.AddHeartBeat(ref heartBeat);
        Clients.Add(ClientHandle); // Remember to update client Id

        var Handle = Clients.Find(x => x.Connection == ClientHandle.Connection);

        Console.WriteLine($"[Server] Client: {ClientEndPoint} - Me: {NetworkUtils.GetLocalLanIp()}:{((IPEndPoint)Client.LocalEndPoint).Port} - ServerId: {Self.PeerId}");

        var stopwatch = Stopwatch.StartNew();

        // CREATE STOPWATCH TO VIEW AUTHENTICATION SPEED

        // CREATE BLOCKING HERE SO THAT WE ARE FORCED TO AUTHENTICATE BEFORE GOING ANY FURTHER
        var result = await EstablishSecureChannelAsync(Handle);

        stopwatch.Stop();

        Console.WriteLine($"[SERVER] Authentication took: {stopwatch.ElapsedMilliseconds}ms");

        //if (ClientHandle.PacketHelper.EncryptionKeys.ChaChaKey is null) Self.TCPServer.InvokeDebugMessage($"[AFTER] ChaChaKey: reported null - IsServer: {Handle.PacketHelper.IsServer()}");
        //else Self.TCPServer.InvokeDebugMessage($"[AFTER] ChaChaKey: reported not null - IsServer: {Handle.PacketHelper.IsServer()}");


        // DISCONNECT 
        if (!result.IsSuccess)
        {
            Console.WriteLine("Client authentication has failed");
            ClientToken.Cancel();
            ClientHandle.Connection.Disconnect(true);

            if (Self.OperationMode == PeerState.Peer)
            {
                var PeerInfo = Self.FindPeerById(ClientHandle.Id);

                if (PeerInfo is not null)
                {
                    Clients.Remove(PeerInfo.Value.Item1);
                    Self.ConnectedPeers.Remove(PeerInfo.Value.Item2);

                    // Announce to all other clients that peer disconnected
                    //Self.ConnectedPeers.ForEach(Peer => Peer.PacketHelper.SendPacket(PeerInfo.Value.Item2.ToJSON().ToUTF8Byte(), PacketAction.PeerLeave));
                    Self.Broadcast(PeerInfo.Value.Item2.ToJSON().ToUTF8Byte(), PacketType.Peer, PacketAction.Leave);

                }
            }
            else Clients.Remove(ClientHandle);

            Console.WriteLine($"[Server] Client -> {ClientHandle.Id} {result.ErrorMessage}");
            return;
        }

        // RECEIVE ALL MESSAGES HERE
        _ = ReadMessageAsync(ClientHandle); //Client, Packer, ClientToken

        // HANDLE PING AND DISCONNECT
        while (!ClientToken.IsCancellationRequested || Client.IsSocketConnected())
        {
            await Task.Delay(5, ClientToken.Token);


            //Console.WriteLine(Packer.IsAuthenticated);
            if (!Packer.IsAuthenticated) continue;

            if (!heartBeat.TrySendHeartBeat(ref Packer, out bool IsDisconnected) && IsDisconnected && !heartBeat.FirstBeat)
            {
                ClientToken.Cancel();

                if (Self.OperationMode == PeerState.Peer)
                {
                    var PeerInfo = Self.FindPeerById(ClientHandle.Id);

                    if (PeerInfo is not null)
                    {
                        Clients.Remove(PeerInfo.Value.Item1);
                        Self.ConnectedPeers.Remove(PeerInfo.Value.Item2);

                        // Announce to all other clients that peer disconnected
                        //Self.ConnectedPeers.ForEach(Peer => Peer.PacketHelper.SendPacket(PeerInfo.Value.Item2.ToJSON().ToUTF8Byte(), PacketAction.PeerLeave));
                        Self.Broadcast(PeerInfo.Value.Item2.ToJSON().ToUTF8Byte(), PacketType.Peer, PacketAction.Leave);

                    }
                }
                else Clients.Remove(ClientHandle);



                // Figure out why our client now times out**** - idk why this is here (why would I question why it timed out)
                Console.WriteLine("[Server] Client Timed out");
                return;
            }
        }
    }

    public async Task HandleAuthenticationAsync(ServerClientHandle ClientHandle, ReceivedPacket<IPacketHeaderIdentifier> ReceivedPacket)
    {
        // Use this for auth timeout (if timeout takes more than 30s close connection)
        DateTime dateTime = DateTime.Now;
        PacketAuthentication Auth;

        var Packer = ClientHandle.PacketHelper;
        var Client = ClientHandle.Connection;

        Console.WriteLine("[Server] handling authentication");

        // If client can find the handle with its socket set the peerId (it should be able to)
        ServerClientHandle? Handle = Clients.Find(x => x.Connection == Client);
        Console.WriteLine("test");

        PeerTable ConnectedPeer = new PeerTable();

        Span<byte> Packet = ReceivedPacket.GetPayloadSpan();
        PacketHeader Header = (PacketHeader)ReceivedPacket.Header;

        Console.WriteLine(Header.ToJSON(new System.Text.Json.JsonSerializerOptions() { WriteIndented = true }));


        if (Header.Type != PacketType.NONE)
        {
            //Console.WriteLine("header is not none!");
            switch (Header.Action)
            {
                default:
                    // NOTHING ELSE SHOULD BE READ WHILE AUTH IS BEING DONE
                    // Client sends PeerJoin but cannot be read because we are still here 
                    Console.WriteLine($"Server received data other than Auth action\n{Packet.ToUTF8String()} - {Header.Action.ToString()}");

                    // Send some type of error response
                    break;
                case PacketAction.SYN: // SYN from client - client sends peer list
                    Console.WriteLine("[Server] [SYN] received");

                    if (Packet.IsValidJSON(out Guid PeerId))
                    {
                        if (Handle is not null) Handle.Id = PeerId;
                        Console.WriteLine($"[Server] received valid [SYN] from {PeerId}");
                        

                        // Verify RSA keys exist
                        if (Packer.EncryptionKeys is null)
                        {
                            Console.WriteLine("EncryptionKeys is null");
                        }
                        else if (Packer.EncryptionKeys.LocalRSAKeys is null)
                        {
                            Console.WriteLine("RSAKeys are null");
                        }

                        Auth = new PacketAuthentication()
                        {
                            EncryptionType = PacketEncryption.RSA,
                            KeyData = Packer.EncryptionKeys.LocalRSAKeys.PublicKey
                        };

                        int sentSYNAck = Packer.SendPacket(Auth.ToJSON().ToUTF8Byte(), PacketType.Control, PacketAction.SYNACK, PacketEncoding.NONE, PacketEncryption.NONE, PacketRoute.Direct, null);
                        Console.WriteLine($"[Server] sent SYNAck: {sentSYNAck}");
                        //Packer.SendUTF8Packet(Auth.ToJSON(), PacketAction.SYNAck, false);
                        // Control, SYNAck
                    }
                    else Console.WriteLine("Invalid SYN");
                    break;
                case PacketAction.ACK:
                    Console.WriteLine($"[Server] received [ACK] from {Handle?.Id}");

                    if (Packet.IsValidJSON(out PacketEncrypted encrypted))
                    {
                        //Console.WriteLine($"[Server] [ACK] successfully parsed into encrypted packet");

                        if (encrypted.TryDecryptInto(Packer.EncryptionKeys.LocalRSAKeys.PrivateKey, out Auth))
                        {
                            Console.WriteLine($"[Server] [ACK] successfully decrypted packet");
                            Packer.EncryptionKeys.ChaChaKey = Auth.KeyData; 
                            Packer.SendPacket(Auth.ToJSON().ToUTF8Byte(), PacketType.Control, PacketAction.ACK, PacketEncoding.NONE, PacketEncryption.ChaCha20Poly1305, PacketRoute.Direct, null);
                            //Packer.SendUTF8Packet(Auth.ToJSON(), PacketAction.ACK, true, PacketEncryption.ChaCha20Poly1305);
                            // Control, ACK
                        }
                        else Console.WriteLine($"[Server] [ACK] failed to decrypt encrypted packet");

                        // Handle situation about being stuck here if auth fails
                    }
                    else Console.WriteLine($"[Server] [ACK] [{Handle?.Id}] failed to parse into encrypted packet...");

                    break;
                case PacketAction.READY:
                    // PREVENT SKIPPAGE, CHECK FOR PACKET ID TO MAKE SURE IT GOT HERE WHEN IT SHOULD

                    Console.WriteLine($"[Server] [Ready] Connection authenticated with [{Handle?.Id}]");
                    Packer.SendPacket("<READY>".ToUTF8Byte(), PacketType.Control, PacketAction.READY, PacketEncoding.NONE, PacketEncryption.NONE, PacketRoute.Direct, null);
                    //Packer.SendUTF8Packet("<READY>", PacketAction.Ready, false);
                    // Control, Ready
                    Packer.IsAuthenticated = true;
                    Packer.IsAuthenticating = false;
                    //Packer.onAuthenticated.Invoke();
                    break;
            }
        }

    }

    public void HandleAuthentication(Socket Client, PacketHelper Packer)
    {
        // Use this for auth timeout (if timeout takes more than 30s close connection)
        DateTime dateTime = DateTime.Now;
        PacketAuthentication Auth;

        Console.WriteLine("[Server] handling authentication");
        
        PeerTable ConnectedPeer = new PeerTable();

        // Stay here until we are authenticated
        while (!Packer.IsAuthenticated)
        {
            // V3
            var received = Client.ReceiveFullPacketAsync(Packer).GetAwaiter().GetResult();
            if (received is null) { Console.WriteLine("receive is null"); continue; }
            Span<byte> Packet = received.GetPayloadSpan();
            var Header = (PacketHeader)received.Header;
            Console.WriteLine("packet receieved and read");

            Console.WriteLine(Header.ToJSON(new System.Text.Json.JsonSerializerOptions() { WriteIndented = true}));

            // V1
            //byte[] Packet = Client.ReceivePacket(ref Packer, out PacketHeader Header);


            // V2
            //using var received = Client.ReceivedPacket(ref Packer, out PacketHeader Header);
            //if (received is null) continue;
            //else
            //{
            //    Console.WriteLine("not null");
            //    Console.WriteLine($"HeaderType: {received.Header.GetType().Name}");

            //    //string json = received.ToJSON(new System.Text.Json.JsonSerializerOptions() { WriteIndented = true });
            //    //Console.WriteLine($"ReceivedJSON => {json}");
            //}
            //Span<byte> Packet = received.GetPayloadSpan();



            //PacketHeader Header = new PacketHeader();
            //if (received.Header is PacketHeader pHeader)
            //{
            //    Header = pHeader;
            //    Console.WriteLine("header is v1 type");

            //    string json = Header.ToJSON(new System.Text.Json.JsonSerializerOptions() { WriteIndented = true });
            //    Console.WriteLine($"ReceivedHeader => {json}");
            //}


            //return;
            if (Header.Type != PacketType.NONE)
            {
                switch (Header.Action)
                {
                    
                    default:
                        // NOTHING ELSE SHOULD BE READ WHILE AUTH IS BEING DONE
                        // Client sends PeerJoin but cannot be read because we are still here 
                        Console.WriteLine($"Server received data other than Auth action\n{Packet.ToUTF8String()} - {Header.Action.ToString()}");

                        // Send some type of error response
                        break;
                    case PacketAction.SYN: // SYN from client - client sends peer list
                        Console.WriteLine("[Server] [SYN] received");


                        if(Packet.IsValidJSON(out Guid PeerId))
                        {
                            Console.WriteLine($"[Server] received valid [SYN] from {PeerId}");

                            // If client can find the handle with its socket set the peerId (it should be able to)
                            ServerClientHandle? Handle = Clients.Find(x => x.Connection == Client);
                            if (Handle is not null) Handle.Id = PeerId;

                            ConnectedPeer.PeerId = PeerId;

                            // Verify RSA keys exist
                            if(Packer.EncryptionKeys is null)
                            {
                                Console.WriteLine("EncryptionKeys is null");
                            }
                            else if (Packer.EncryptionKeys.LocalRSAKeys is null)
                            {
                                Console.WriteLine("RSAKeys are null");
                            }

                            Auth = new PacketAuthentication()
                            {
                                EncryptionType = PacketEncryption.RSA,
                                KeyData = Packer.EncryptionKeys.LocalRSAKeys.PublicKey
                            };

                            int sentSYNAck = Packer.SendPacket(Auth.ToJSON().ToUTF8Byte(), PacketType.Control, PacketAction.SYNACK, PacketEncoding.NONE, PacketEncryption.NONE, PacketRoute.Direct, null);

                            //Packer.SendUTF8Packet(Auth.ToJSON(), PacketAction.SYNAck, false);
                            // Control, SYNAck
                        }
                        else Console.WriteLine("Invalid SYN");
                        break;
                    case PacketAction.ACK:
                        Console.WriteLine($"[Server] received [ACK] from {ConnectedPeer.PeerId}");

                        if(Packet.IsValidJSON(out PacketEncrypted encrypted))
                        {
                            //Console.WriteLine($"[Server] [ACK] successfully parsed into encrypted packet");

                            if(encrypted.TryDecryptInto(Packer.EncryptionKeys.LocalRSAKeys.PrivateKey, out Auth))
                            {
                                Console.WriteLine($"[Server] [ACK] successfully decrypted packet");
                                Packer.EncryptionKeys.ChaChaKey = Auth.KeyData;
                                Packer.SendPacket(Auth.ToJSON().ToUTF8Byte(), PacketType.Control, PacketAction.ACK, PacketEncoding.NONE, PacketEncryption.ChaCha20Poly1305, PacketRoute.Direct, null);
                                //Packer.SendUTF8Packet(Auth.ToJSON(), PacketAction.ACK, true, PacketEncryption.ChaCha20Poly1305);
                                // Control, ACK
                            }
                            else Console.WriteLine($"[Server] [ACK] failed to decrypt encrypted packet");

                            // Handle situation about being stuck here if auth fails
                        }
                        else Console.WriteLine($"[Server] [ACK] [{ConnectedPeer.PeerId}] failed to parse into encrypted packet...");

                        break;
                    case PacketAction.READY:
                        // PREVENT SKIPPAGE, CHECK FOR PACKET ID TO MAKE SURE IT GOT HERE WHEN IT SHOULD

                        Console.WriteLine($"[Server] [Ready] Connection authenticated with [{ConnectedPeer.PeerId}]");
                        Packer.SendPacket("<READY>".ToUTF8Byte(), PacketType.Control, PacketAction.READY, PacketEncoding.NONE, PacketEncryption.NONE, PacketRoute.Direct, null);
                        //Packer.SendUTF8Packet("<READY>", PacketAction.Ready, false);
                        // Control, Ready
                        Packer.IsAuthenticated = true;
                        Packer.IsAuthenticating = false;
                        //Packer.onAuthenticated.Invoke();
                        break;
                }
            }
        }

        // End of Auth
        //Console.WriteLine("Packer Is Authenticated now");
    }



    public void HandleAction(PacketHeader Header, ReadOnlyMemory<byte> Data, PacketHelper Helper, HeartBeat heartBeat)
    {
        byte[] DATA = Array.Empty<byte>();
        string UTF8 = string.Empty;

        switch (Header.Type)
        {
            case PacketType.Control:
                switch (Header.Action)
                {
                    case PacketAction.Ping: heartBeat.HandleHeartBeatActions(Header, Data, Helper); break;
                    case PacketAction.Pong: heartBeat.HandleHeartBeatActions(Header, Data, Helper); break;
                }
                break;
            case PacketType.Peer:
                switch (Header.Action)
                {
                    case PacketAction.Join: 
                        Console.WriteLine("peer joined");

                        // Check if in packet init class
                        if (UTF8.IsValidJSON(out PeerTable initPeer)) Self.AddPeer(Helper.ClientHandle, initPeer);
                        else if (UTF8.IsValidJSON(out IEnumerable<PeerTable> initPeers)) Self.AddPeers(Helper.ClientHandle, initPeers);
                        break;
                    case PacketAction.Leave: break;
                }
                
                break;
        }

        //switch (Header.Action)
        //{
        //    case PacketAction.Ping: heartBeat.HandleHeartBeatActions(Header, Data, Helper); break;
        //    case PacketAction.Pong: heartBeat.HandleHeartBeatActions(Header, Data, Helper); break;
        //    case PacketAction.SYN:
        //        break;

        //    case PacketAction.Data:
        //        //OnDataReceived.Invoke(Helper.ClientHandle, Data.Span);
        //        break;

        //    case PacketAction.Voice:
        //        NETConnect.Audio.Audio.QueueAudio(Data.ToArray());
        //        break;

        //    case PacketAction.PeerJoin:

        //        DATA = Data.ToArray();
        //        UTF8 = DATA.ToUTF8String();

        //        //Console.WriteLine("peer joined");

        //        // Check if in packet init class
        //        if (UTF8.IsValidJSON(out PeerTable initPeer)) Self.AddPeer(Helper.ClientHandle, initPeer);
        //        else if (UTF8.IsValidJSON(out IEnumerable<PeerTable> initPeers)) Self.AddPeers(Helper.ClientHandle, initPeers);
        //        break;

        //    default:
        //        //OnDataReceived.Invoke(Helper.ClientHandle, Data.Span);
        //        break;
        //}
    }

    public void InvokeOnPeerConnected(ServerClientHandle ClientHandle, PeerTable initPeer) => OnPeerConnected?.Invoke(ClientHandle, initPeer);

    public void HandleDataReceived(ServerClientHandle Client, PacketHeader Header, ReadOnlySpan<byte> Data)
    {
        byte[] DATA = Data.ToArray();
        string UTF8 = DATA.ToUTF8String();
        // Print the message that was received from the client
        //Console.WriteLine($"Server Received => [{Header.PacketAction}] Encryption: [{Header.PacketEncryption}] \"{UTF8}\""); //Client.Buffers.CharBuffer
        HandleAction(Header, DATA, Client.PacketHelper, Client.HeartBeat);

    } 
}
