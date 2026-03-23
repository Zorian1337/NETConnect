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
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NETConnect;

// Each instance of this class should be able to create a new server running on a different port.
public class BaseTCPServer : BaseServerProperties
{
    public Peer Self { get; set; }
    public Socket SocketServer { get; set; }
    public CancellationTokenSource ServerToken { get; set; }
    public IPAddress Address { get; set; }


    public string ServerAddress { get; set; }
    public int Port { get; set; }


    public PeerTable MyPeerTable { get; set; }

    // Server itself

    /// <summary>
    /// Event for the first connect between the server and a client
    /// At this point the client isnt a peer, nor has it setup any encryption
    /// </summary>
    public event Action<Socket> OnClientConnected;
    public event Action<Socket> OnClientDisconnected;
    public event Action<Socket, PacketHelper> OnAuthenticationRequested;
    public event Action<ServerClientHandle, PacketHeader, ReadOnlySpan<byte>> OnDataReceived;

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

    public void StartServer()
    {
        // Prevent multiple servers from running in the same app instance 

        Task.Run(() =>
        {
            NETConnect.Audio.Audio.Init();

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
                    Self.Multicast.ReadMulticast(); // blocks

                    ServerAddress = $"{NetworkUtils.GetLocalLanIp()}:{Point.Port}";
                    
                    // This breaks when we are using it in something other than a console
                    //if (Environment.UserInteractive && Console.Title != null) Console.Title = $"Server: [{ServerAddress}]";

                    // Create Peer Table here - Might not update the peerId with it as its set within multiicast but will need change anyway later, as thats not a good way to set it
                    MyPeerTable = new PeerTable(ref SelfPeer, NetworkUtils.GetLocalLanIp().ToString(), Point.Port); // this might need fixed, port here and port in multicast are conflicting
                    //(idr if I want this to list the server connection) im pretty sure I do, as I need to have clients connect to the server (so having the server informationa as the peer makes sense)


                    //if (Self.TCPServer.ServerToken is null) OnDebugMessage?.Invoke($"ServerToken is null");
                    //else OnDebugMessage?.Invoke($"ServerToken is not null");

                    Task.Run(async () =>
                    {
                        //OnDebugMessage?.Invoke($"BEFORE Advertising to the multicast : {Self.TCPServer.ServerToken.IsCancellationRequested}"); // 
                        // Multicast broadcasting of the server (only way we can get clients for now)
                        while (!Self.TCPServer.ServerToken.IsCancellationRequested)
                        {
                            //OnDebugMessage?.Invoke($"Advertising to the multicast as {ServerAddress}");
                            Self.Multicast.SendUTF8Message(ServerAddress, MulticastAction.Join);
                            //int DelayPerSecond = 
                            //await Task.Delay((60 * 1000) * 1);
                            await Task.Delay(500 * 60);
                        }
                    });

                   //Using this for informational purposes related to peers

                   Task.Run(() => UpdatePeerDisplay());
                }

                // Handles searching for clients in another thread...
                Task.Run(() =>
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

    public void UpdatePeerDisplay()
    {
        while (!ServerToken.IsCancellationRequested)
        {
            Thread.Sleep(5000);

            //Clear console for a clean display while doing peer mapping
            //Console.Clear();

            Console.WriteLine($"PeerId: {Self.PeerId} - OperationMode: {Self.OperationMode}\n");

            if(Self.OperationMode == PeerState.Peer && Self.ConnectedPeers.Count() > 0)
            {
                // Check if there are any disconnected clients and remove them from our peer list
                if(Self.ConnectedPeers.Any(x => x.Client.Token.IsCancellationRequested))
                {
                    List<PeerTable> expiredPeers = Self.ConnectedPeers.FindAll(x => x.Client.Token.IsCancellationRequested);

                    Self.ConnectedPeers.RemoveAll(x => x.Client.Token.IsCancellationRequested);
                    Self.TCPServer.Clients.RemoveAll(x => x.ClientToken.IsCancellationRequested);

                    Console.WriteLine("Some peers have expired tokens!");
                }

                Console.WriteLine($"Connected Peers: {Self.ConnectedPeers.Count()}\n");

                Console.WriteLine($"Peers: \n{String.Join("\n", Self.ConnectedPeers.Select(x => $"{x.PeerId} [{x.Address}:{x.Port}]"))}\n");

                Console.WriteLine($"MyStats: {Self.NetStats.ToJSON(new System.Text.Json.JsonSerializerOptions() { WriteIndented = true})}");
            }
            else if (Self.OperationMode == PeerState.Server && Self.TCPServer.Clients.Count() > 0)
            {
                Console.WriteLine($"Connected Clients: {Self.TCPServer.Clients.Count()}\n");

                Console.WriteLine($"Clients: \n{String.Join("\n", Self.TCPServer.Clients.Select(x => $"[{x.Id}]"))}");
            }

        }
    }

    public (bool HasSentSYN, bool IsAuthorized) TLSV2(Socket Client, (bool HasSentSYN, bool IsAuthorized) ExistingParams, PacketHelper Packer)
    {
        if (ExistingParams.HasSentSYN && ExistingParams.IsAuthorized) return (true, true);

        bool IsAuthorized = ExistingParams.IsAuthorized;
        bool HasSentSYN = ExistingParams.HasSentSYN;


        if (!HasSentSYN)
        {
            PacketAuthentication auth = new PacketAuthentication()
            {
                EncryptionType = PacketEncryptionType.RSA,
                KeyData = Packer.EncryptionKeys.LocalRSAKeys.PublicKey
            };

            string Auth = auth.ToJSON();

            //Console.WriteLine($"sending Auth to {ClientEndPoint}");
            Packer.SendUTF8Packet(Auth, PacketActionType.SYN);
            //Console.WriteLine($"[Server] Sent (AuthPacket-RSAPubkey)SYN to client"); //\n{auth.ToJSON()
            HasSentSYN = true;
        }

        // Might need to rework this to where it doesnt return anything if its not valid (FIX: set default data length to -1 to symbolize not valid data)
        byte[] Packet = Client.ReceivePacket(out PacketHeader Header);

        // Ignore packet if nothing was read
        if (Header.PacketAction != PacketActionType.Empty)
        {
            string JSON = string.Empty;
            PacketAuthentication Auth;

            // Handle only Authorization related requests (and ping just because there is no system for it globally) - (FIX: automatically handle it while we are reading packet data)

            switch (Header.PacketAction)
            {
                // Only Respond to SYNAck
                case PacketActionType.SYNAck:
                    //Console.WriteLine("[Server] received [SYNAck]");

                    JSON = Packet.ToUTF8String();
                    if (JSON.IsValidJSON(out Auth))
                    {
                        switch (Auth.EncryptionType)
                        {
                            case PacketEncryptionType.RSA:
                                // Set remote RSAPubKey
                                Packer.EncryptionKeys.SetRemoteRSAKey(Auth.KeyData);

                                // Encrypt testing data to confirm key can be successfully read via client
                                // Build a packet with encrypted data and include some hashing data that it can be verified against

                                bool SuccessfullyEncryptedData = true;
                                if(PacketEncrypted.TryEncryptSend(SuccessfullyEncryptedData.ToJSON().ToUTF8Byte(), Auth.EncryptionType, PacketActionType.SYNAck, Packer))
                                {
                                    //Console.WriteLine($"[Server] sent testing data encrypted with client RSAPubKey");
                                }
                                else
                                {
                                    // Report back to the client with some sort of error code indicating that the data either couldnt be encrypted or sent in general (most likely encrypted)
                                }


                                    break;
                        }
                    }


                    break;
                case PacketActionType.ACK:
                    //Console.WriteLine("[Server] received [ACK]");

                    JSON = Packet.ToUTF8String();
                    //Console.WriteLine(JSON);
                    //Console.WriteLine($"EncryptionType: {Header.PacketEncryptionType}");
                    if(Header.PacketEncryptionType != PacketEncryptionType.NONE)
                    {
                        // Any encryption goes

                        if(JSON.IsValidJSON(out PacketEncrypted Encrypted))
                        {
                            

                            // Check header to see what type of encryption this is using
                            switch (Header.PacketEncryptionType) 
                            {
                                case PacketEncryptionType.RSA:
                                    // This should be where the client and server both has the right RSA Keys but there also was a symetric key sent


                                    // DecryptInto version
                                    if(Encrypted.TryDecryptInto(Packer.EncryptionKeys.LocalRSAKeys.PrivateKey, out Auth))
                                    {
                                        //Console.WriteLine($"[Server] [ACK] {Auth.ToJSON()}");
                                        //Console.WriteLine($"[Server] ChaChaKey from client {Auth.KeyData.ToJSON()}");

                                        //Console.WriteLine(Encrypted.EncryptionType);

                                        // This will tell us where to store our key!
                                        switch (Auth.EncryptionType) // Make sure Auth packet is used rather than Encrypted
                                        {
                                            case PacketEncryptionType.RSA: // This shouldnt be used yet, so only making ChaCha for now
                                                break;
                                            case PacketEncryptionType.ChaCha20Poly1305: // Using ChaCha for our sym key, as its apparently easy to use for low end devices
                                                //Console.WriteLine("[Server] [ACK] received ChaCha sym key!");
                                                Packer.EncryptionKeys.ChaChaKey = Auth.KeyData;

                                                // Send client ACK in ChaCha telling them that we have received their key
                                                if(PacketEncrypted.TryEncryptSend(new PacketAuthentication() { EncryptionType = PacketEncryptionType.ChaCha20Poly1305, KeyData = Auth.KeyData}.ToJSON().ToUTF8Byte(), 
                                                    PacketEncryptionType.ChaCha20Poly1305, PacketActionType.ACK, Packer))
                                                {
                                                    Console.WriteLine("[Server] has authenticated the connection");
                                                    // Give the client some time to receive this message
                                                    Thread.Sleep(1000);
                                                    IsAuthorized = true;
                                                }
                                                else
                                                {
                                                    // Restart whole process
                                                    HasSentSYN = false;
                                                }

                                                break;
                                        }
                                    }

                                    break;
                                case PacketEncryptionType.ChaCha20Poly1305: // This will only ever be valid if we were exchanging other keys as this encryption type 

                                    break;
                            }
                        }
                    }
                    break;
            }
        }

        return (HasSentSYN, IsAuthorized);
    }


    public void HandleClientConnected(Socket client)
    {
        Task.Run(() =>
        {
            //bool IsAuthenticating = false;
            //bool IsAuthenticated = false;

            string ClientEndPoint = client.RemoteEndPoint.ToString();
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



            // Use ClientHandle to control all aspects of the connection (I believe I wired it that way, that or PacketHelper)

            //Action onAuthenticated = () =>
            //{
            //    IsAuthenticated = true;
            //    IsAuthenticating = false;
            //    Console.WriteLine($"[Server] Client {ClientEndPoint} authenticated successfully");
            //};

            //Packer.onAuthenticated = onAuthenticated;
            //bool IsAuthenticated = false; // scraping auth for now
            //bool HasSentSYN = false;

            //Console.WriteLine($"[Server] Client has connected to me [{ClientEndPoint}]"); //{NetworkUtils.GetLocalLanIp()}:{((IPEndPoint)Client.RemoteEndPoint).Port}
            Console.WriteLine($"[Server] Client: {ClientEndPoint} - Me: {NetworkUtils.GetLocalLanIp()}:{((IPEndPoint)Client.LocalEndPoint).Port} - ServerId: {Self.PeerId}");
            // Handles client while token is still valid and the client hasnt timed out

            while (!ClientToken.IsCancellationRequested || Client.IsGracefulShutdown())
            {
                Thread.Sleep(5); // Handles client data at a certain time per loop

                // Continue until authentication is complete



                if (Packer.IsAuthenticating) continue;
                if (!Packer.IsAuthenticated)
                {
                    Packer.IsAuthenticating = true;
                    OnAuthenticationRequested.Invoke(Client, Packer);

                }
                //Console.WriteLine("FULLY OUT OF AUTHENTICATION");

                //Console.WriteLine("Client passed authentication");
                //ClientHandle.PacketHelper.SendUTF8Packet("testing");

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
                            //Self.ConnectedPeers.ForEach(Peer => Peer.PacketHelper.SendPacket(PeerInfo.Value.Item2.ToJSON().ToUTF8Byte(), PacketActionType.PeerLeave));
                            Self.Broadcast(PeerInfo.Value.Item2.ToJSON().ToUTF8Byte(), PacketActionType.PeerLeave);
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
                byte[] Packet = Client.ReceivePacket(out PacketHeader Header);

                //Console.WriteLine($"HeaderInfo: {Header.ToJSON(new System.Text.Json.JsonSerializerOptions() { WriteIndented = true})}");
                if (Header.PacketAction != PacketActionType.Empty)
                {
                    //Console.WriteLine($"C++ data received ->\nSize: {Packet.Length} - DATA: {string.Join(" ", Packet.Select(x => x.ToString("X2")))}");
                    //Console.WriteLine($"HeaderInfo: {Header.ToJSON(new System.Text.Json.JsonSerializerOptions() { WriteIndented = true })}");
                    //Console.WriteLine($"data as string {Packet.ToUTF8String()}");
                    // Prevent replay attacks here*

                    //Console.WriteLine($"[Server] [{Header.PacketAction}]: {Packet.ToUTF8String()}");

                    if (Header.PacketEncryptionType != PacketEncryptionType.NONE && Packet.IsValidJSON(out PacketEncrypted encrypted) && encrypted.TryDecrypt(Packer, out byte[] Decrypted))
                    {
                        Packet = Decrypted;

                        //if(Header.PacketAction == PacketActionType.Ping || Header.PacketAction)

                        //Console.WriteLine($"[Server] [{Header.PacketAction}] [Auto-Decrypted]: {Decrypted.ToUTF8String()}");
                        //Console.WriteLine($"[Server] [{Header.PacketAction}]: {Packet.ToUTF8String()}");
                        OnDataReceived.Invoke(ClientHandle, Header, Packet);
                    }
                    else if (Header.PacketEncryptionType == PacketEncryptionType.NONE) { OnDataReceived.Invoke(ClientHandle, Header, Packet); }

                    // Invalid packet - either send it back to the client or handle it here in the peer log idk right now
                    else { }
                }

                // This is to test the encryption/decryption
                //Packer.SendUTF8Packet("I am server!");


                // Everything past here needs to be authenticated with ChaCha
                //byte[] Packet = Client.ReceiveValidatedPacket(ref heartBeat, ref Packer, out PacketHeader Header);


                ////heartBeat.SetLastBeat();



                //if (Header.PacketAction != PacketActionType.Empty) continue;



            }
        });
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
            byte[] Packet = Client.ReceivePacket(out PacketHeader Header);
            if (Header.PacketAction != PacketActionType.Empty)
            {
                switch (Header.PacketAction)
                {
                    case PacketActionType.SYN: // SYN from client - client sends peer list
                        //Console.WriteLine("[Server] [SYN] received");


                        if(Packet.IsValidJSON(out Guid PeerId))
                        {
                            //Console.WriteLine($"[Server] received valid [SYN] from {PeerId}");

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
                                EncryptionType = PacketEncryptionType.RSA,
                                KeyData = Packer.EncryptionKeys.LocalRSAKeys.PublicKey
                            };

                            Packer.SendUTF8Packet(Auth.ToJSON(), PacketActionType.SYNAck, false);
                        }
                        else Console.WriteLine("Invalid SYN");
                        break;
                    case PacketActionType.ACK:
                        //Console.WriteLine($"[Server] received [ACK] from {ConnectedPeer.PeerId}");

                        if(Packet.IsValidJSON(out PacketEncrypted encrypted))
                        {
                            //Console.WriteLine($"[Server] [ACK] successfully parsed into encrypted packet");

                            if(encrypted.TryDecryptInto(Packer.EncryptionKeys.LocalRSAKeys.PrivateKey, out Auth))
                            {
                                //Console.WriteLine($"[Server] [ACK] successfully decrypted packet");
                                Packer.EncryptionKeys.ChaChaKey = Auth.KeyData;
                                Packer.SendUTF8Packet(Auth.ToJSON(), PacketActionType.ACK, true, PacketEncryptionType.ChaCha20Poly1305);
                                
                            }
                            else Console.WriteLine($"[Server] [ACK] failed to decrypt encrypted packet");

                            // Handle situation about being stuck here if auth fails
                        }
                        else Console.WriteLine($"[Server] [ACK] [{ConnectedPeer.PeerId}] failed to parse into encrypted packet...");

                        break;
                    case PacketActionType.Ready:
                        Console.WriteLine($"[Server] [Ready] Connection authenticated with [{ConnectedPeer.PeerId}]");
                        Packer.SendUTF8Packet("<READY>", PacketActionType.Ready, false);
                        Packer.IsAuthenticated = true;
                        Packer.IsAuthenticating = false;
                        //Packer.onAuthenticated.Invoke();
                        break;
                }
            }
        }
    }



    public void HandleAction(PacketHeader Header, ReadOnlyMemory<byte> Data, PacketHelper Helper, HeartBeat heartBeat)
    {
        byte[] DATA = Array.Empty<byte>();
        string UTF8 = string.Empty;

        switch (Header.PacketAction)
        {
            case PacketActionType.Ping: heartBeat.HandleHeartBeatActions(Header, Data, Helper); break;
            case PacketActionType.Pong: heartBeat.HandleHeartBeatActions(Header, Data, Helper); break;
            case PacketActionType.SYN:
                break;



            case PacketActionType.Data:
                //OnDataReceived.Invoke(Helper.ClientHandle, Data.Span);
                break;

            case PacketActionType.Voice:
                NETConnect.Audio.Audio.QueueAudio(Data.ToArray());
                break;

            case PacketActionType.PeerJoin:

                DATA = Data.ToArray();
                UTF8 = DATA.ToUTF8String();

                Console.WriteLine("peer joined");

                // Check if in packet init class
                if (UTF8.IsValidJSON(out PeerTable initPeer)) Self.AddPeer(Helper.ClientHandle, initPeer);
                else if (UTF8.IsValidJSON(out IEnumerable<PeerTable> initPeers)) Self.AddPeers(Helper.ClientHandle, initPeers);
                break;

            default:
                //OnDataReceived.Invoke(Helper.ClientHandle, Data.Span);
                break;
        }
    }

    public void InvokeOnPeerConnected(ServerClientHandle ClientHandle, PeerTable initPeer) => OnPeerConnected?.Invoke(ClientHandle, initPeer);

    public void HandleDataReceived(ServerClientHandle Client, PacketHeader Header, ReadOnlySpan<byte> Data)
    {
        byte[] DATA = Data.ToArray();
        string UTF8 = DATA.ToUTF8String();
        // Print the message that was received from the client
        //Console.WriteLine($"Server Received => [{Header.PacketAction}] Encryption: [{Header.PacketEncryptionType}] \"{UTF8}\""); //Client.Buffers.CharBuffer
        HandleAction(Header, DATA, Client.PacketHelper, Client.HeartBeat);

    } 
}
