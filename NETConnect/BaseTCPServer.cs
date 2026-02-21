using NETConnect.Interfaces;
using NETConnect.MyExtensions;
using NETConnect.Shared;
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
    public event Action<ServerClientHandle, Span<byte>> OnDataReceived;
    

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
        client.SendUTF8("Welcome Client!, You seem to be new here!", ref ClientHandle.Buffers.ByteBuffer);

        Clients.Add(ClientHandle);

        Span<byte> Buffer = new Span<byte>(Buffers.ByteBuffer);


        // Handles client while token is still valid and the client hasnt timed out
        while (!Token.IsCancellationRequested)
        {
            Thread.Sleep(100); // Handles client data at a certain time per loop


            //Checks if client is alive via ping, If no response kicks off server.
            if (ClientHandle.TrySendPing(out bool IsTimeout))
            {


                return; // Probably requires more than this later but for now it works
            } // I care about reading data first this structure will probably change later anyway

            if (client.Available > 0)
            {
                client.Receive(Buffer);
                Span<byte> DATA = Buffer.Slice(0, Buffer.Length);
                OnDataReceived?.Invoke(ClientHandle, DATA);

            }


            //client.ReadAvailableData(ref Buffer);

            
            //OnDataReceived?.Invoke(ClientHandle, DATA);
        }
    }

    public void HandleDataReceived(ServerClientHandle Client, Span<byte> Data)
    {
        // Print the message that was received from the client
        Console.WriteLine($"Client => \"{Data.UTF8ByteToUTF8String(Client.Buffers.CharBuffer)}\"");
    } 
}
