using NETConnect.Encryption;
using NETConnect.Encryption.Crypt;
using NETConnect.Encryption.Hash;
using NETConnect.Interfaces;
using NETConnect.MyExtensions;
using NETConnect.MyExtensions.Encryption;
using NETConnect.Network;
using NETConnect.Peers;
using NETConnect.Shared;
using NETConnect.Shared.Packet;
using NETConnect.Shared.Packet.Headers;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices.Swift;
using System.Security.Cryptography;
using System.Text;
using System.Text.Unicode;
using System.Threading.Tasks;
using static NETConnect.BaseTCPServer;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace NETConnect;

public class BaseTCPClient
{
    public Peer Self { get; private set; }
    public Socket SocketClient { get; set; }
    public CancellationTokenSource Token { get; set; }
    public IPEndPoint? EndPoint { get; set; }


    public HeartBeat HeartBeat { get; set; }
    public PacketHelper Packer { get; set; }
    public int Port { get; set; }

    public NetworkBuffer NetworkBuffer { get; set; } 

    public event Action OnConnected;
    public event Func<Task> OnConnectedAsync;

    public event Action OnDisconnected;

    public event Action<Socket, PacketHelper> OnAuthenticationRequested;
    public event Func<PacketHelper, ReceivedPacket<IPacketHeaderIdentifier>, Task> OnAuthenticationRequestedAsync;


    public event Action<PacketHelper, PacketHeader, ReadOnlySpan<byte>> OnDataReceived;
    public event Action<PacketHelper, ReceivedPacket<IPacketHeaderIdentifier>> OnPacketReceived;


    // USING VERSION FROM PACKER TO SEE IF IT'LL HAVE ITS OWN SEPERATED FROM THE SERVER ITSELF
    //public bool IsAuthenticating = false;
    //public bool IsAuthenticated = false;




    //public ServerSettings ServerSettings { get; set; }

    public BaseTCPClient(ref Peer Self) { this.Self = Self; }
    //public BaseTCPClient(Guid PeerId) { this.PeerId = PeerId; }
    public bool TryConnect(string IP, int Port)
    {
        // Init some starting client stuff
        if(SocketClient is null)
        {
            SocketClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            Token = new CancellationTokenSource();

            NetworkBuffer = new NetworkBuffer();

            OnConnected += HandleConnected;

            OnAuthenticationRequested += HandleAuthentication;

            OnDataReceived += HandleOnDataReceived;
        }


        // Parse IP to IPAddress
        if(IPAddress.TryParse(IP, out IPAddress? _IPAddress))
        {
            // Try to connect to server here
            this.EndPoint = new IPEndPoint(_IPAddress, Port);

            try
            {
                SocketClient.Connect(EndPoint); 
                OnConnected?.Invoke();

                // Don't return true on this function until its registered as Authenticated
                // Will need to redo later, as it wont always need to be authenticated
                do { Thread.Sleep(1); }
                while (Packer.IsAuthenticating);

                return true;
            }
            catch (Exception Ex) { Console.WriteLine(Ex.ToString()); Debug.WriteLine($"TryConnect Exception: {Ex.ToString()}"); }
            
            // Failed to parse IPAddress
            return false;
        }

        // Returns false if client didnt connect properly
        return false;
    }



    public async Task<bool> TryConnectAsync(string IP, int Port)
    {
        // Init some starting client stuff
        if (SocketClient is null)
        {
            SocketClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            Token = new CancellationTokenSource();

            NetworkBuffer = new NetworkBuffer();

            OnConnectedAsync += HandleConnectionAsync;

            OnAuthenticationRequestedAsync += HandleAuthenticationAsync;

            OnDataReceived += HandleOnDataReceived;
        }


        // Parse IP to IPAddress
        if (IPAddress.TryParse(IP, out IPAddress? _IPAddress))
        {
            // Try to connect to server here
            this.EndPoint = new IPEndPoint(_IPAddress, Port);

            try
            {
                //Console.WriteLine("connecting");
                await SocketClient.ConnectAsync(EndPoint);//.Connect(EndPoint);
                //Console.WriteLine("connected");

                // RUN THIS ASYNC, BUT DONT AWAIT SO WE CAN RETURN WHEN WE ARE AUTHENTICATED!
                _ = OnConnectedAsync?.Invoke(); 

                // Don't return true on this function until its registered as Authenticated
                // Will need to redo later, as it wont always need to be authenticated
                do { await Task.Delay(1); }
                while (Packer.IsAuthenticating);

                return true;
            }
            catch (Exception Ex) { Console.WriteLine(Ex.ToString()); Debug.WriteLine($"TryConnect Exception: {Ex.ToString()}"); }

            // Failed to parse IPAddress
            return false;
        }

        // Returns false if client didnt connect properly
        return false;
    }

    public async Task ReadMessageAsync(PacketHelper Helper)
    {

        var Client = Helper.Connection;
        var Token = Helper.Token;

        while (!Token.IsCancellationRequested && !Client.IsGracefulShutdown())
        {
            //Console.WriteLine("[CLIENT] -> looping receive messages");

            using (var received = await Client.ReceiveFullPacketAsync(Packer))
            {
                //Console.WriteLine("received packet");
                if (received is null) break; //Console.WriteLine("null packet"); 

                // RECORD ANY PACKET BEING RECEIVED
                OnPacketReceived?.Invoke(Packer, received);

                Span<byte> Packet = received.GetPayloadSpan();
                PacketHeader Header = (PacketHeader)received.Header;

                // HANDLE EVERYTHING ELSE HERE 
                if (Header.Type != PacketType.NONE)
                {
                    //Console.WriteLine($"[Client] [{Header.Action}]: {Packet.ToUTF8String()}");


                    if (Header.Encryption != PacketEncryption.NONE && Packet.IsValidJSON(out PacketEncrypted encrypted) && encrypted.TryDecrypt(Packer, out byte[] Decrypted))
                    {
                        Packet = Decrypted;
                        //Console.WriteLine($"[Client] [{Header.PacketAction}] [Auto-Decrypted]: {Decrypted.ToUTF8String()}");
                        //Console.WriteLine($"[Client] [{Header.PacketAction}]: {Packet.ToUTF8String()}");
                        OnDataReceived.Invoke(Packer, Header, Packet);
                    }
                    else if (Header.Encryption == PacketEncryption.NONE) { OnDataReceived.Invoke(Packer, Header, Packet); }


                }
            }
        }
    }

    public class SecureChannelResult
    {
        public bool IsSecured { get; set; } = false;
        public bool IsSuccess { get; set; } = false;
        public string? ErrorMessage { get; set; }
    }

    public async Task<SecureChannelResult> EstablishSecureChannelAsync()
    {
        SecureChannelResult result = new SecureChannelResult();

        // SET A TIMEOUT FOR THIS AUTHENTICATION
        //   * IF TIMEOUT FAILS REJECT CONNECTION
        //   * ALLOW UNSECURED IF WE SUPPORT IT

        // TIMEOUT TIME SET TO 30s MAX 
        //   * SHOULDN'T TAKE MORE THAN 100-500ms 
        //   * ALLOW LEEWAY FOR SLOWER CONNECTIONS

        // ALLOW FOR 5 SECONDS PER RECEIVE

        // THIS IS UGLY 
        var Client = SocketClient;
        var Buffers = NetworkBuffer;

        CancellationTokenSource TokenSource = Token;
        var SelfPeer = Self;
        Packer = new PacketHelper(ref Client, ref SelfPeer, ref TokenSource);

        HeartBeat = new HeartBeat(ref SelfPeer);
        var heartBeat = HeartBeat;

        Guid ServerId = Guid.Empty;

        // GENERATE OUR ChaChaPoly Key
        Packer.EncryptionKeys.ChaChaKey = CryptUtils.GenerateRandomData(32);

        //[Client] Server: {Client.RemoteEndPoint} - Me: {NetworkUtils.GetLocalLanIp()}:{((IPEndPoint)Client.LocalEndPoint).Port} - ClientId: {Self.PeerId}
        Console.WriteLine($"[Client] Connected to {Client.RemoteEndPoint} - From: {NetworkUtils.GetLocalLanIp()}:{((IPEndPoint)Client.LocalEndPoint).Port} - ClientId: {Self.PeerId}");

        // GENERATE OUR LOCAL RSA KEY HERE 
        Packer.EncryptionKeys.GenerateLocalRSAKeys(RSAKeySize.Minium);

        byte[] SYNPayload = PacketSYN.GetFirstSYNPayload(Environment.MachineName, Environment.OSVersion.VersionString, DeviceType.PC, Packer.EncryptionKeys.LocalRSAKeys.PublicKey); 
        int sent = Packer.SendPacket(SYNPayload, PacketType.Control, PacketAction.SYN, PacketEncoding.NONE, PacketEncryption.NONE, PacketRoute.Direct, null);
        Console.WriteLine($"[Client] Sent [SYN] - bytesSent: {sent}");


        PacketAuthentication Auth;
        int Loop = 0;
        while (!Packer.IsAuthenticated)
        {
            if (Loop >= 6)
            {
                // AUTHENTICATION TIMEDOUT
                //Console.WriteLine("30s has passed and hasnt completed authentication");
                result.ErrorMessage = "Failed to complete authentication under 30 seconds.";
                return result;
            }

            
            if(ServerId != Guid.Empty) Console.WriteLine($"[CLIENT]:{Self.PeerId} -> waiting for authentication packets [{ServerId}]");
            else Console.WriteLine($"[CLIENT]:{Self.PeerId} -> waiting for authentication packets with server");
            try
            {
                using (var received = await Client.ReceiveFullPacketAsync(Packer))
                {
                    if (received is null) continue;

                    Span<byte> Packet = received.GetPayloadSpan();
                    PacketHeader Header = (PacketHeader)received.Header;

                    // AUTHENTICATION IS UNDER PACKET TYPE CONTROL
                    // USING ACTION AS HOW TO HANDLE IT
                    if (Header.Type != PacketType.Control) continue;

                    if (ServerId == Guid.Empty) ServerId = Header.OriginPeerId;

                    Console.WriteLine($"[Client] received authentication [{Header.Action}] packet from => [{Header.OriginPeerId}]");

                    switch (Header.Action)
                    {
                        case PacketAction.SYNACK:
                            if (Packet.IsValidJSON(out Auth))
                            {
                                if (Auth.EncryptionType != PacketEncryption.RSA) break;

                                Packer.EncryptionKeys.SetRemoteRSAKey(Auth.KeyData);

                                Auth = new PacketAuthentication()
                                {
                                    EncryptionType = PacketEncryption.ChaCha20Poly1305,
                                    KeyData = Packer.EncryptionKeys.ChaChaKey
                                };

                                Packer.SendPacket(Auth.ToJSON().ToUTF8Byte(), PacketType.Control, PacketAction.ACK, PacketEncoding.NONE, PacketEncryption.RSA, PacketRoute.Direct, null);

                                Console.WriteLine($"[Client] sent encrypted ChaChaKey using RSA");
                            }
                            break;
                        case PacketAction.ACK:
                            if (Packet.IsValidJSON(out PacketEncrypted encrypted))
                            {
                                if (encrypted.EncryptionType != PacketEncryption.ChaCha20Poly1305) break;

                                if (encrypted.TryDecryptInto(Packer.EncryptionKeys.ChaChaKey, out Auth) && Auth.KeyData.ToHashString() == Packer.EncryptionKeys.ChaChaKey.ToHashString())
                                {
                                    Packer.SendPacket([], PacketType.Control, PacketAction.READY, PacketEncoding.NONE, PacketEncryption.NONE, PacketRoute.Direct, null);
                                }
                            }
                            break;
                        case PacketAction.READY:
                            Console.WriteLine($"[Client]:{Self.PeerId} [Ready] Connection authenticated with [Server]:{Header.OriginPeerId}");
                            Packer.IsAuthenticated = true;
                            Packer.IsAuthenticating = false;
                            result.IsSuccess = true;
                            return result;
                    }
                }
            }
            catch { }
            //catch(Exception Ex)
            //{
            //    Console.WriteLine(Ex.Message); 
            //    //result.ErrorMessage = Ex.Message;
            //    //return result;
            //}


            Loop++;

            
        }

        Console.WriteLine(Packer.IsAuthenticated);
        Console.WriteLine("no longer authenticating");
        return result;
    }

    public async Task<bool> TryConnectAsyncV2(string IP, int Port)
    {
        Console.WriteLine("c start");
        Console.WriteLine($"[CLIENT] {Self.PeerId} attempting to connect to {IP}:{Port}");

        if (SocketClient is null)
        {
            SocketClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            Token = new CancellationTokenSource();

            NetworkBuffer = new NetworkBuffer();

            OnConnectedAsync += HandleConnectionAsync;

            OnAuthenticationRequestedAsync += HandleAuthenticationAsync;

            OnDataReceived += HandleOnDataReceived;
        }

        // Parse IP to IPAddress
        if (!IPAddress.TryParse(IP, out IPAddress? _IPAddress)) return false;

        // Try to connect to server here
        this.EndPoint = new IPEndPoint(_IPAddress, Port);

        await SocketClient.ConnectAsync(EndPoint);

        // CREATE STOPWATCH TO VIEW AUTHENTICATION SPEED

        SecureChannelResult result = default;

        try
        {
            var stopwatch = Stopwatch.StartNew();

            // BLOCK UNTIL AUTHENTICATED THEN RETURN AND START THE CLIENT THREAD
            result = await EstablishSecureChannelAsync();

            stopwatch.Stop();

            Console.WriteLine($"[CLIENT] Authentication took: {stopwatch.ElapsedMilliseconds}ms");

            // START THE CLIENT THREAD
            if (result.IsSuccess)
            {
                // RUN THIS ASYNC, BUT DONT AWAIT SO WE CAN RETURN WHEN WE ARE AUTHENTICATED!
                _ = OnConnectedAsync?.Invoke();
            }

            Console.WriteLine($"client try connect has ended with this result -> result.IsSuccess");
        }
        catch(Exception Ex) { Console.WriteLine(Ex.ToString()); }


        return result.IsSuccess;
    }

    public async Task HandleConnectionAsync()
    {
        string ClientEndPoint = SocketClient.RemoteEndPoint.ToString();
        //Console.WriteLine($"[CLIENT] Connected to server [{ClientEndPoint}]");

        var Client = SocketClient;
        var Buffers = NetworkBuffer;
        // Need this to pass references to things that might still trying to run after disconnect - probably pass into helper
        CancellationTokenSource TokenSource = Token;
        var SelfPeer = Self;
        Packer = new PacketHelper(ref Client, ref SelfPeer, ref TokenSource); //, 

        HeartBeat = new HeartBeat(ref SelfPeer);
        var heartBeat = HeartBeat;
        bool VoiceStarted = false;


        //Console.WriteLine($"[Client] I connected to {Client.RemoteEndPoint} but this is my ID [{Self.PeerId}]:{NetworkUtils.GetLocalLanIp()}:{((IPEndPoint)Client.LocalEndPoint).Port}");
        Console.WriteLine($"[Client] Server: {Client.RemoteEndPoint} - Me: {NetworkUtils.GetLocalLanIp()}:{((IPEndPoint)Client.LocalEndPoint).Port} - ClientId: {Self.PeerId}");

        var _Packer = Packer;
        while (!Token.IsCancellationRequested)
        {
            await Task.Delay(5, Token.Token);

            //Console.WriteLine("reading messages");
            _ = ReadMessageAsync(_Packer);

            // SEND HEARTBEAT AND HANDLE TIMEOUT
            while (!Token.IsCancellationRequested || Client.IsSocketConnected())
            {
                await Task.Delay(5, Token.Token);

                if (!Packer.IsAuthenticated) continue;

                if (heartBeat.TrySendHeartBeat(ref _Packer, out bool IsDisconnected)! && IsDisconnected && !heartBeat.FirstBeat)
                {
                    Token.Cancel();

                    if (Self.OperationMode == PeerState.Peer)
                    {

                        var PeerInfo = Self.FindPeerById(Self.PeerId);

                        if (PeerInfo is not null)
                        {
                            Self.TCPServer.Clients.Remove(PeerInfo.Value.Item1);
                            Self.ConnectedPeers.Remove(PeerInfo.Value.Item2);

                            // Announce to all other clients that peer disconnected - ENCRYPTION WILL NEED CHANGED FROM NON TO SOME AUTOMATIC FORM LATER
                            Self.ConnectedPeers.ForEach(Peer => Peer.PacketHelper.SendPacket(PeerInfo.Value.Item2.ToJSON().ToUTF8Byte(), PacketType.Peer, PacketAction.Leave, PacketEncoding.NONE, PacketEncryption.NONE, PacketRoute.Broadcast, null));
                        }
                    }
                    else Self.TCPServer.Clients.Remove(Self.TCPServer.Clients.Find(x => x.Connection == Client));

                    Console.WriteLine("[Client] Timed out");
                    return;
                }
            }
        }
    }

    

    public void HandleConnected()
    {
        string ClientEndPoint = SocketClient.RemoteEndPoint.ToString();
        //Console.WriteLine($"[CLIENT] Connected to server [{ClientEndPoint}]");

        var Client = SocketClient;
        var Buffers = NetworkBuffer;
        // Need this to pass references to things that might still trying to run after disconnect - probably pass into helper
        CancellationTokenSource TokenSource = Token;
        var SelfPeer = Self;
        Packer = new PacketHelper(ref Client, ref SelfPeer, ref TokenSource); //, 

        HeartBeat = new HeartBeat(ref SelfPeer);
        var heartBeat = HeartBeat;
        bool VoiceStarted = false;


        //Console.WriteLine($"[Client] I connected to {Client.RemoteEndPoint} but this is my ID [{Self.PeerId}]:{NetworkUtils.GetLocalLanIp()}:{((IPEndPoint)Client.LocalEndPoint).Port}");
        Console.WriteLine($"[Client] Server: {Client.RemoteEndPoint} - Me: {NetworkUtils.GetLocalLanIp()}:{((IPEndPoint)Client.LocalEndPoint).Port} - ClientId: {Self.PeerId}");


        //return;
        Task.Run(() =>
        {
            var _Packer = Packer;

            while (!Token.IsCancellationRequested)
            {
                Thread.Sleep(5);


                // Continue until authentication is complete - 
                if (Packer.IsAuthenticating) continue;
                if (!Packer.IsAuthenticated)
                {
                    Packer.IsAuthenticating = true;
                    OnAuthenticationRequested.Invoke(Client, _Packer);
                }

                // Everything past here needs to be authenticated with ChaCha

                //Check for ping every so often


                if (heartBeat.TrySendHeartBeat(ref _Packer, out bool IsDisconnected)! && IsDisconnected && !heartBeat.FirstBeat)
                {
                    Token.Cancel();

                    if (Self.OperationMode == PeerState.Peer)
                    {

                        var PeerInfo = Self.FindPeerById(Self.PeerId);

                        if (PeerInfo is not null)
                        {
                            Self.TCPServer.Clients.Remove(PeerInfo.Value.Item1);
                            Self.ConnectedPeers.Remove(PeerInfo.Value.Item2);

                            // Announce to all other clients that peer disconnected - ENCRYPTION WILL NEED CHANGED FROM NON TO SOME AUTOMATIC FORM LATER
                            Self.ConnectedPeers.ForEach(Peer => Peer.PacketHelper.SendPacket(PeerInfo.Value.Item2.ToJSON().ToUTF8Byte(), PacketType.Peer, PacketAction.Leave, PacketEncoding.NONE, PacketEncryption.NONE, PacketRoute.Broadcast, null));
                        }
                    }
                    else Self.TCPServer.Clients.Remove(Self.TCPServer.Clients.Find(x => x.Connection == Client));

                    Console.WriteLine("[Client] Timed out");
                    return;
                }

                // V3
                //var received = Client.ReceiveFullPacketAsync(Packer).GetAwaiter().GetResult();
                //if (received is null) continue;
                //Span<byte> Packet = received.GetPayloadSpan();
                //var Header = (PacketHeader)received.Header;

                // V1
                //return;
                //byte[] Packet = Client.ReceivePacket(ref _Packer, out PacketHeader Header);

                // V2
                var received = Client.ReceivedPacket(ref _Packer, out PacketHeader Header);
                if (received is null) continue;
                Span<byte> Packet = received.GetPayloadSpan();

                OnPacketReceived?.Invoke(_Packer, received);

                if (Header.Type != PacketType.NONE)
                {
                    //Console.WriteLine($"[Client] [{Header.PacketAction}]: {Packet.ToUTF8String()}");


                    if (Header.Encryption != PacketEncryption.NONE && Packet.IsValidJSON(out PacketEncrypted encrypted) && encrypted.TryDecrypt(Packer, out byte[] Decrypted))
                    {
                        Packet = Decrypted;
                        //Console.WriteLine($"[Client] [{Header.PacketAction}] [Auto-Decrypted]: {Decrypted.ToUTF8String()}");
                        //Console.WriteLine($"[Client] [{Header.PacketAction}]: {Packet.ToUTF8String()}");
                        OnDataReceived.Invoke(Packer, Header, Packet);
                    }
                    else if (Header.Encryption == PacketEncryption.NONE) {  OnDataReceived.Invoke(Packer, Header, Packet); }


                }

            }
        });
    }

    public async Task HandleAuthenticationAsync(PacketHelper Packer, ReceivedPacket<IPacketHeaderIdentifier> ReceivedPacket)
    {
        // Generate ChaChaKey here as we will be needing it anyway
        if (Packer.EncryptionKeys.ChaChaKey is null || Packer.EncryptionKeys.ChaChaKey.Length < 32) Packer.EncryptionKeys.ChaChaKey = CryptUtils.GenerateRandomData(32);

        Span<byte> Packet = ReceivedPacket.GetPayloadSpan();
        PacketHeader Header = (PacketHeader)ReceivedPacket.Header;

        //Console.WriteLine(Header.ToJSON(new System.Text.Json.JsonSerializerOptions() { WriteIndented = true }));

        if (Header.Type != PacketType.NONE)
        {
            switch (Header.Action)
            {
                case PacketAction.SYNACK: // SYNAck sent from server - send Ack to client
                    Console.WriteLine($"[Client] received [SYNAck]");

                    if (Packet.IsValidJSON(out PacketAuthentication Auth))
                    {
                        // Extract PubRSAKey and then create one for the client based on the servers KeySize
                        Packer.EncryptionKeys.SetRemoteRSAKey(Auth.KeyData);

                        int RemoteKeySize = RSACrypt.GetPKCS8KeySize(Auth.KeyData, false);
                        Packer.EncryptionKeys.UpdateLocalRSAKeys(RemoteKeySize, RSACrypt.CreateExport(RemoteKeySize));

                        // Send server the clients PubRSAKey
                        //Auth = new PacketAuthentication()
                        //{
                        //    EncryptionType =.Encryption.RSA,
                        //    KeyData = Packer.EncryptionKeys.LocalRSAKeys.PublicKey
                        //};
                        //Packer.SendUTF8Packet(Auth.ToJSON(), PacketAction.SYNAck, false);

                        //// Send back ChaChaKey encrypted with RSA
                        Auth = new PacketAuthentication()
                        {
                            EncryptionType = PacketEncryption.ChaCha20Poly1305,
                            KeyData = Packer.EncryptionKeys.ChaChaKey
                        };

                        ////Packer.SendUTF8Packet(Auth.ToJSON(), PacketAction.ACK);
                        //Packer.SendEncryptedPacket(Auth.ToJSON().ToUTF8Byte(), PacketEncryption.RSA, PacketAction.ACK, false);
                        Packer.SendPacket(Auth.ToJSON().ToUTF8Byte(), PacketType.Control, PacketAction.ACK, PacketEncoding.NONE, PacketEncryption.RSA, PacketRoute.Direct, null);

                        Console.WriteLine($"[Client] sent encrypted ChaChaKey using RSA");
                    }
                    else Console.WriteLine($"[Client] invalid Auth Packet");

                    //Packer.SendUTF8Packet("", PacketAction.ACK);
                    break;
                case PacketAction.ACK:
                    // server sends client ack, will be encrypted with rsa containing the pubkeys hash (data doesnt matter so its ignored), we then send the ChaChaKey
                    //Console.WriteLine($"[Client] received [ACK] from server");

                    if (Packet.IsValidJSON(out PacketEncrypted encrypted))
                    {
                        //Console.WriteLine("encrypted packet detected");
                        if (encrypted.TryDecryptInto(Packer.EncryptionKeys.ChaChaKey, out Auth) && Auth.KeyData.ToHashString() == Packer.EncryptionKeys.ChaChaKey.ToHashString())
                        {
                            //Console.WriteLine("[client] hash the same");
                            //Packer.SendUTF8Packet("<READY>", PacketAction.READY, false);
                            Packer.SendPacket("<READY>".ToUTF8Byte(), PacketType.Control, PacketAction.READY, PacketEncoding.NONE, PacketEncryption.NONE, PacketRoute.Direct, null);
                        }
                        else Console.WriteLine($"[Client] [ACK] failed to decrypt encrypted ChaChaKey");
                    }


                    break;
                case PacketAction.READY:
                    Packer.IsAuthenticated = true;
                    Packer.IsAuthenticating = false;

                    Console.WriteLine($"[Client] [Ready] Connection authenticated with Server [{Header.OriginPeerId}]");
                    break;
            }
        }
    }

    public void HandleAuthentication(Socket Client, PacketHelper Packer)
    {
        //Console.WriteLine("[Client] handling authentication");

        // Generate ChaChaKey here as we will be needing it anyway
        Packer.EncryptionKeys.ChaChaKey = CryptUtils.GenerateRandomData(32);
        //Console.WriteLine("[Client] ChaChaKey generated");


        // Sends DiscoveredPeers if its existing, but if not sends ConnectedPeers

        //List<PeerTable> DiscoveredPeers = new List<PeerTable>();
        //if (Self.DiscoveredPeers is not null && Self.DiscoveredPeers.Count() > 0) { DiscoveredPeers = Self.DiscoveredPeers.ToList(); }
        //else DiscoveredPeers = Self.ConnectedPeers;
        //Console.WriteLine("Set discovered peers");

        //PeerSYN SYN = new PeerSYN(Self.TCPServer.MyPeerTable, DiscoveredPeers);

        int sent = Packer.SendPacket($"{Self.PeerId.ToJSON()}".ToUTF8Byte(), PacketType.Control, PacketAction.SYN, PacketEncoding.NONE, PacketEncryption.NONE, PacketRoute.Direct, null);
        Console.WriteLine($"[Client] Sent [SYN] - bytesSent: {sent}");

        // Stay here until we are authenticated
        while (!Packer.IsAuthenticated)
        {
            //byte[] Packet = Client.ReceivePacket(ref Packer, out PacketHeader Header);

            //_ = Client.ReceiveFullPacketAsync();
            //var received = Client.ReceiveFullPacketAsync(Packer).GetAwaiter().GetResult();
            //if (received is null) continue;
            //Span<byte> Packet = received.GetPayloadSpan();
            //var Header = (PacketHeader)received.Header;

            using var received = Client.ReceivedPacket(ref Packer, out PacketHeader Header);
            if (received is null) continue;
            Span<byte> Packet = received.GetPayloadSpan();

            if (Header.Type != PacketType.NONE)
            {
                switch (Header.Action)
                {
                    case PacketAction.SYNACK: // SYNAck sent from server - send Ack to client
                        Console.WriteLine($"[Client] received [SYNAck]");
                        
                        if(Packet.IsValidJSON(out PacketAuthentication Auth))
                        {
                            // Extract PubRSAKey and then create one for the client based on the servers KeySize
                            Packer.EncryptionKeys.SetRemoteRSAKey(Auth.KeyData);

                            int RemoteKeySize = RSACrypt.GetPKCS8KeySize(Auth.KeyData, false);
                            Packer.EncryptionKeys.UpdateLocalRSAKeys(RemoteKeySize, RSACrypt.CreateExport(RemoteKeySize));

                            // Send server the clients PubRSAKey
                            //Auth = new PacketAuthentication()
                            //{
                            //    EncryptionType =.Encryption.RSA,
                            //    KeyData = Packer.EncryptionKeys.LocalRSAKeys.PublicKey
                            //};
                            //Packer.SendUTF8Packet(Auth.ToJSON(), PacketAction.SYNAck, false);

                            //// Send back ChaChaKey encrypted with RSA
                            Auth = new PacketAuthentication()
                            {
                                EncryptionType = PacketEncryption.ChaCha20Poly1305,
                                KeyData = Packer.EncryptionKeys.ChaChaKey
                            };

                            ////Packer.SendUTF8Packet(Auth.ToJSON(), PacketAction.ACK);
                            //Packer.SendEncryptedPacket(Auth.ToJSON().ToUTF8Byte(), PacketEncryption.RSA, PacketAction.ACK, false);
                            Packer.SendPacket(Auth.ToJSON().ToUTF8Byte(), PacketType.Control, PacketAction.ACK, PacketEncoding.NONE, PacketEncryption.RSA, PacketRoute.Direct, null);

                            Console.WriteLine($"[Client] sent encrypted ChaChaKey using RSA");
                        }
                        else Console.WriteLine($"[Client] invalid Auth Packet");

                        //Packer.SendUTF8Packet("", PacketAction.ACK);
                        break;
                    case PacketAction.ACK:
                        // server sends client ack, will be encrypted with rsa containing the pubkeys hash (data doesnt matter so its ignored), we then send the ChaChaKey
                        //Console.WriteLine($"[Client] received [ACK] from server");

                        if(Packet.IsValidJSON(out PacketEncrypted encrypted))
                        {
                            //Console.WriteLine("encrypted packet detected");
                            if (encrypted.TryDecryptInto(Packer.EncryptionKeys.ChaChaKey, out Auth) && Auth.KeyData.ToHashString() == Packer.EncryptionKeys.ChaChaKey.ToHashString())
                            {
                                //Console.WriteLine("[client] hash the same");
                                //Packer.SendUTF8Packet("<READY>", PacketAction.READY, false);
                                Packer.SendPacket("<READY>".ToUTF8Byte(), PacketType.Control, PacketAction.READY, PacketEncoding.NONE, PacketEncryption.NONE, PacketRoute.Direct, null);
                            }
                            else Console.WriteLine($"[Client] [ACK] failed to decrypt encrypted ChaChaKey");
                        }


                        break;
                    case PacketAction.READY:
                        Packer.IsAuthenticated = true;
                        Packer.IsAuthenticating = false;
                        Console.WriteLine($"[Client] [Ready] Connection authenticated with Server");
                        break;
                }
            }
        }

    }


    public void HandleAction(PacketHeader Header, ReadOnlyMemory<byte> Data, PacketHelper Helper)
    {
        //string json = Header.ToPacketHeaderV2().ToJSON(new System.Text.Json.JsonSerializerOptions() { WriteIndented = true });
        //Console.WriteLine(json);

        byte[] DATA = Array.Empty<byte>();
        string UTF8 = string.Empty;

        switch (Header.Type)
        {
            case PacketType.Control:
                switch (Header.Action)
                {
                    case PacketAction.Ping: HeartBeat.HandleHeartBeatActions(Header, Data, Helper); break;
                    case PacketAction.Pong: HeartBeat.HandleHeartBeatActions(Header, Data, Helper); break;
                }
                break;
            case PacketType.Peer:
                switch (Header.Action)
                {
                    case PacketAction.Join:

                        Console.WriteLine("[TCPClient] peer joined");

                        // Check if in packet init class
                        if (UTF8.IsValidJSON(out PeerTable initPeer)) Self.AddPeer(Helper.ClientHandle, initPeer);
                        else if (UTF8.IsValidJSON(out IEnumerable<PeerTable> initPeers)) Self.AddPeers(Helper.ClientHandle, initPeers);
                        break;
                }
                
                break;
        }

        //switch (Header.Action)
        //{
        //    case PacketAction.Ping: HeartBeat.HandleHeartBeatActions(Header, Data, Helper); break;
        //    case PacketAction.Pong: HeartBeat.HandleHeartBeatActions(Header, Data, Helper); break;
        //    case PacketAction.Data:
        //        //OnDataReceived.Invoke(Data.Span);
        //        break;
        //    case PacketAction.PeerJoin:
        //        Console.WriteLine($"[Client] [{Helper.Self.PeerId}] received PeerJoin");
        //        DATA = Data.ToArray();
        //        UTF8 = DATA.ToUTF8String();

        //        //Console.WriteLine("peer joined");

        //        // Check if in packet init class
        //        if (UTF8.IsValidJSON(out PeerTable initPeer)) Self.AddPeer(Helper.ClientHandle, initPeer);
        //        else if (UTF8.IsValidJSON(out IEnumerable<PeerTable> initPeers)) Self.AddPeers(Helper.ClientHandle, initPeers);
        //        break;
        //    //case 
        //    //default:
        //    //    OnDataReceived.Invoke(Data.Span);
        //    //    break;
        //}
    }

    public void HandleOnDataReceived(PacketHelper Packer, PacketHeader Header, ReadOnlySpan<byte> Data)
    {
        byte[] DATA = Data.ToArray();

        if (DATA.Length == 0) return;
        // Print the message that was received from the client
        //Console.WriteLine($"Client Received => [{Header.PacketAction}] EncryptionType: [{Header.Encryption}] \"{DATA.ToUTF8String()}\""); 
        HandleAction(Header, DATA, Packer);
    }
}
