using NETConnect.Shared.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETConnect.Shared
{

    /// <summary>
    /// Settings for each individual peer server, They can be different for each, but they are all determined by the Server Peer
    /// </summary>
    public class PeerSettings
    {
        /// <summary>
        /// Amount of time in ms that it takes for a peer to check for messages
        /// </summary>
        public int ReceivePollTimer { get; set; } = 50;
        /// <summary>
        /// Restricts the number of peers recorded in peer discovery section
        /// </summary>
        public int MaxPeerDiscoveryLimit { get; set; } = 50;
        /// <summary>
        /// Restricts the amount of connections on a given server
        /// </summary>
        public int MaxConnectionPerPeer { get; set; } = 10;
        /// <summary>
        /// Restricts how many peers can be shared in one connection list
        /// </summary>
        public float UniquePeerRequirement = 70;
        
        /// <summary>
        /// Prevents connections to peers that have lower than 70% rep
        /// </summary>
        public float ReputationLimits = 70;
        
        public PacketEncryptionType EncryptionType {  get; set; }


    }
}
