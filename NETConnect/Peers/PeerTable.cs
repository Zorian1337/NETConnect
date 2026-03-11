using NETConnect.Network.Info;
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

        [JsonIgnore]
        public Peer Self { get; set; }  
        public Guid PeerId { get; set; }

        [JsonIgnore]
        public BaseTCPClient Client { get; set; }

        /// <summary>
        /// Used for server side to get a direct line
        /// </summary>
        [JsonIgnore]
        public PacketHelper PacketHelper { get; set; }

        public bool IsLocal { get; set; }
        public string Address { get; set; }
        public int Port { get; set; }

        public string AddressPort
        {
            get => $"{Address}:{Port}";
        }

        public NetworkStats NetStats { get; set; }






        //public PeerTable




        public PeerTable() { }

        public PeerTable(ref Peer Self, string Address, int Port)
        {
            this.Self = Self;   
            this.PeerId = Self.PeerId;   
            this.Address = Address;
            this.Port = Port;
        }

        public PeerTable(Guid PeerId, string Address, int Port)
        {
            this.PeerId = PeerId;
            this.Address = Address;
            this.Port = Port;

            this.NetStats = new NetworkStats(PeerId);
            //Console.WriteLine(NetStats.ToJSON());
        }



        public PeerTable(ref PacketHelper PacketHelper, Guid PeerId, string Address, int Port)
        {
            this.PacketHelper = PacketHelper;
            this.PeerId = PeerId;
            this.Address = Address;
            this.Port = Port;

            this.NetStats = new NetworkStats(PeerId);
        }
    }
}
