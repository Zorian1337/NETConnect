using NETConnect.Shared.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
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

    public int TimeoutAfterInSeconds = 10; // default 90
    public int PulseAtInSeconds = 5; // default 30
    public int PulseCooldown = 5;

    public bool IsTimeout()
    {
        if (DateTime.UtcNow >= LastBeatAt.AddSeconds(TimeoutAfterInSeconds)) return true;
        else return false;
    }

    public void SetLastBeat()
    {
        // Beats can be either when a message is received or over a time where there is no network activity and one gets sent 
        LastBeatAt = DateTime.UtcNow;
        LastPulseAt = DateTime.UtcNow;

    }

    //public bool TrySendPing(out bool TimedOut)
    //{
    //    TimedOut = false;
    //    try
    //    {
    //        // Check if its time to ping
    //        if (DateTime.UtcNow >= LastBeatAt.AddSeconds(PulseAtInSeconds))
    //        {
    //            // Cooldown to prevent ping spam if ping is missed
    //            if (DateTime.UtcNow >= LastPulseAt.AddSeconds(PulseCooldown))
    //            {
    //                LastPulseAt = DateTime.UtcNow;
    //                var obj = new
    //                {
    //                    Time = DateTime.Now
    //                };

    //                string json = JsonSerializer.Serialize(obj);

    //                //PacketHelper.SendUTF8Packet(json, PacketActionType.Ping);
    //            }
    //        }

    //        // Check for timeout
    //        if (DateTime.UtcNow >= LastBeatAt.AddSeconds(TimeoutAfterInSeconds))
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
