using NETConnect.MyExtensions;
using NETConnect.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;


using NETConnect.Shared.Packet;

namespace NETConnect;

// Per server client this will hold important data as to reference the client, the clients buffer, connection data etc
public class ServerClientHandle
{
    public Guid Id { get; set; } = new Guid();
    public Socket Connection { get; private set; }
    public NetworkBuffer Buffers { get; private set; }
    public DateTime ConnectedAt { get; private set; }
    public DateTime LastPingAt { get; private set; }

    public int TimeoutInSecondsAfterNoPing = 90; // default 90
    public int SendPingInSeconds = 5; // default 30

    public ServerClientHandle(Socket connection, NetworkBuffer buffers, DateTime connectedAt)
    {
        Connection = connection;
        Buffers = buffers;
        ConnectedAt = connectedAt;
        LastPingAt = DateTime.UtcNow;
    }



    public bool TrySendPing(out bool TimedOut)
    {
        TimedOut = false;
        try
        {
            // Check if its time to ping
            if(DateTime.UtcNow >= LastPingAt.AddSeconds(SendPingInSeconds))
            {
                // Attempt to send ping
                //Connection.SendUTF8("<PING>", ref Buffers.ByteBuffer);// changing text later but we at least need to send the ping

                var obj = new
                {
                    Time = DateTime.Now
                };

                string json = JsonSerializer.Serialize(obj);
                Connection.Send(ref Buffers.ByteBuffer, json.ToUTF8Byte(Buffers.ByteBuffer), PacketActionType.Ping);


                // Mark ping sucessful
            }

        }
        catch (Exception ex) { TimedOut = true; }

        // Check last ping vs current time to determine if its a timeout

        return false; // False means it didnt ping
    }
}
