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
    public event Action<ReadOnlySpan<byte>> OnDataReceived;


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

        var Client = SocketClient;
        var Buffers = NetworkBuffer;
        PacketHelper Packer = new PacketHelper(ref Client, ref Buffers);

        Packer.SendUTF8Packet("Hello Server!, I am new here");

        while (!Token.IsCancellationRequested)
        {
            Thread.Sleep(100);

            ReadOnlyMemory<byte> Packet = SocketClient.Receive(ref NetworkBuffer.ByteBuffer, ref Buffer, 4);
            if (SocketClient.ReadForPacketV2(Packet, out PacketHeader[] Headers, out ReadOnlyMemory<byte>[] PacketData))
            {
                //Console.WriteLine("SERVER => Packet has been found!");
                // Group packets that are split (when more than one packet at once is supported)

                if(Headers.Length > 0) 
                {
                    // Only one header available so just grab first
                    PacketHeader Header = Headers.FirstOrDefault();
                    ReadOnlyMemory<byte> Data = PacketData.FirstOrDefault();

                    // Handle Packet per Action
                    HandleAction(Header, Data, Packer);
                }
            }        }
    }

    public  void HandleAction(PacketHeader Header, ReadOnlyMemory<byte> Data, PacketHelper Helper)
    {
        switch (Header.PacketAction)
        {
            case PacketActionType.Ping: // Server sends client ping, client sends back pong
                Helper.SendUTF8Packet("<PONG>", PacketActionType.Pong);
                OnDataReceived.Invoke(Data.Span);
                break;

            case PacketActionType.Data:
                OnDataReceived.Invoke(Data.Span);
                break;
        }
    }

    public void HandleOnDataReceived(ReadOnlySpan<byte> Data)
    {
        // Print the message that was received from the client
        Console.WriteLine($"Server => \"{Data.ToUTF8String(NetworkBuffer.CharBuffer)}\""); 
    }
}
