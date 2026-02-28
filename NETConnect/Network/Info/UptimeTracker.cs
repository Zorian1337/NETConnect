using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETConnect.Network.Info
{
    /// <summary>
    /// Uses this data to form a collective average uptime using collections of this data
    /// </summary>
    public class UptimeTracker
    {
        public DateTime OnlineAt { get; set; }
        public DateTime OfflineAt { get; set; }
    }
}
