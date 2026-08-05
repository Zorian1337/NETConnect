using System;
using System.Buffers.Binary;
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
        1 +  // HeaderLength
        4 +  // PayloadLength
        8 +  // PacketId
        1 +  // Type
        1 +  // Action
        1 +  // Encoding
        1 +  // Encryption
        16 + // OriginPeerId
        16 + // RecipientPeerId
        1;   // TTL


        private static readonly Dictionary<Guid, ulong> _packetCounters = new();

        public PacketHeaderV2() { }

        public ushort Magic { get; set; } = 0x4E43;             // "NC" 2 bytes
        public byte Version { get; set; } = 2;                    // 1 byte 
        public byte HeaderLength { get; set; } = HeaderSize;
        /// Only Payload size 
        public int PayloadLength { get; set; }                     // 4 bytes
        public ulong PacketId { get; set; }                     // 8 bytes
        public PacketType Type { get; set; }
        public PacketActionV2 Action { get; set; }              // 1 bytes
        public PacketEncodingV2 Encoding { get; set; }          // 1 bytes
        public PacketEncryptionV2 Encryption { get; set; }      // 1 bytes
        public Guid OriginPeerId { get; set; }                  // 16 bytes
        public Guid RecipientPeerId { get; set; } = Guid.Empty; // 16 bytes
        public byte TTL { get; set; } = 1;                      // 1 bytes




        public byte[] ToBinaryHeader()
        {
            Span<byte> data = new byte[HeaderSize];

            int offset = 0;
            BinaryPrimitives.WriteUInt16LittleEndian(data[offset..], Magic);
            offset += 2;

            data[offset++] = Version;

            data[offset++] = HeaderLength;
            BinaryPrimitives.WriteInt32LittleEndian(data[offset..], PayloadLength);
            offset += 4;

            BinaryPrimitives.WriteUInt64LittleEndian(data[offset..], PacketId);
            offset += 8;

            data[offset++] = (byte)Type;
            data[offset++] = (byte)Action;
            data[offset++] = (byte)Encoding;
            data[offset++] = (byte)Encryption;

            OriginPeerId.TryWriteBytes(data[offset..]);
            offset += 16;

            RecipientPeerId.TryWriteBytes(data[offset..]);
            offset += 16;

            data[offset] = TTL;

            return data.ToArray();
        }

        public static PacketHeaderV2? FromBinaryHeader(byte[] data)
        {
            Span<byte> buffer = data.AsSpan();

            if (buffer.Length < 2) return null;


            int offset = 0;
            ushort magic = BinaryPrimitives.ReadUInt16LittleEndian(buffer);
            offset += 2;
            if (magic != 0x4E43) return null;

            byte Version = buffer[offset++];
            byte HeaderLength = buffer[offset++];


            if (Version != 2 || HeaderLength != HeaderSize) return null;

            int PayloadLength = BinaryPrimitives.ReadInt32LittleEndian(buffer[offset..]);
            offset += 4;

            ulong PacketID = BinaryPrimitives.ReadUInt64LittleEndian(buffer[offset..]);
            offset += 8;

            PacketType Type = (PacketType)buffer[offset++];
            PacketActionV2 Action = (PacketActionV2)buffer[offset++];
            PacketEncodingV2 Encoding = (PacketEncodingV2)buffer[offset++];
            PacketEncryptionV2 Encryption = (PacketEncryptionV2)buffer[offset++];

            Guid originPeerId = new Guid(buffer.Slice(offset, 16));
            offset += 16;

            Guid recipientPeerId = new Guid(buffer.Slice(offset, 16));
            offset += 16;

            byte TTL = buffer[offset];

            return new PacketHeaderV2(){
                Magic = magic,
                Version = Version,
                HeaderLength = HeaderLength,
                PayloadLength = PayloadLength,
                PacketId = PacketID,
                Action = Action,
                Encoding = Encoding,
                Encryption = Encryption,
                OriginPeerId = originPeerId,
                RecipientPeerId = recipientPeerId,
                TTL = TTL
            };

        }

        public static ulong NextPacketId(Guid peerId)
        {
            if (_packetCounters.TryGetValue(peerId, out ulong current))
            {
                current++;
                _packetCounters[peerId] = current;
                return current;
            }

            _packetCounters[peerId] = 1;
            return 1;
        }
    }
}
