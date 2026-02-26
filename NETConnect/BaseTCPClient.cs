using NETConnect.Encryption.Crypt;
using NETConnect.MyExtensions;
using NETConnect.MyExtensions.Encryption;
using NETConnect.Network;
using NETConnect.Peers;
using NETConnect.Shared;
using NETConnect.Shared.Packet;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices.Swift;
using System.Text;
using System.Threading.Tasks;

namespace NETConnect;

public class BaseTCPClient
{
    public Peer Self { get; private set; }
    public Guid PeerId { get; private set; }
    public Socket SocketClient { get; set; }
    public CancellationTokenSource Token { get; set; }
    public IPEndPoint? EndPoint { get; set; }



    public HeartBeat HeartBeat { get; set; }
    public PacketHelper Packer { get; set; }
    public int Port { get; set; }

    public NetworkBuffer NetworkBuffer { get; set; } 

    public event Action OnConnected;
    public event Action OnDisconnected;
    public event Action<ReadOnlySpan<byte>> OnDataReceived;

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

    public void HandleConnected()
    {
        string ClientEndPoint = SocketClient.RemoteEndPoint.ToString();
        //Console.WriteLine($"[CLIENT] Connected to server [{ClientEndPoint}]");

        var Client = SocketClient;
        var Buffers = NetworkBuffer;
        // Need this to pass references to things that might still trying to run after disconnect - probably pass into helper
        CancellationTokenSource TokenSource = Token;
        Packer = new PacketHelper(ref Client, ref Buffers, ref TokenSource); //, 

        HeartBeat = new HeartBeat();
        var heartBeat = HeartBeat;
        bool VoiceStarted = false;


        //Console.WriteLine($"[Client] I connected to {Client.RemoteEndPoint} but this is my ID [{Self.PeerId}]:{NetworkUtils.GetLocalLanIp()}:{((IPEndPoint)Client.LocalEndPoint).Port}");
        Console.WriteLine($"[Client] Server: {Client.RemoteEndPoint} - Me: {NetworkUtils.GetLocalLanIp()}:{((IPEndPoint)Client.LocalEndPoint).Port} - ClientId: {Self.PeerId}");



        // Client sends all realavent information to become a p2p system
        //if (P2PMerge) Packer.SendUTF8Packet($"{}", PacketActionType.P2PInt);

        Task.Run(() =>
        {
            bool IsAuthenticated = true;
            while (!Token.IsCancellationRequested)
            {
                Thread.Sleep(5);


                var Helper = Packer;


                if (IsAuthenticated)
                {
                    // Check for ping every so often
                    if (!heartBeat.TrySendHeartBeat(ref Helper, out bool IsDisconnected) && IsDisconnected)
                    {
                        Token.Cancel();

                        if (Self.OperationMode == PeerState.Peer)
                        {

                            var PeerInfo = Self.FindPeerById(Self.PeerId);

                            if (PeerInfo is not null)
                            {
                                Self.TCPServer.Clients.Remove(PeerInfo.Value.Item1);
                                Self.Peers.Remove(PeerInfo.Value.Item2);

                                // Announce to all other clients that peer disconnected
                                Self.Peers.ForEach(Peer => Peer.PacketHelper.SendPacket(PeerInfo.Value.Item2.ToJSON().ToUTF8Byte(), PacketActionType.PeerLeave));
                            }
                        }


                        Console.WriteLine("[Client] Timed out");
                        return;
                    }

                    // This works but we'll mess with that later after auth
                    //if (!VoiceStarted) { Audio.Audio.StartStreaming(ref Helper); VoiceStarted = true; }
                    //else if (VoiceStarted && Token.IsCancellationRequested) { Audio.Audio.StopStreaming(); }

                    // Complete normal auth then share your peer list

                    // receive normal messages after auth
                    byte[] Packet = SocketClient.ReceivePacket(ref heartBeat, out PacketHeader Header);
                    if (Header.PacketAction != PacketActionType.Empty)
                    {
                        if(Header.PacketAction != PacketActionType.Ping && Header.PacketAction != PacketActionType.Pong)
                        {
                            //Console.WriteLine($"[ClientPacketData] {Header.ByteLength} {Header.PacketAction.ToString()} {Header.SentAt}");
                        }
                            


                        HandleAction(Header, Packet, Packer);
                    }
                }
                else
                {
                    // Uses receive version without a heartbeat requirement so we can authenticate before setting up pings
                    byte[] Packet = SocketClient.ReceivePacket(out PacketHeader Header);
                    if (Header.PacketAction != PacketActionType.Empty)
                    {
                        //Console.WriteLine($"[ClientPacketData] {Header.ByteLength} {Header.PacketAction.ToString()} {Header.SentAt} - debug: \"{Packet.ToUTF8String()}\"");

                        string JSON = string.Empty;

                        // If client isnt authenticated drop packets that arent allowed
                        switch (Header.PacketAction)
                        {
                            // Client connects to server, Server Sends SYN (includes server settings, public RSA key)
                            case PacketActionType.SYN:
                                //Console.WriteLine($"Auth Received from {ClientEndPoint}");

                                // Client responds with SYNAck sending its public RSAKey (Encrypted with the servers PublicKey) for privacy

                                JSON = Packet.ToUTF8String();

                                if(JSON.IsValidJSON(out PacketAuthentication AuthPacket))
                                {
                                    //Console.WriteLine("[Client] ValidAuth");
                                    //Console.WriteLine(AuthPacket.KeyData);

                                    // Generate Keys based on server encryption
                                    int KeySize = (int)AuthPacket.KeyData.GetRSASecurityLevel();

                                    Packer.EncryptionKeys.UpdateLocalRSAKeys(KeySize, RSACrypt.CreateExport(KeySize));
                                    Packer.EncryptionKeys.SetRemoteRSAKey(AuthPacket.KeyData);

                                    // Send as AES
                                    //PacketAuthentication Auth = new PacketAuthentication()
                                    //{
                                    //    EncryptionType = AuthPacket.EncryptionType,
                                    //    KeyData = Packer.EncryptionKeys.LocalRSAKeys.PublicKey
                                    //};

                                    //Packer.SendPacket(Auth.ToJSON().ToUTF8Byte().EncryptRSA(AuthPacket.KeyData), PacketActionType.SYN);
                                    //Console.WriteLine("[Client] sent RSAPubkey to server");
                                }

                                break;
                            // Server sends back the client an AES key encrypted with its PubRSAKey
                            case PacketActionType.SYNAck:
                                // Client Encrypts some data, sends it to server with SYNCAck, and some Hashing for integrity
                                break;
                            // Server sends client Ack confirming the data was able to be read and verified
                            case PacketActionType.ACK:
                                // Client sends one final Ack to the server to also confirm it was able to read the data
                                break;
                            default: break;
                        }
                    }


                }


              
            }
        });
    }

    public  void HandleAction(PacketHeader Header, ReadOnlyMemory<byte> Data, PacketHelper Helper)
    {

        switch (Header.PacketAction)
        {
            case PacketActionType.Ping: HeartBeat.HandleHeartBeatActions(Header, Data, Helper); break;
            case PacketActionType.Pong: HeartBeat.HandleHeartBeatActions(Header, Data, Helper); break;
            case PacketActionType.Data:
                //Console.WriteLine($"server sent to client => {Data.Span}");
                OnDataReceived.Invoke(Data.Span);
                break;
            //default:
            //    OnDataReceived.Invoke(Data.Span);
            //    break;
        }
    }

    public void HandleOnDataReceived(ReadOnlySpan<byte> Data)
    {
        byte[] DATA = Data.ToArray();

        if (DATA.Length == 0) return;
        // Print the message that was received from the client
        Console.WriteLine($"Client Received => \"{DATA.ToUTF8String()}\""); 
    }
}
