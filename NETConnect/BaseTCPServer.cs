using NETConnect.Interfaces;
using NETConnect.MyExtensions;
using NETConnect.Shared;
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
    public Socket SocketServer { get; set; }
    public CancellationToken Token { get; set; }
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

    public void StartServer()
    {
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

                Token = new CancellationToken();
            }


            // Verify Address and Port arent null 

            SocketServer.Bind(new IPEndPoint(Address, Port));
            SocketServer.Listen();

            Console.WriteLine($"Server listening on Port {Port}\n");


            // Searches for clients until the token is set to get ready to cancel
            while (!Token.IsCancellationRequested)
            {
                Console.WriteLine("Waiting on new client connections...\n");
                Socket Client = SocketServer.Accept();

                // Find client then immediately handle it elsewhere for performance
                OnClientConnected?.Invoke(Client);
            }
        }
        catch (Exception ex) { Console.WriteLine(ex.ToString()); }

        

    }

    public void HandleClientConnected(Socket client)
    {
        Console.WriteLine("client connected on the server");
        
        // Keeps a valid buffer span to reuse
        NetworkBuffer Buffers = new NetworkBuffer();

        ServerClientHandle ClientHandle = new ServerClientHandle(client, Buffers, DateTime.UtcNow);
        //client.SendUTF8("Welcome Client!, You seem to be new here!", ref ClientHandle.Buffers.ByteBuffer);

        Clients.Add(ClientHandle);

        Span<byte> Buffer = new Span<byte>(Buffers.ByteBuffer);


        var Client = client;
        PacketHelper Packer = new PacketHelper(ref Client, ref Buffers, ref ClientHandle);


        // Handles client while token is still valid and the client hasnt timed out
        while (!Token.IsCancellationRequested)
        {
            Thread.Sleep(100); // Handles client data at a certain time per loop


            //Checks if client is alive via ping, If no response kicks off server.
            if (ClientHandle.TrySendPing(out bool IsTimeout))
            {


                return; // Probably requires more than this later but for now it works
            } // I care about reading data first this structure will probably change later anyway



            ReadOnlyMemory<byte> Packet = client.Receive(ref Buffers.ByteBuffer, ref Buffer, 4);
            if (client.ReadForPacketV2(Packet, out PacketHeader[] Headers, out ReadOnlyMemory<byte>[] PacketData))
            {
                //Console.WriteLine("SERVER => Packet has been found!");
                // Group packets that are split (when more than one packet at once is supported)

                if (Headers.Length > 0)
                {
                    // Only one header available so just grab first
                    PacketHeader Header = Headers.FirstOrDefault();
                    ReadOnlyMemory<byte> Data = PacketData.FirstOrDefault();

                    // Handle Packet per Action
                    HandleAction(Header, Data, Packer);
                }
            }
        }
    }

    public void HandleAction(PacketHeader Header, ReadOnlyMemory<byte> Data, PacketHelper Helper)
    {
        switch (Header.PacketAction)
        {
            case PacketActionType.Pong:
                Helper.SendUTF8Packet("Pong Received, Handling Accordingly", PacketActionType.Data);
                OnDataReceived.Invoke(Helper.ClientHandle, Data.Span);
                break;
            case PacketActionType.Data:
                OnDataReceived.Invoke(Helper.ClientHandle, Data.Span);
                break;
        }
    }

    public void HandleDataReceived(ServerClientHandle Client, ReadOnlySpan<byte> Data)
    {
        // Print the message that was received from the client
        Console.WriteLine($"Client => \"{Data.ToUTF8String(Client.Buffers.CharBuffer)}\""); //Client.Buffers.CharBuffer
    } 
}
