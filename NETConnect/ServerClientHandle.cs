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
using NETConnect.Shared.Packet.Headers;

namespace NETConnect;

// Per server client this will hold important data as to reference the client, the clients buffer, connection data etc
public class ServerClientHandle
{
    public PacketHelper PacketHelper {  get; private set; }
    public CancellationTokenSource ClientToken { get; private set; }
    public Guid Id { get; set; } = new Guid();
    public Socket Connection { get; private set; }
    public NetworkBuffer Buffers { get; private set; }
    public DateTime ConnectedAt { get; private set; }

    public HeartBeat HeartBeat { get; private set; }

    //public DateTime LastPingAt { get; private set; }
    //public DateTime PingSentAt { get; private set; }

    //public int TimeoutInSecondsAfterNoPing = 10; // default 90
    //public int SendPingInSeconds = 5; // default 30
    //public int SendPingCoolDownInSeconds = 5; 



    public ServerClientHandle(Socket connection, NetworkBuffer buffers, DateTime connectedAt, ref CancellationTokenSource ClientToken)
    {
        Connection = connection;
        Buffers = buffers;
        ConnectedAt = connectedAt;
        //LastPingAt = DateTime.UtcNow;
        this.ClientToken = ClientToken;
    }

    public void UpdateClientId(Guid Id) => this.Id = Id;

    public void AddPacketHelper(ref PacketHelper Helper) => this.PacketHelper = Helper;
    public void AddHeartBeat(ref HeartBeat HeartBeat) => this.HeartBeat = HeartBeat;

    //public bool TrySendPing(out bool TimedOut)
    //{
    //    TimedOut = false;
    //    try
    //    {
    //        // Check if its time to ping
    //        if(DateTime.UtcNow >= LastPingAt.AddSeconds(SendPingInSeconds))
    //        {
    //            // Cooldown to prevent ping spam if ping is missed
    //            if(DateTime.UtcNow >= PingSentAt.AddSeconds(SendPingCoolDownInSeconds))
    //            {
    //                PingSentAt = DateTime.UtcNow;
    //                var obj = new
    //                {
    //                    Time = DateTime.Now
    //                };

    //                string json = JsonSerializer.Serialize(obj);

    //                PacketHelper.SendUTF8Packet(json, PacketActionType.Ping);
    //            }
    //        }

    //        // Check for timeout
    //        if (DateTime.UtcNow >= LastPingAt.AddSeconds(TimeoutInSecondsAfterNoPing))
    //        {
    //            TimedOut = true;
    //            return false;
    //        }


    //    }
    //    catch (Exception Ex) { Console.WriteLine($"{Ex.ToString()}"); TimedOut = true; }

    //    // Check last ping vs current time to determine if its a timeout

    //    return false; // False means it didnt ping
    //}
}
