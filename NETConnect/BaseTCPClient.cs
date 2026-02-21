using NETConnect.MyExtensions;
using NETConnect.Shared;
using NETConnect.Shared.Packet;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NETConnect;

public class BaseTCPClient
{
    public Socket SocketClient { get; set; }

    public CancellationToken Token { get; set; }

    public IPEndPoint? EndPoint { get; set; }

    public int Port { get; set; }


    public NetworkBuffer NetworkBuffer { get; set; } 

    public event Action OnConnected;
    public event Action OnDisconnected;
    public event Action<Span<byte>> OnDataReceived;


    public bool TryConnect(string IP, int Port)
    {
        // Init some starting client stuff
        if(SocketClient is null)
        {
            SocketClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            Token = new CancellationToken();

            NetworkBuffer = new NetworkBuffer();

            OnConnected += HandleConnected;

            OnDataReceived += HandleOnDataReceived;
        }


        // Parse IP to IPAddress
        if(IPAddress.TryParse(IP, out IPAddress? _IPAddress))
        {
            // Try to connect to server here
            this.EndPoint = new IPEndPoint(_IPAddress, Port);

            try { SocketClient.Connect(EndPoint); OnConnected?.Invoke(); return true; }
            catch (Exception Ex) { Debug.WriteLine($"TryConnect Exception: {Ex.ToString()}"); }
            
            // Failed to parse IPAddress
            return false;
        }

        // Returns false if client didnt connect properly
        return false;
    }

    public void HandleConnected()
    {
        Console.WriteLine("ClientAPP connected to server");

        Span<byte> Buffer = new Span<byte>(NetworkBuffer.ByteBuffer);
        SocketClient.SendUTF8("Hello Server!, I am new here", ref NetworkBuffer.ByteBuffer);

        while (!Token.IsCancellationRequested)
        {
            Thread.Sleep(100);


            // Check for timeout every so often

            // Then detect any incoming messages
            //if (SocketClient.Available > 0)
            //{
            //    // OnAvailable Only read the data that we need to
            //    int ReceivedLength = SocketClient.Receive(Buffer);
            //    Span<byte> DATA = Buffer.Slice(0, ReceivedLength);

            //    OnDataReceived?.Invoke(DATA);

            //} // Reuse this later doing testing with our packet header 

            if(SocketClient.Available > 4)
            {
                // OnAvailable Only read the data that we need to
                int ReceivedLength = SocketClient.Receive(Buffer);//.Receive(Buffer, 0, 4);
                Span<byte> DATA = Buffer.Slice(0, ReceivedLength);

                // Read the first 4 bytes 
                PacketHeader Header = PacketHeader.ReadFrom(DATA, out _);

                Console.WriteLine($"ActionType: {Header.PacketAction} - PacketLength: {Header.ByteLength}");



                //OnDataReceived?.Invoke(DATA);
            }
        }
    }


    public void HandleOnDataReceived(Span<byte> Data)
    {
        // Print the message that was received from the client
        Console.WriteLine($"Server => \"{Data.UTF8ByteToUTF8String(NetworkBuffer.CharBuffer)}\"");
    }
}
