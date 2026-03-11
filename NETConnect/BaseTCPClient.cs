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
using System.Threading.Tasks;

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
    public event Action OnDisconnected;

    public event Action<Socket, PacketHelper> OnAuthenticationRequested;

    public event Action<PacketHelper, PacketHeader, ReadOnlySpan<byte>> OnDataReceived;


    public bool IsAuthenticating = false;
    public bool IsAuthenticated = false;




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
                return true;
            }
            catch (Exception Ex) { Console.WriteLine(Ex.ToString()); Debug.WriteLine($"TryConnect Exception: {Ex.ToString()}"); }
            
            // Failed to parse IPAddress
            return false;
        }

        // Returns false if client didnt connect properly
        return false;
    }

    public bool TLSV2(Socket Server, bool IsAuthorized, PacketHelper Packer)
    {
        // Might need to rework this to where it doesnt return anything if its not valid (FIX: set default data length to -1 to symbolize not valid data)
        byte[] Packet = Server.ReceivePacket(out PacketHeader Header);

       

        // Ignore packet if nothing was read
        if (Header.PacketAction != PacketActionType.Empty)
        {
            //Console.WriteLine($"[Client] [{Header.PacketAction}] [{Header.ToJSON()}]");
            string JSON = string.Empty;
            PacketAuthentication Auth;


            // Handle only Authorization related requests (and ping just because there is no system for it globally) - (FIX: automatically handle it while we are reading packet data)

            switch (Header.PacketAction)
            {
                case PacketActionType.SYN:

                    //Console.WriteLine("[Client] received [SYN] froms server");
                    JSON = Packet.ToUTF8String();
                    // Receives PacketAuthorization from server containing which data it is to be set as

                    if (JSON.IsValidJSON(out Auth))
                    {
                        //Console.WriteLine($"[Client] Valid AuthPacket [SYN]\n{JSON}");
                        switch (Auth.EncryptionType)
                        {
                            case PacketEncryptionType.RSA: // Sets remote RSAPubKey to start other auths

                                Packer.EncryptionKeys.SetRemoteRSAKey(Auth.KeyData);

                                // Generate our keys for RSA based on the servers key
                                int RemoteKeySize = RSACrypt.GetPKCS8KeySize(Auth.KeyData, false);
                                Packer.EncryptionKeys.UpdateLocalRSAKeys(RemoteKeySize, RSACrypt.CreateExport(RemoteKeySize));

                                // Sends server back our RSAPubKey
                                Auth = new PacketAuthentication()
                                {
                                    EncryptionType = PacketEncryptionType.RSA,
                                    KeyData = Packer.EncryptionKeys.LocalRSAKeys.PublicKey
                                };

                                JSON = Auth.ToJSON();

                                Packer.SendUTF8Packet(JSON, PacketActionType.SYNAck); 
                                //Console.WriteLine($"[Client] Sent (AuthPacket-RSAPubkey)SYNAck to Server");
                                break;
                        } 
                    }
                    break;
                case PacketActionType.SYNAck:
                    //Console.WriteLine($"[Client] received [SYNAck] - EncryptionType: {Header.PacketEncryptionType}");
                    JSON = Packet.ToUTF8String();

                    //Console.WriteLine(JSON);
                    if (JSON.IsValidJSON(out PacketEncrypted Encrypted))
                    {
                        //Console.WriteLine("[Client] [SYNAck] valid PacketEncrypted");
                        //Console.WriteLine(Encrypted.ToJSON());
                        // Attempt decryption based on stored keys
                        // Then pack the key we actually want to transport - ChaCha 
                        switch (Encrypted.EncryptionType)
                        {
                            case PacketEncryptionType.RSA:
                                // After we confirm decryption, we can send the key we want to share directly to ACK

                                // seems our 
                                byte[] Decrypted = Encrypted.encryptedData.DecryptRSA(Packer.EncryptionKeys.LocalRSAKeys.PrivateKey);
                                if(Decrypted.ToUTF8String().IsValidJSON(out bool IsEncrypted))
                                {
                                    //Console.WriteLine($"IsEncrypted: {IsEncrypted}");

                                    Packer.EncryptionKeys.ChaChaKey = CryptUtils.GenerateRandomData(32);
                                    //Console.WriteLine($"[Client] ChaChaKey I generated {Packer.EncryptionKeys.ChaChaKey.ToJSON()}");

                                    Auth = new PacketAuthentication()
                                    {
                                        EncryptionType = PacketEncryptionType.ChaCha20Poly1305,
                                        KeyData = Packer.EncryptionKeys.ChaChaKey
                                    };

                                    //Console.WriteLine(Auth.ToJSON());


                                    // Send ChaCha encrypted with RSA
                                    if(PacketEncrypted.TryEncryptSend(Auth.ToJSON().ToUTF8Byte(), PacketEncryptionType.RSA, PacketActionType.ACK, Packer))
                                    {
                                        //Console.WriteLine("[Client] [SYNAck] sent RSAEncrypted ChaCha key to [Ack]");
                                    }

                                }
                                // Report back to the person who sent this and try to reexchange keys 
                                else
                                {

                                }
                                break;
                        }
                    }
                    //else Console.WriteLine("[Client] [SYNAck] invalid PacketEncrypted");
                        break;
                case PacketActionType.ACK:
                    //Console.WriteLine($"[Client] [ACK] [{Header.PacketEncryptionType}]");

                    PacketEncrypted encrypted = Packet.FromUTF8IntoJSON<PacketEncrypted>();

                    switch (Header.PacketEncryptionType)
                    {
                        case PacketEncryptionType.RSA:
                            break;

                        case PacketEncryptionType.ChaCha20Poly1305:


                            //Console.WriteLine($"[Client] ChaChaKey {Packer.EncryptionKeys.ChaChaKey.ToHashString()}");

                            if(encrypted.TryDecryptInto(Packer.EncryptionKeys.ChaChaKey, out Auth))
                            {
                                //Console.WriteLine("valid auth packet");
                                //Console.WriteLine($"[Client] ChaChaKey from server {Auth.KeyData.ToHashString()}");

                                // Compare Keys, if same authenticate
                                if (Packer.EncryptionKeys.ChaChaKey.ToHashString() == Auth.KeyData.ToHashString())
                                {
                                    Console.WriteLine("[Client] has authenticated the connection");
                                    IsAuthorized = true;
                                }
                            }
                           // else Console.WriteLine("[Client] failed to decrypt into AuthPacket");
                                break;
                    }

                    //encrypted.TryDecryptInto<PacketAuthentication>()
                    break;

            }
        }

        return IsAuthorized;
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

        Task.Run(() =>
        {
            var _Packer = Packer;

            while (!Token.IsCancellationRequested)
            {
                Thread.Sleep(5);

                // Continue until authentication is complete
                if (IsAuthenticating) continue;
                if (!IsAuthenticated)
                {
                    IsAuthenticating = true;
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

                            // Announce to all other clients that peer disconnected
                            Self.ConnectedPeers.ForEach(Peer => Peer.PacketHelper.SendPacket(PeerInfo.Value.Item2.ToJSON().ToUTF8Byte(), PacketActionType.PeerLeave));
                        }
                    }


                    Console.WriteLine("[Client] Timed out");
                    return;
                }


                byte[] Packet = Client.ReceivePacket(out PacketHeader Header);
                if (Header.PacketAction != PacketActionType.Empty)
                {
                    //Console.WriteLine($"[Client] [{Header.PacketAction}]: {Packet.ToUTF8String()}");


                    if (Header.PacketEncryptionType != PacketEncryptionType.NONE && Packet.IsValidJSON(out PacketEncrypted encrypted) && encrypted.TryDecrypt(Packer, out byte[] Decrypted))
                    {
                        Packet = Decrypted;
                        //Console.WriteLine($"[Client] [{Header.PacketAction}] [Auto-Decrypted]: {Decrypted.ToUTF8String()}");
                        //Console.WriteLine($"[Client] [{Header.PacketAction}]: {Packet.ToUTF8String()}");
                        OnDataReceived.Invoke(Packer, Header, Packet);
                    }
                    else if (Header.PacketEncryptionType == PacketEncryptionType.NONE) {  OnDataReceived.Invoke(Packer, Header, Packet); }


                }
                // Search for valid packet, if valid packet not found, skips
                //byte[] Packet = Client.ReceiveValidatedPacket(ref heartBeat, ref _Packer, out PacketHeader Header);
                //if (Header.PacketAction != PacketActionType.Empty) continue;








                //Console.WriteLine($"[Client] {Self.Settings.ToJSON()} {Self.Settings.IsEncryptionEnabled}");





                #region OLD
                //if (!IsAuthenticated) { IsAuthenticated = TLSV2(Client, IsAuthenticated, Packer); heartBeat.SetLastBeat(); }
                //else
                //{
                //    Console.WriteLine($"[Client] IsAuthenticated: {IsAuthenticated}");

                //    byte[] Packet = Client.ReceiveValidatedPacket(ref heartBeat, ref _Packer, out PacketHeader Header);
                //    //byte[] Packet = SocketClient.ReceivePacket(ref heartBeat, out PacketHeader Header);
                //    if (Header.PacketAction != PacketActionType.Empty)
                //    {
                //        // Check for ping every so often
                //        if (!heartBeat.TrySendHeartBeat(ref Helper, out bool IsDisconnected) && IsDisconnected)
                //        {
                //            Token.Cancel();

                //            if (Self.OperationMode == PeerState.Peer)
                //            {

                //                var PeerInfo = Self.FindPeerById(Self.PeerId);

                //                if (PeerInfo is not null)
                //                {
                //                    Self.TCPServer.Clients.Remove(PeerInfo.Value.Item1);
                //                    Self.ConnectedPeers.Remove(PeerInfo.Value.Item2);

                //                    // Announce to all other clients that peer disconnected
                //                    Self.ConnectedPeers.ForEach(Peer => Peer.PacketHelper.SendPacket(PeerInfo.Value.Item2.ToJSON().ToUTF8Byte(), PacketActionType.PeerLeave));
                //                }
                //            }


                //            Console.WriteLine("[Client] Timed out");
                //            return;
                //        }

                //        // This works but we'll mess with that later after auth
                //        if (!VoiceStarted) { Audio.Audio.StartStreaming(ref Helper); VoiceStarted = true; }
                //        else if (VoiceStarted && Token.IsCancellationRequested) { Audio.Audio.StopStreaming(); }

                //        // Complete normal auth then share your peer list

                //        // receive normal messages after auth
                //        ///byte[] Packet = SocketClient.ReceivePacket(ref heartBeat, out PacketHeader Header);
                //        if (Header.PacketAction != PacketActionType.Empty)
                //        {
                //            if (Header.PacketEncryptionType != PacketEncryptionType.NONE)
                //            {
                //                Console.WriteLine("Encrypted packet detected, autodecrypting");

                //                PacketEncrypted encrypted = Packet.FromUTF8IntoJSON<PacketEncrypted>();
                //                switch (encrypted.EncryptionType)
                //                {
                //                    case PacketEncryptionType.RSA:
                //                        Packet = encrypted.Decrypt(Packer.EncryptionKeys.RemoteRSAPubKey);
                //                        break;
                //                    case PacketEncryptionType.ChaCha20Poly1305:
                //                        Packet = encrypted.Decrypt(Packer.EncryptionKeys.ChaChaKey);
                //                        break;
                //                }
                //            }


                //            if (Header.PacketAction != PacketActionType.Ping && Header.PacketAction != PacketActionType.Pong)
                //            {
                //                //Console.WriteLine($"[ClientPacketData] {Header.ByteLength} {Header.PacketAction.ToString()} {Header.SentAt}");
                //            }



                //            HandleAction(Header, Packet, Packer);
                //        }
                //    }
                //}
                #endregion

            }
        });
    }

    public void HandleAuthentication(Socket Client, PacketHelper Packer)
    {
        Console.WriteLine("[Client] handling authentication");

        // Generate ChaChaKey here as we will be needing it anyway
        Packer.EncryptionKeys.ChaChaKey = CryptUtils.GenerateRandomData(32);
        Console.WriteLine("[Client] ChaChaKey generated");


        // Sends DiscoveredPeers if its existing, but if not sends ConnectedPeers

        //List<PeerTable> DiscoveredPeers = new List<PeerTable>();
        //if (Self.DiscoveredPeers is not null && Self.DiscoveredPeers.Count() > 0) { DiscoveredPeers = Self.DiscoveredPeers.ToList(); }
        //else DiscoveredPeers = Self.ConnectedPeers;
        //Console.WriteLine("Set discovered peers");

        //PeerSYN SYN = new PeerSYN(Self.TCPServer.MyPeerTable, DiscoveredPeers);

        Packer.SendUTF8Packet($"{Self.PeerId.ToJSON()}", PacketActionType.SYN, false);
        Console.WriteLine("[Client] Sent [SYN]");

        // Stay here until we are authenticated
        while (!IsAuthenticated)
        {
            byte[] Packet = Client.ReceivePacket(out PacketHeader Header);
            if (Header.PacketAction != PacketActionType.Empty)
            {
                switch (Header.PacketAction)
                {
                    case PacketActionType.SYNAck: // SYNAck sent from server - send Ack to client
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
                            //    EncryptionType = PacketEncryptionType.RSA,
                            //    KeyData = Packer.EncryptionKeys.LocalRSAKeys.PublicKey
                            //};
                            //Packer.SendUTF8Packet(Auth.ToJSON(), PacketActionType.SYNAck, false);

                            //// Send back ChaChaKey encrypted with RSA
                            Auth = new PacketAuthentication()
                            {
                                EncryptionType = PacketEncryptionType.ChaCha20Poly1305,
                                KeyData = Packer.EncryptionKeys.ChaChaKey
                            };

                            //Packer.SendUTF8Packet(Auth.ToJSON(), PacketActionType.ACK);
                            Packer.SendEncryptedPacket(Auth.ToJSON().ToUTF8Byte(), PacketEncryptionType.RSA, PacketActionType.ACK, false);
                            Console.WriteLine($"[Client] sent encrypted ChaChaKey using RSA");
                        }
                        else Console.WriteLine($"[Client] invalid Auth Packet");

                        //Packer.SendUTF8Packet("", PacketActionType.ACK);
                        break;
                    case PacketActionType.ACK:
                        // server sends client ack, will be encrypted with rsa containing the pubkeys hash (data doesnt matter so its ignored), we then send the ChaChaKey
                        Console.WriteLine($"[Client] received [ACK] from server");

                        if(Packet.IsValidJSON(out PacketEncrypted encrypted))
                        {
                            Console.WriteLine("encrypted packet detected");
                            if (encrypted.TryDecryptInto(Packer.EncryptionKeys.ChaChaKey, out Auth) && Auth.KeyData.ToHashString() == Packer.EncryptionKeys.ChaChaKey.ToHashString())
                            { 
                                Console.WriteLine("[client] hash the same");
                                Packer.SendUTF8Packet("<READY>", PacketActionType.Ready, false);
                            }
                            else Console.WriteLine($"[Client] [ACK] failed to decrypt encrypted ChaChaKey");
                        }


                        break;
                    case PacketActionType.Ready:
                        IsAuthenticated = true;
                        IsAuthenticating = false;
                        Console.WriteLine($"[Client] [Ready] Connection authenticated with Server");
                        break;
                }
            }
        }

    }


    public  void HandleAction(PacketHeader Header, ReadOnlyMemory<byte> Data, PacketHelper Helper)
    {

        switch (Header.PacketAction)
        {
            case PacketActionType.Ping: HeartBeat.HandleHeartBeatActions(Header, Data, Helper); break;
            case PacketActionType.Pong: HeartBeat.HandleHeartBeatActions(Header, Data, Helper); break;
            case PacketActionType.Data:
                //OnDataReceived.Invoke(Data.Span);
                break;
            //default:
            //    OnDataReceived.Invoke(Data.Span);
            //    break;
        }
    }

    public void HandleOnDataReceived(PacketHelper Packer, PacketHeader Header, ReadOnlySpan<byte> Data)
    {
        byte[] DATA = Data.ToArray();

        if (DATA.Length == 0) return;
        // Print the message that was received from the client
        Console.WriteLine($"Client Received => [{Header.PacketAction}] EncryptionType: [{Header.PacketEncryptionType}] \"{DATA.ToUTF8String()}\""); 
        HandleAction(Header, DATA, Packer);
    }
}
