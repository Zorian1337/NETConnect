using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NETConnect.Peers
{
    public class PeerTable
    {
        public Guid PeerId { get; set; }
        public string Address { get; set; }
        public int Port { get; set; }
        [JsonIgnore]
        public BaseTCPClient Client { get; set; }

        public bool IsLocal { get; set; }

        public PeerTable() { }

        public PeerTable(Guid peerId, string address, int port)
        {
            PeerId = peerId;
            Address = address;
            Port = port;
        }
    }
}
