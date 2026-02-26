using NETConnect.Network.Info;
using NETConnect.Peers;
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

    public int TimeoutAfterInSeconds = 50; // default 90
    public int PulseAtInSeconds = 5; // default 30
    public int PulseCooldown = 10;


    public TimeSpan LastLatency { get; set; }
    public List<PingTracker> PingLog  = new List<PingTracker>();

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
        if(now >= LastBeatAt.AddSeconds(PulseAtInSeconds)) //&& now >= LastPulseAt.AddSeconds(PulseCooldown)
        {
            // Send heartBeat here
            int bytesSent = Helper.SendUTF8Packet("<PING>", PacketActionType.Ping);

            if (bytesSent > 0) { SetLastBeat(); return true; }
            else { IsDisconnected = true; return false; } // Failed to send ping, probably due to socket exception 
            //LastPulseAt = DateTime.UtcNow;
        }

        return false; // By default a heartBeat was not sent, and the connection was not disconnected
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
            //Console.WriteLine("Server Sent Client <PING>");
            //Helper.SendUTF8Packet("Ping Received, Handling Accordingly", PacketActionType.Data);
            //OnDataReceived.Invoke(Data.Span);
        }
        else if (Header.PacketAction == PacketActionType.Pong)
        {
            // Check for PingTracker 
            if(Data.ToArray().ToUTF8String().IsValidJSON(out PingTracker Ping))
            {
                Ping.PongReceivedAt = DateTime.UtcNow;

                // Sets the heartbeat speed
                UpdateLatency(Ping.Latency);

                PingLog.Add($"{Helper.} {Ping}ms");
                Console.WriteLine(Ping.Latency.Milliseconds); //.ToString(@"hh\:mm\:ss\.fff")
            }


            // Update the heartbeat 

            //Console.WriteLine("Ponging");
            //Helper.SendUTF8Packet("Server Sent Client <PONG>", PacketActionType.Data);
            //Helper.SendUTF8Packet("Pong Received, Handling Accordingly", PacketActionType.Data);
            //OnDataReceived.Invoke(Data.Span);
        }
    }

}
