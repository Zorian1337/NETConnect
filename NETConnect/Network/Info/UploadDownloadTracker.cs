using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETConnect.Network.Info
{
    public class UploadDownloadTracker
    {
        public int Upload { get; set; }
        public int Download { get; set; }

        public DateTime UpdatedAt { get; set; } 
    }
}
