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

        public BaseTCPClient GetClient(Peer Self)
        {
            if(Client is null)
            {
                var SelfPeer = Self;
                Client = new BaseTCPClient(ref SelfPeer);
            }

            return Client;
        }


        /// <summary>
        /// Used for server side to get a direct line
        /// I think in real communications this wont really be used but im unsure, p2p is alot to wrap my head around
        /// </summary>
        [JsonIgnore]
        public PacketHelper PacketHelper { get; set; }

        [JsonIgnore]
        public string AddressPort => $"{Address}:{Port}";

        [JsonIgnore]
        public bool IsConnected 
        { 
            get
            {
                PacketHelper helper = GetPacketHelper();
                if (helper is null) return false;
                else return true;
            }
        }

        //public bool IsLocal { get; set; } - true or false based on public or private IP
        public string Address { get; set; }
        public int Port { get; set; }

        //[JsonIgnore] // ignored for now - makes it hard to visualize this in json 
        // we still need to have this get updated at some point
        public NetworkStats NetStats { get; set; } 

        // needs to be updated via the client when they discover
        // gossip to say who they know?, could do gossip but without a fanout (would reach further)
        /// <summary>
        ///  Logs the collection of all of our total peers (stores connected peers aswell) - Use .IsConnected property to check if its there too
        /// </summary>
        public List<PeerTable> DiscoveredPeers { get; set; } = new List<PeerTable>();

        public PacketHelper GetPacketHelper() {
            if (PacketHelper is null) return Client.Packer;
            else return PacketHelper;
        }


        public PeerTable() { }

        public PeerTable(ref Peer Self, string Address, int Port)
        {
            this.Self = Self;   
            this.PeerId = Self.PeerId;   
            this.Address = Address;
            this.Port = Port;

            this.NetStats = new NetworkStats(PeerId);
        }

        // I DONT SEEM TO USE THIS VERSION 
        // PROBABLY WILL REMOVE IT SOON
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
