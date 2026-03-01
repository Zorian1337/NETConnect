using Microsoft.VisualBasic;
using NETConnect.Network.Info;
using NETConnect.Peers;
using NETConnect.Shared.Packet;
using NETConnect.Shared.Packet.Headers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NETConnect.Shared;

public class HeartBeat
{
    public Peer Self { get; set;}

    public HeartBeat(ref Peer Self) => this.Self = Self;


    /// <summary>
    /// Beat is a confirmed response from the other connection
    /// </summary>
    public DateTime LastBeatAt { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Pulse is a Ping that the other side needs to respond to
    /// </summary>
    public DateTime LastPulseAt { get; set; } = DateTime.UtcNow;

    public int TimeoutAfterInSeconds = 120; // default 90
    public int PulseAtInSeconds = 5; // default 30
    public int PulseCooldown = 10;

    public bool FirstBeat { get; set; } = true;


    public TimeSpan LastLatency { get; set; }
    public List<PingTracker> PingLog  = new List<PingTracker>();

    public bool IsTimeout()
    {
        if (FirstBeat) return false;
        if (DateTime.UtcNow >= LastBeatAt.AddSeconds(TimeoutAfterInSeconds)) { Console.WriteLine("heartBeat Timed out"); return true; }
        else return false;
    }

    public void SetLastBeat() //bool JustBeat = true
    {
        // Beats can be either when a message is received or over a time where there is no network activity and one gets sent 
        LastBeatAt = DateTime.UtcNow;
        LastPulseAt = DateTime.UtcNow;

    }

    [Obsolete]
    public bool TrySendHeartBeat(ref PacketHelper Helper, out bool IsDisconnected)
    {
        IsDisconnected = false;




        DateTime now = DateTime.UtcNow;

        // Check if Time for beat, and if its not a pulse cooldown
        if(now >= LastBeatAt.AddSeconds(PulseAtInSeconds) && now >= LastPulseAt.AddSeconds(PulseCooldown)) //&& now >= LastPulseAt.AddSeconds(PulseCooldown)
        {
            //Console.WriteLine("First beat detected"); 
            // Inits the beat after our TLS
            if (FirstBeat) { SetLastBeat(); FirstBeat = false; } //return true;

            // Attempts to prevent spam
            //if (now >= LastPulseAt.AddSeconds(PulseCooldown)) return false;


            //LastPulseAt = DateTime.UtcNow;


            // Send heartBeat here
            return SendPing(ref Helper, out IsDisconnected);    
        }

        // Check for timeout or Disconnect
        if (Helper.Connection.IsGracefulShutdown() || IsTimeout())
        {
            IsDisconnected = true;
            return false; // Didnt send heartbeat, but IsDisconnected
        }


        return false; // By default a heartBeat was not sent, and the connection was not disconnected
    }

    public bool SendPing(ref PacketHelper Helper, out bool IsDisconnected)
    {
        IsDisconnected = false;

        // Allow sending data that is in the length of 0, and just send the packet header (those are control instructions)
        int bytesSent = Helper.SendUTF8Packet("<PING>", PacketActionType.Ping);
        Console.WriteLine($"Sending ping [{bytesSent}]");


        if (bytesSent > 0) { SetLastBeat(); return true; }
        else { IsDisconnected = true; return false; } // Failed to send ping, probably due to socket exception 
                                                      //LastPulseAt = DateTime.UtcNow;
    }


    public void UpdateLatency(TimeSpan latency)
    {
        LastLatency = latency;

        // Adaptive interval example
        if (latency.TotalMilliseconds > 200)
            PulseAtInSeconds = 1; // ping faster
        else if (latency.TotalMilliseconds < 50)
            PulseAtInSeconds = 10; // ping slower
        else
            PulseAtInSeconds = 5; // default
    }


    public void HandleHeartBeatActions(PacketHeader Header, ReadOnlyMemory<byte> Data, PacketHelper Helper)
    {
        if(Header.PacketAction == PacketActionType.Ping)
        {
            PingTracker Pinger = new PingTracker()
            {
                PingSentAt = DateTimeOffset.FromUnixTimeMilliseconds(Header.SentAt).DateTime,
                PingReceivedAt = DateTime.UtcNow
                
            };
            Helper.SendUTF8Packet($"{Pinger.ToJSON()}", PacketActionType.Pong);

        }
        else if (Header.PacketAction == PacketActionType.Pong)
        {
            // Check for PingTracker 
            if(Data.ToArray().ToUTF8String().IsValidJSON(out PingTracker Ping))
            {
                Ping.PongReceivedAt = DateTime.UtcNow;

                // Sets the heartbeat speed
                UpdateLatency(Ping.Latency);

                PingLog.Add(Ping);
                //Console.WriteLine($"{Ping.Latency.Milliseconds}ms");
            }
        }
    }

}
