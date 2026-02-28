using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETConnect.Network.Info;

public class NetworkStats
{
    public Guid PeerId { get; set; }




    // need to figure out a realiable way to record upload/download speed 
    public int Upload { get; set; } = 1;
    public int Download { get; set; } = 1;

    public List<PingTracker> LastFewPings { get; set; }
    

    public int TotalBytesSent { get; set; }
    public int TotalBytesRead { get; set; }

    public int RequestsServed { get; set; } = 0;
    public int CurrentConnections { get; set; } = 0;


    public int TotalPeersShared { get; set; }
    public int TotalPeersDiscovered { get; set; }



    /// <summary>
    /// Score to determine its worth as a peer (0-100)
    /// </summary>
    public float Reputation
    {
        get
        {
            // Ideal peer is someone with availability at/above 70%
            // Average uptime above 30 minutes
            // Has less than 4 duplicate peers
            // Has an upload/download speed at 0.5mb/s (not sure the good average)



            // Uses Availiblity, Upload/Download 

            return 100f;
        }
    }


    /// <summary>
    /// Will measure how long this stays up compared to downtime etc
    /// </summary>
    public float Availability
    {
        get
        {
            // Dynamically updates based on "LastFewConnections"

            return 100f;
        }
    }
    public List<UptimeTracker> LastFewConnections { get; set; }


    // Simple recording of the peer start time
    public DateTime PeerStarted { get; set; }

    public DateTime LastUpdated { get; set; }

}
