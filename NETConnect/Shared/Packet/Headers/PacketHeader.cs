using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETConnect.Shared.Packet.Headers
{
    // 8-7-26
    // IM NOT ENTIRELY SURE HOW TO IMPLEMENT THIS
    public enum PacketType: byte
    {
        /// <summary>
        /// CONSIDER THIS INVALID DATA -> THIS SHOULD ALWAYS BE POPULATED
        /// </summary>
        NONE = 0,
        // CONTROLS CONNECTION SUBSETS -> SYN, SYN-ACK, ACK, READY
        Control = 1,
        // HANDLES PEER FLOW -> JOIN, LEAVE, DISCOVER ETC
        Peer = 2,
        // 
        Data = 3
    }

    
    public enum PacketAction: byte
    {
        // NONE ON NORMAL DATA
        NONE = 0,

        // CONNECTION 
        SYN = 1, SYNACK = 2, ACK = 3, READY = 4, 
        // DATA
        Broadcast = 5, // DEBATE ON MAKING BROADCAST ONE HOP OR MULTIPLE BASED ON TTL?
        Gossip = 6,

        // PEER
        Join = 7, Leave = 8,

        // PING
        Ping = 9, Pong = 10
    }

    // IMPLEMENT THIS SOON
    // JUST NEED TO UPDATE THE BINARY READ/WRITING AND THE HEADER SIZE
    public enum PacketRoute : byte
    {
        NONE = 0,

        Direct = 1,
        Broadcast = 2,
        Gossip = 3
    }


    // WE NEED TO HANDLE THIS PROPERLY 
    // SO BASE IDEA; HEADER IS ALWAYS IN BINARY-
    // THIS ONLY TELLS US HOW WE ARE GOING TO HANDLE THIS DATA (BASICALLY JUST PARSING INTO A USABLE FORMAT?)
    // FOR NOW WE WILL ONLY SUPPORT DATA IN UTF8 FORMAT
    // ONLY ISSUE IS I CANT ADD JSON, XML, BINARY DIRECTLY AS THAT IS A SERIALIZATION TYPE
    // SO ITS KINDA CONFUSING ON WHATS MEANT TO BE HERE, DO I JUST NEED ANOTHER TYPE TO TELL US WHAT THE PACKET DECODES WITH?
    // OR SHOULD I SCRAP THAT AND JUST HAVE THIS BE SERIALIZATION TYPE?
    // IM JUST GONNA LEAVE THIS AT NONE FOR NOW LOL, IM NOT GONNA USE THIS FOR AWHILE BUT LONGTERM THIS WILL BE VERY IMPORTANT FOR DATA
    // SPLIT THIS INTO TWO FIELDS -> ContentType, Serialization?
    public enum PacketEncoding: byte
    {
        NONE = 0,

    }

    public enum PacketEncryption: byte
    {
        NONE = 0,
        AES,
        RSA,
        ChaCha20Poly1305

    }

    // This is build to be the routing layer, not the handling layer
    // Inside this after we get to our destination and decrypt the packet inside should contain the information on what to do with it
    // we probably dont need to define a new packet inside but we at least need to route this where it needs to go
    public class PacketHeader : IPacketHeaderIdentifier
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
        1 +  // Route
        16 + // OriginPeerId
        16 + // RecipientPeerId
        1;   // TTL

        public const ushort MAGIC = 0x4E43;
        private static readonly Dictionary<Guid, ulong> _packetCounters = new();

        public PacketHeader() { }

        public ushort Magic { get; set; } = MAGIC;              // "NC" 2 bytes
        public byte Version { get; set; } = 2;                  // 1 byte 
        public byte HeaderLength { get; set; } = HeaderSize;    // 1 bytes
        public int PayloadLength { get; set; }                  // 4 bytes
        public ulong PacketId { get; set; }                     // 8 bytes
        public PacketType Type { get; set; }                    // 1 bytes
        public PacketAction Action { get; set; }                // 1 bytes
        public PacketEncoding Encoding { get; set; }            // 1 bytes
        public PacketEncryption Encryption { get; set; }        // 1 bytes
        public PacketRoute Route { get; set; }                  // 1 bytes
        public Guid OriginPeerId { get; set; }                  // 16 bytes
        public Guid RecipientPeerId { get; set; } = Guid.Empty; // 16 bytes
        public byte TTL { get; set; } = 1;                      // 1 bytes
        // ADD SIGNATURE FIELD FOR DATA INTEGRITY

        public PacketHeader(byte[] Payload, PacketType Type, PacketAction Action, PacketEncoding Encoding, PacketEncryption Encryption, PacketRoute Route, Guid OriginPeerId, Guid? RecipientPeerId, byte TTL)
        {
            this.PayloadLength = Payload.Length;
            this.Type = Type;
            this.Action = Action;
            this.Encoding = Encoding;
            this.Encryption = Encryption;
            this.Route = Route;
            this.OriginPeerId = OriginPeerId;

            this.PacketId = NextPacketId(OriginPeerId);
            if(RecipientPeerId != null) this.RecipientPeerId = RecipientPeerId.Value;
            else this.RecipientPeerId = Guid.Empty;

            this.TTL = TTL;
        }

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
            data[offset++] = (byte)Route;

            OriginPeerId.TryWriteBytes(data[offset..]);
            offset += 16;

            RecipientPeerId.TryWriteBytes(data[offset..]);
            offset += 16;

            data[offset] = TTL;

            return data.ToArray();
        }

        public static PacketHeader? FromBinaryHeader(byte[] data)
        {
            Span<byte> buffer = data.AsSpan();

            if (buffer.Length < 2) return null;

            int offset = 0;
            ushort magic = BinaryPrimitives.ReadUInt16LittleEndian(buffer);
            offset += 2;
            if (magic != 0x4E43) return null; // reject packet if its not our magic
            Console.WriteLine($"[DEBUG]:FromBinaryHeader -> Magic valid");

            byte Version = buffer[offset++];
            byte HeaderLength = buffer[offset++];

            // LIMITS VERSION AT 2 RIGHT NOW 
            // ALSO PREVENTS INVALID HEADER SIZES
            if (Version != 2 || HeaderLength != HeaderSize) return null;
            Console.WriteLine($"[DEBUG]:FromBinaryHeader -> Magic valid");

            int PayloadLength = BinaryPrimitives.ReadInt32LittleEndian(buffer[offset..]);
            //Console.WriteLine($"PayloadLength: {PayloadLength}");
            offset += 4;

            ulong PacketID = BinaryPrimitives.ReadUInt64LittleEndian(buffer[offset..]);
            offset += 8;

            PacketType Type = (PacketType)buffer[offset++];
            PacketAction Action = (PacketAction)buffer[offset++];
            PacketEncoding Encoding = (PacketEncoding)buffer[offset++];
            PacketEncryption Encryption = (PacketEncryption)buffer[offset++];
            PacketRoute Route = (PacketRoute)buffer[offset++];

            Guid originPeerId = new Guid(buffer.Slice(offset, 16));
            offset += 16;

            Guid recipientPeerId = new Guid(buffer.Slice(offset, 16));
            offset += 16;

            byte TTL = buffer[offset];

            return new PacketHeader(){
                Magic = magic,
                Version = Version,
                HeaderLength = HeaderLength,
                PayloadLength = PayloadLength,
                PacketId = PacketID,
                Action = Action,
                Encoding = Encoding,
                Encryption = Encryption,
                Route = Route,
                OriginPeerId = originPeerId,
                RecipientPeerId = recipientPeerId,
                TTL = TTL
            };

        }

        public static PacketHeader FromBinaryHeader(Span<byte> buffer)
        {
            if (buffer.Length < 2) return default;

            int offset = 0;
            ushort magic = BinaryPrimitives.ReadUInt16LittleEndian(buffer);
            offset += 2;
            if (magic != 0x4E43) return default; // reject packet if its not our magic
            Console.WriteLine($"[DEBUG]:FromBinaryHeader -> Magic valid");

            byte Version = buffer[offset++];
            byte HeaderLength = buffer[offset++];
            // LIMITS VERSION AT 2 RIGHT NOW 
            // ALSO PREVENTS INVALID HEADER SIZES
            if (Version != 2 || HeaderLength != HeaderSize) return default;
            Console.WriteLine($"[DEBUG]:FromBinaryHeader -> Magic valid");
            //Console.WriteLine($"offset: {offset}");
            //Console.WriteLine($"Payload bytes: {Convert.ToHexString(buffer.Slice(0,4))}");

            int PayloadLength = BinaryPrimitives.ReadInt32LittleEndian(buffer[offset..]);
            //Console.WriteLine($"PayloadLength: {PayloadLength}");
            offset += 4;

            ulong PacketID = BinaryPrimitives.ReadUInt64LittleEndian(buffer[offset..]);
            offset += 8;

            PacketType Type = (PacketType)buffer[offset++];
            PacketAction Action = (PacketAction)buffer[offset++];
            PacketEncoding Encoding = (PacketEncoding)buffer[offset++];
            PacketEncryption Encryption = (PacketEncryption)buffer[offset++];
            PacketRoute Route = (PacketRoute)buffer[offset++];

            Guid originPeerId = new Guid(buffer.Slice(offset, 16));
            offset += 16;

            Guid recipientPeerId = new Guid(buffer.Slice(offset, 16));
            offset += 16;

            byte TTL = buffer[offset];

            return new PacketHeader()
            {
                Magic = magic,
                Version = Version,
                HeaderLength = HeaderLength,
                PayloadLength = PayloadLength,
                PacketId = PacketID,
                Type = Type, 
                Action = Action,
                Encoding = Encoding,
                Encryption = Encryption,
                Route = Route,
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

        
        public byte[] BuildFullHeader((ushort Magic, byte Version, byte HeaderLength, int PayloadLength) info, Span<byte> header)
        {
            Console.WriteLine("[DEBUG]:BuildFullHeader - init");
            // HANDLE INVALID DATA - JUST NEED TO WORK OUT THE CASES WHERE THIS GETS TRIGGERED
            if (header.Length != info.HeaderLength - 8 && header.Length != info.HeaderLength) return Array.Empty<byte>();
            Console.WriteLine("[DEBUG]:BuildFullHeader - first");


            // VALID DATA CASES; 
            // SUBTRACTS THE PREHEADER INFORMATION FROM THE ARRAY TO DETECT IF ITS PERFECTLY ALIGNED WITH THAT LENGTH IF REMOVED
            if (header.Length == (info.HeaderLength - 8))
            {
                Console.WriteLine("HEADER LENGTH = H - 8");

                // THIS IS THE PERFECT CASE BUT SHOULDNT BE THE ONLY CASE
                // THIS SHOULD BE USED TO GRAB HEADER DATA THEN HELP TO BUILD IT INTO THE OTHER EXTRACTED DATA SO THAT NO EFFECIENTCY IS LOST

                int offset = 0;
                Span<byte> FullHeader = stackalloc byte[info.HeaderLength];

                BinaryPrimitives.WriteUInt16LittleEndian(FullHeader[offset..], info.Magic);
                offset += 2;

                FullHeader[offset++] = (byte)info.Version;
                FullHeader[offset++] = (byte)info.HeaderLength;

                BinaryPrimitives.WriteInt32LittleEndian(FullHeader[offset..], info.PayloadLength);
                offset += 4;

                // END OF PREHEADER (EXISTS LIKE THIS ON ALL NEW HEADER VERSIONS)

                //Console.WriteLine($"Header: {BitConverter.ToString(header.ToArray())}");
                //Console.WriteLine($"FullHeader: {BitConverter.ToString(FullHeader.ToArray())}");
                // MERGE PREHEADER WITH HEADER AND RETURN AS FULL
                //header.CopyTo(FullHeader); // NOTE: CopyTo cant be used to merge two arrays
                //Array.Copy(header.ToArray(), 0, FullHeader.ToArray(), offset, header.Length); // look into a replacement for Span instead of using .ToArray
                header.CopyTo(FullHeader[offset..]);


                //Console.WriteLine($"destArray: -> {BitConverter.ToString(FullHeader.ToArray())}");


                return FullHeader.ToArray();
            }
            else Console.WriteLine("[DEBUG]:BuildFullHeader - second");

            // FIND MORE CASES FOR THIS TO BE USED (I CANT THINK RIGHT NOW)

            // default return we'll need to return in the other scopes

            return Array.Empty<byte>();
        }

        public byte[] BuildFullHeader((ushort Magic, byte Version, byte HeaderLength, int PayloadLength) info, byte[] header)
        {
            // HANDLE INVALID DATA - JUST NEED TO WORK OUT THE CASES WHERE THIS GETS TRIGGERED
            if ((HeaderLength != header.Length || header.Length != (HeaderLength + PayloadLength))) return Array.Empty<byte>();

            // VALID DATA CASES; 
            // SUBTRACTS THE PREHEADER INFORMATION FROM THE ARRAY TO DETECT IF ITS PERFECTLY ALIGNED WITH THAT LENGTH IF REMOVED
            if (header.Length == (HeaderLength - 8))
            {
                Console.WriteLine("HEADER LENGTH = H - 8");

                // THIS IS THE PERFECT CASE BUT SHOULDNT BE THE ONLY CASE
                // THIS SHOULD BE USED TO GRAB HEADER DATA THEN HELP TO BUILD IT INTO THE OTHER EXTRACTED DATA SO THAT NO EFFECIENTCY IS LOST

                int offset = 0;
                byte[] FullHeader = new byte[HeaderLength];
                
                BinaryPrimitives.WriteUInt16LittleEndian(FullHeader.AsSpan(offset), Magic);
                offset += 2;

                FullHeader[offset++] = (byte)Version;
                FullHeader[offset++] = (byte)HeaderLength;

                BinaryPrimitives.WriteInt32LittleEndian(FullHeader.AsSpan(offset), PayloadLength);
                offset += 4;

                // END OF PREHEADER (EXISTS LIKE THIS ON ALL NEW HEADER VERSIONS)

                // MERGE PREHEADER WITH HEADER AND RETURN AS FULL
                header.CopyTo(FullHeader, offset);

                return FullHeader;
            }

            // FIND MORE CASES FOR THIS TO BE USED (I CANT THINK RIGHT NOW)

            // default return we'll need to return in the other scopes
            return Array.Empty<byte>();
        }
    }
}
