using NETConnect.Shared.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NETConnect.Shared;

public class HeartBeat
{
    /// <summary>
    /// Beat is a confirmed response from the other connection
    /// </summary>
    public DateTime LastBeatAt { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Pulse is a Ping that the other side needs to respond to
    /// </summary>
    public DateTime LastPulseAt { get; set; } = DateTime.UtcNow;

    public int TimeoutAfterInSeconds = 50; // default 90
    public int PulseAtInSeconds = 5; // default 30
    public int PulseCooldown = 10;

    public bool IsTimeout()
    {
        if (DateTime.UtcNow >= LastBeatAt.AddSeconds(TimeoutAfterInSeconds)) return true;
        else return false;
    }

    public void SetLastBeat() //bool JustBeat = true
    {
        // Beats can be either when a message is received or over a time where there is no network activity and one gets sent 
        LastBeatAt = DateTime.UtcNow;
        LastPulseAt = DateTime.UtcNow;

    }

    public bool TrySendHeartBeat(ref PacketHelper Helper, out bool IsDisconnected)
    {
        IsDisconnected = false;

        // Check for timeout or Disconnect
        if (Helper.Connection.IsGracefulShutdown() || IsTimeout())
        {
            IsDisconnected = true;
            return false; // Didnt send heartbeat, but IsDisconnected
        }

        DateTime now = DateTime.UtcNow;

        // Check if Time for beat, and if its not a pulse cooldown
        if(now >= LastBeatAt.AddSeconds(PulseAtInSeconds) && now >= LastPulseAt.AddSeconds(PulseCooldown))
        {
            // Send heartBeat here
            int bytesSent = Helper.SendUTF8Packet("<PING>", PacketActionType.Ping);

            if (bytesSent > 0) { SetLastBeat(); return true; }
            else { IsDisconnected = true; return false; } // Failed to send ping, probably due to socket exception 
            //LastPulseAt = DateTime.UtcNow;
        }

        return false; // By default a heartBeat was not sent, and the connection was not disconnected
    }


}
