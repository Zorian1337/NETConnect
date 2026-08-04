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
    public event Action OnDisconnected;

    public event Action<Socket, PacketHelper> OnAuthenticationRequested;

    public event Action<PacketHelper, PacketHeader, ReadOnlySpan<byte>> OnDataReceived;

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
                do { Thread.Sleep(100); }
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

                            // Announce to all other clients that peer disconnected
                            Self.ConnectedPeers.ForEach(Peer => Peer.PacketHelper.SendPacket(PeerInfo.Value.Item2.ToJSON().ToUTF8Byte(), PacketActionType.PeerLeave));
                        }
                    }
                    else Self.TCPServer.Clients.Remove(Self.TCPServer.Clients.Find(x => x.Connection == Client));

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

            }
        });
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

        Packer.SendUTF8Packet($"{Self.PeerId.ToJSON()}", PacketActionType.SYN, false);
        //Console.WriteLine("[Client] Sent [SYN]");

        // Stay here until we are authenticated
        while (!Packer.IsAuthenticated)
        {
            byte[] Packet = Client.ReceivePacket(out PacketHeader Header);
            if (Header.PacketAction != PacketActionType.Empty)
            {
                switch (Header.PacketAction)
                {
                    case PacketActionType.SYNAck: // SYNAck sent from server - send Ack to client
                        //Console.WriteLine($"[Client] received [SYNAck]");
                        
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
                            //Console.WriteLine($"[Client] sent encrypted ChaChaKey using RSA");
                        }
                        else Console.WriteLine($"[Client] invalid Auth Packet");

                        //Packer.SendUTF8Packet("", PacketActionType.ACK);
                        break;
                    case PacketActionType.ACK:
                        // server sends client ack, will be encrypted with rsa containing the pubkeys hash (data doesnt matter so its ignored), we then send the ChaChaKey
                        //Console.WriteLine($"[Client] received [ACK] from server");

                        if(Packet.IsValidJSON(out PacketEncrypted encrypted))
                        {
                            //Console.WriteLine("encrypted packet detected");
                            if (encrypted.TryDecryptInto(Packer.EncryptionKeys.ChaChaKey, out Auth) && Auth.KeyData.ToHashString() == Packer.EncryptionKeys.ChaChaKey.ToHashString())
                            { 
                                //Console.WriteLine("[client] hash the same");
                                Packer.SendUTF8Packet("<READY>", PacketActionType.Ready, false);
                            }
                            else Console.WriteLine($"[Client] [ACK] failed to decrypt encrypted ChaChaKey");
                        }


                        break;
                    case PacketActionType.Ready:
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
        byte[] DATA = Array.Empty<byte>();
        string UTF8 = string.Empty;

        switch (Header.PacketAction)
        {
            case PacketActionType.Ping: HeartBeat.HandleHeartBeatActions(Header, Data, Helper); break;
            case PacketActionType.Pong: HeartBeat.HandleHeartBeatActions(Header, Data, Helper); break;
            case PacketActionType.Data:
                //OnDataReceived.Invoke(Data.Span);
                break;
            case PacketActionType.PeerJoin:
                Console.WriteLine($"[Client] [{Helper.Self.PeerId}] received PeerJoin");
                DATA = Data.ToArray();
                UTF8 = DATA.ToUTF8String();

                //Console.WriteLine("peer joined");

                // Check if in packet init class
                if (UTF8.IsValidJSON(out PeerTable initPeer)) Self.AddPeer(Helper.ClientHandle, initPeer);
                else if (UTF8.IsValidJSON(out IEnumerable<PeerTable> initPeers)) Self.AddPeers(Helper.ClientHandle, initPeers);
                break;
            //case 
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
        //Console.WriteLine($"Client Received => [{Header.PacketAction}] EncryptionType: [{Header.PacketEncryptionType}] \"{DATA.ToUTF8String()}\""); 
        HandleAction(Header, DATA, Packer);
    }
}
