using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETConnect.Network.Info;

public class PingTracker
{
    public PingTracker() { }

    public DateTime PingSentAt { get; set; }
    public DateTime PingReceivedAt { get; set; }
    public DateTime PongReceivedAt { get; set; }

    public TimeSpan Latency => PongReceivedAt - PingSentAt;

    public static double GetAverageLatencyInMilliseconds(IEnumerable<PingTracker> TrackedPings)
    {
        if (TrackedPings is null || TrackedPings.Count() == 0) return 0;

        return (TrackedPings.Sum(x => x.Latency.TotalMilliseconds) / TrackedPings.Count());
    }

    public PingTracker(DateTime pingSentAt, DateTime pingReceivedAt, DateTime pongReceivedAt)
    {
        PingSentAt = pingSentAt;
        PingReceivedAt = pingReceivedAt;
        PongReceivedAt = pongReceivedAt;
    }
}
