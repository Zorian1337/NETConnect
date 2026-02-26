using NETConnect.Shared.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
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

        // Add feature support for knownPeers and connectedPeers 


        [JsonIgnore]
        public BaseTCPClient Client { get; set; }


        //public Socket Connection { get; private set; }

        /// <summary>
        /// Used for server side to get a direct line
        /// </summary>
        [JsonIgnore]
        public PacketHelper PacketHelper { get; set; }

        public bool IsLocal { get; set; }








        //public NetworkStats 

        public float Realiabilty { get; set; }












        public PeerTable() { }

        public PeerTable(Guid PeerId, string Address, int Port)
        {
            this.PeerId = PeerId;
            this.Address = Address;
            this.Port = Port;
        }



        public PeerTable(ref PacketHelper PacketHelper, Guid PeerId, string Address, int Port)
        {
            this.PacketHelper = PacketHelper;
            this.PeerId = PeerId;
            this.Address = Address;
            this.Port = Port;
        }
    }
}
