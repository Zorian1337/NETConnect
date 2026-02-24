using NETConnect.Interfaces;
using NETConnect.MyExtensions;
using NETConnect.MyExtensions.Encryption;
using NETConnect.Peers;
using NETConnect.Shared;
using NETConnect.Shared.Multicast;
using NETConnect.Shared.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NETConnect;

// Each instance of this class should be able to create a new server running on a different port.
public class BaseTCPServer : BaseServerProperties, IServer
{
    public Peer Self { get; set; }
    public Socket SocketServer { get; set; }
    public CancellationTokenSource ServerToken { get; set; }
    public IPAddress Address { get; set; }



    public int Port { get; set; }


    public event Action<Socket> OnClientConnected;
    public event Action<Socket> OnClientDisconnected;
    public event Action<ServerClientHandle, ReadOnlySpan<byte>> OnDataReceived;
    

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

    public void StartServer()
    {
        // Prevent multiple servers from running in the same app instance 

        Task.Run(() =>
        {
            NETConnect.Audio.Audio.Init();

            try
            {
                Console.WriteLine("Starting TCP Server!\n");

                // Init stuff if it doesnt already exist

                if (SocketServer is null)
                {
                    // Creates instance of socket server if not existing
                    SocketServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                    // Wire up our base events
                    OnClientConnected += HandleClientConnected;
                    OnDataReceived += HandleDataReceived;

                    ServerToken = new CancellationTokenSource();
                }


                // Verify Address and Port arent null 

                SocketServer.Bind(new IPEndPoint(Address, Port));
                SocketServer.Listen();

                // Grabbed this just to assign it directly to the server class
                int assignedPort = ((IPEndPoint)SocketServer.LocalEndPoint).Port;
                Port = assignedPort;

                Console.WriteLine($"Server listening at {SocketServer.LocalEndPoint.ToString()}\n");

                // Check if multicast is valid then send announcement
                if(Self is not null)
                {
                    // Transmit server IP and Port to connect to it over local net
                    IPEndPoint Point = ((IPEndPoint)SocketServer.LocalEndPoint);//.ToString();

                    // This is where the server can join the multicast
                    var SelfPeer = Self;
                    Self.Multicast = new Multicast(ref SelfPeer);
                    Self.Multicast.ReadMulticast();

                    string LocalConnection = $"{NetworkUtils.GetLocalLanIp()}:{Point.Port}";
                    Console.Title = $"Server: [{LocalConnection}]";

                    Self.Multicast.SendUTF8Message(LocalConnection, MulticastAction.Join);

                    //Console.WriteLine($"connected via {}"); //Client {remoteIp} 

                    // Using this for informational purposes related to peers
                    //Task.Run(async () =>
                    //{
                    //    while (!ServerToken.IsCancellationRequested)
                    //    {
                    //        await Task.Delay(1000);

                    //        //Clear console for a clean display while doing peer mapping
                    //        Console.Clear();

                    //        Console.WriteLine($"Connected Clients: {Clients.Count()}");
                    //        Console.WriteLine($"Connected Peer Clients: {Self.Clients.Count()}");

                    //        Console.WriteLine();

                    //        Console.WriteLine($"Peers I connected to: \n{String.Join("\n", Self.Clients.Select(x => x.EndPoint))}");
                    //    }
                    //});
                }

                // Handles searching for clients in another thread...
                Task.Run(() =>
                {
                    // Searches for clients until the token is set to get ready to cancel
                    while (!ServerToken.IsCancellationRequested)
                    {
                        Console.WriteLine("Waiting on new client connections...\n");
                        Socket Client = SocketServer.Accept();

                        // Find client then immediately handle it elsewhere for performance
                        OnClientConnected?.Invoke(Client);
                    }
                });

            }
            catch (Exception ex) { Console.WriteLine(ex.ToString()); }
        });
    }

    public void HandleClientConnected(Socket client)
    {
        Console.WriteLine($"[SERVER] Client connected to the server [{client.RemoteEndPoint.ToString()}]");

        CancellationTokenSource _ServerToken = ServerToken;
        CancellationTokenSource ClientToken = new CancellationTokenSource();

        // Keeps a valid buffer span to reuse
        var Client = client;
        NetworkBuffer Buffers = new NetworkBuffer();

        ServerClientHandle ClientHandle = new ServerClientHandle(client, Buffers, DateTime.UtcNow, ref ClientToken);
        PacketHelper Packer = new PacketHelper(ref Client, ref Buffers, ref ClientHandle, ref _ServerToken);
        ClientHandle.AddPacketHelper(ref Packer);
        Clients.Add(ClientHandle);

        HeartBeat heartBeat = new HeartBeat();

        bool IsAuthenticated = false;
        bool HasSentSYN = false;


        // Handles client while token is still valid and the client hasnt timed out
        while (!ClientToken.IsCancellationRequested)
        {
            Thread.Sleep(5); // Handles client data at a certain time per loop

            // Path after key auth
            if (IsAuthenticated)
            {
                if (!heartBeat.TrySendHeartBeat(ref Packer, out bool IsDisconnected) && IsDisconnected)
                {
                    ClientToken.Cancel();
                    Console.WriteLine("[Server] Client Timed out");
                    return;
                }

                byte[] Packet = client.ReceivePacket(ref heartBeat, out PacketHeader Header);

                if (Header.PacketAction != PacketActionType.Empty)
                {
                    //Console.WriteLine($"[ServerPacketData] {Header.ByteLength} {Header.PacketAction.ToString()} {Header.SentAt}");
                    HandleAction(Header, Packet, Packer);
                }
            }
            else
            {
                // Send SYN to client with our server settings and security keys
                if (!HasSentSYN)
                {
                    PacketAuthentication auth = new PacketAuthentication()
                    {
                        EncryptionType = PacketEncryptionType.RSA,
                        KeyData = Packer.EncryptionKeys.LocalRSAKeys.PublicKey
                    };

                    Packer.SendUTF8Packet(auth.ToJSON(), PacketActionType.SYN);
                    HasSentSYN = true;

                    // Make some timer to reenable this incase for some reason, this SYN was not detected
                }



                byte[] Packet = client.ReceivePacket(out PacketHeader Header);

                if (Header.PacketAction != PacketActionType.Empty)
                {
                    Console.WriteLine($"[ServerPacketData] {Header.ByteLength} {Header.PacketAction.ToString()} {Header.SentAt} - debug: \"{Packet.ToUTF8String()}\"");

                    string JSON = string.Empty;

                    // If client isnt authenticated drop packets that arent allowed
                    switch (Header.PacketAction)
                    {
                        // Client connects to server, Server Sends SYN (includes server settings, public RSA key)
                        case PacketActionType.SYN:
                            // Client responds with SYN sending its public RSAKey (Encrypted with the servers PublicKey) for privacy

                            // Client should have sent (encrypted PacketAuth)
                            byte[] Decrypted = Packet.DecryptRSA(Packer.EncryptionKeys.LocalRSAKeys.PrivateKey);

                            if(Decrypted.Length > 0 && Decrypted.ToUTF8String().IsValidJSON(out PacketAuthentication AuthPacket))
                            {
                                Packer.EncryptionKeys.SetRemoteRSAKey(AuthPacket.KeyData);
                                Console.WriteLine("Client sent server their rsa pub key in the servers rsa encrypted key");
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
    }

    public void HandleAction(PacketHeader Header, ReadOnlyMemory<byte> Data, PacketHelper Helper)
    {
        switch (Header.PacketAction)
        {
            case PacketActionType.Ping:
                Helper.SendUTF8Packet("<PONG>", PacketActionType.Pong);

                break;
            case PacketActionType.Pong:

                break;


            case PacketActionType.SYN:
                break;



            case PacketActionType.Data:
                OnDataReceived.Invoke(Helper.ClientHandle, Data.Span);
                break;

            case PacketActionType.Voice:
                NETConnect.Audio.Audio.QueueAudio(Data.ToArray());
                break;

            default:
                OnDataReceived.Invoke(Helper.ClientHandle, Data.Span);
                break;
        }
    }




    public void HandleDataReceived(ServerClientHandle Client, ReadOnlySpan<byte> Data)
    {
        byte[] DATA = Data.ToArray();

        // Print the message that was received from the client
        Console.WriteLine($"Server Received => \"{DATA.ToUTF8String()}\""); //Client.Buffers.CharBuffer
    } 
}
