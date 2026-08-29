using NETConnect.Peers;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NETConnect;

// CREATED 8-28-26
// PURPOSE OF THIS IS TO HANDLE OUR SOCKET 
public class UDPBasePeer
{
    public Peer Self { get; set; }
    public Socket Connection { get; set; } 


    // THINKING ABOUT THIS NOW I'D ALSO NEED TO USE MY HEARTBEAT CLASS IT MIGHT NOT BE ABLE TO WORK IMMEDIATELY
    // IT WONT DUE TO PACKETHELPER USING THE FUNCTIONS WE MADE FOR TCP AUTOMATICALLY FOR PING EXCHANGE, SO WE'LL NEED TO MODIFY

    public void Init(ref Peer Self)
    {
        // CREATE NEW SOCKET FOR OUR CONNECTION
        Connection = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        Connection.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true); // I ASSUME WE'LL NEED THIS


        // THIS IS WHERE WE PASS THE REFERENCE TO THE OUR PEER 
        // MAYBE WE CAN IMMEDIATELY PASS OTHER INFO LIKE PEER TO SEND DATABACK AND FORTH TO HERE 
        // SCRACH THAT, AS THIS NEEDS TO BE STARTED AS SOON OUR TCP-SERVER STARTS, TO SIMPLY RECEIVE OTHER DATA FROM OTHER PEERS
        this.Self = Self; // SAVES REFERENCE TO CURRENT PEER - NOT SURE IF THIS IS ACTUALLY EFFICIENT 

        // INIT SOCKET AND KEEP AN OPEN LINE USING CANCELATION TOKEN
        // BASICALLY WHILE TOKEN ISNT CANCELED;
        // - LOOP RECEIVE AND SEND WHILE VALID 
        // KEEPALIVE


    }

    //alloc once - returned at end
    public byte[] buffer = ArrayPool<byte>.Shared.Rent(65536); 

    public async Task ReceiveAsync(int Port = 0)
    {
        var memoryBuffer = buffer.AsMemory();

        // WHILE OUR TCP SERVER IS UP AND RUNNING USING OUR TOKEN TO VERIFY WE CAN WAIT FOR MESSAGES
        // WAIT FOR MESSAGES USING OUR TCP SERVER TOKEN AS THE TWO ARE STARTED AT THE SAME TIME ANYWAY
        while (!Self.TCPServer.ServerToken.Token.IsCancellationRequested)
        {
            // CREATES ENDPOINT TO BE POPULATED FROM OUR RECEIVE ADDRESS
            EndPoint receiveFrom = new IPEndPoint(IPAddress.Any, Port); 
            SocketReceiveMessageFromResult result = await Connection.ReceiveMessageFromAsync(memoryBuffer, receiveFrom);
            int readBytes = result.ReceivedBytes;
            if (result.ReceivedBytes == 0) continue;

            byte[] packetData = ArrayPool<byte>.Shared.Rent(result.ReceivedBytes);
            memoryBuffer.Slice(0, result.ReceivedBytes).CopyTo(packetData);
        }

        // RETURN ArrayPool AFTER TOKEN IS CANCELED FOR CLEANUP
        ArrayPool<byte>.Shared.Return(buffer);
    }
}
