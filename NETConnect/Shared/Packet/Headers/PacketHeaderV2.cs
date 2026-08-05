using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETConnect.Shared.Packet.Headers
{
    public enum PacketType: byte
    {
        None = 0,
        Data = 1,
        Gossip = 2,
        Discovery = 3
    }


    public enum PacketActionV2: byte
    {
        None = 0,

    }

    public enum PacketEncodingV2: byte
    {
        None = 0
    }

    public enum PacketEncryptionV2: byte
    {
        None = 0
    }

    [Flags]
    public enum PacketFlags: byte
    {
        None = 0,
        Gossip = 1 << 0,
        Forward = 1 << 1,
        Broadcast = 1 << 2,
    }

    // This is build to be the routing layer, not the handling layer
    // Inside this after we get to our destination and decrypt the packet inside should contain the information on what to do with it
    // we probably dont need to define a new packet inside but we at least need to route this where it needs to go
    public class PacketHeaderV2
    {
        public const int HeaderSize =
        2 +  // Magic
        1 +  // Version
        2 +  // HeaderLength
        4 +  // PacketLength
        8 +  // PacketId
        1 +  // Type
        1 +  // Action
        1 +  // Encoding
        1 +  // Encryption
        1 +  // Flags
        16 + // OriginPeerId
        16 + // RecipientPeerId
        1;   // TTL

        public PacketHeaderV2() { }

        public ushort Magic { get; set; } = 0x4E43;             // "NC" // 2 bytes
        public byte Version { get; set; }                       // 1 byte 
        public byte HeaderLength { get; set; } = HeaderSize;
        public int ByteLength { get; set; }                     // 4 bytes
        public ulong PacketId { get; set; }                     // 8 bytes
        public byte Type { get; set; }
        public PacketActionV2 Action { get; set; }              // 1 bytes
        public PacketEncodingV2 Encoding { get; set; }          // 1 bytes
        public PacketEncryptionV2 Encryption { get; set; }      // 1 bytes
        public Guid OriginPeerId { get; set; }                  // 16 bytes
        public Guid RecipientPeerId { get; set; }               // 16 bytes
        public byte TTL { get; set; }                           // 1 bytes
    }
}
