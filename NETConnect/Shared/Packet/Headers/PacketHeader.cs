using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace NETConnect.Shared.Packet.Headers;


public enum PacketActionType : ushort // ushort is 2 bytes
{
    Empty = 0,
    Ping, Pong,
    SYN, SYNAck, ACK, Ready,
    Data, Voice,

    // This section is detected to errors
    EmptyEncryptedPacket, 


    #region Peer to Peer Types...
    /// <summary>
    /// Sent when the remote party wants to form a p2p network
    /// </summary>
    P2PInt, 
    
    /// <summary>
    /// Signals to the remote party that a peer has joined, and forwards their information
    /// </summary>
    PeerJoin, 
    
    /// <summary>
    /// Signals when a peer has been shared from another peer (it gets discovery but doesnt have to be connected)
    /// </summary>
    PeerShared,

    /// <summary>
    /// Signals to the remote party that a peer has left, and forwards their information
    /// </summary>
    PeerLeave, Disconnect
    #endregion
}


public enum PacketEncodingType : ushort
{
    UTF8, 
    JSON, 
    XML,
    BINARY
}

[Flags]
public enum  EncryptionTypeFLAG : ushort
{
    NONE = 0,
    AES = 1 >> 1,
    RSA = 1 >> 2,
    ChaCha20Poly1305 = 1 >> 3
}


public enum PacketEncryptionType : ushort 
{
    NONE = 0,
    AES, 
    RSA,
    ChaCha20Poly1305
}

// I think these packets need a direct reference to our peer to make sharing data easier
public struct PacketHeader
{
    public PacketHeader() { }

    public PacketHeader(int ByteLength, PacketActionType PacketAction, PacketEncodingType PacketEncodingType)
    {
        this.ByteLength = ByteLength;
        this.PacketAction = PacketAction;
        this.PacketEncodingType = PacketEncodingType;
    }

    public PacketHeader(int ByteLength, PacketActionType PacketAction, PacketEncryptionType EncryptionType)
    {
        this.ByteLength = ByteLength;
        this.PacketAction = PacketAction;
        PacketEncryptionType = EncryptionType;
    }

    public bool IsSenderIPv4() 
    {
        if (OriginIP.Length == 16 && OriginIP.All(x => x == 0)) return false;
        if (OriginIP.Take(12).All(x => x == 0)) return true;
        else if (OriginIP.All(x => x != 0)) return false;

        return false;
    }

    public bool IsSenderIPv6() 
    {
        if (OriginIP.Length == 16 && OriginIP.All(x => x == 0)) return false;
        else if (IsSenderIPv4()) return false;
        else if (OriginIP.All(x => x != 0)) return true;

        return false;
    }

    public bool IsValidIP(out bool IsIPv4) 
    {
        IsIPv4 = IsSenderIPv4();
        bool IPv6 = IsSenderIPv6();
        if (IsIPv4 || IPv6) return true;
        else return false;
    }

    public string GetIPString() 
    {
        if (IsValidIP(out bool IsIPv4))
        {

            if (IsIPv4) return new IPAddress(OriginIP.TakeLast(4).ToArray()).ToString();
            else return new IPAddress(OriginIP.ToArray()).ToString();
        }
        else return String.Empty;
    }

    /// <summary>
    /// Used to quickly read if the packet that this was sent to is meant for this peer
    /// </summary>
    /// <param name="MyPeerId"></param>
    /// <returns></returns>
    public bool IsPacketForMe(Guid MyPeerId) 
    {
        // Empty means the packet is for anyone who reads it
        if (RecipientPeerId == Guid.Empty || MyPeerId == RecipientPeerId) return true;
        else return false;
    }

    public static PacketHeader GetTraversalHeader(Guid SenderId, string SenderIP, ushort SenderPort, Guid RecipientPeerId) 
    {
        PacketHeader header = new PacketHeader();
        header.OriginPeerId = SenderId;

        // IPv4 will be 4 bytes, IPv6 will be 16 
        byte[] SendableIP = new byte[16];
        IPAddress IP = IPAddress.Parse(SenderIP);
        byte[] IPBytes = IP.GetAddressBytes();

        if (IPBytes.Length == 4) Array.Copy(IPBytes, 0, SendableIP, 12, IPBytes.Length);
        else Array.Copy(IPBytes, 0, SendableIP, 0, SendableIP.Length);

        header.OriginIP = SendableIP;
        header.OriginPort = SenderPort;
        header.RecipientPeerId = RecipientPeerId;
        return header;
    }

    // Basic Header - Maybe add a HeaderLength before ByteLength so we can have dynamic header sizes 
    public int ByteLength { get; set; }
    public PacketActionType PacketAction { get; set; } = PacketActionType.Empty;
    public PacketEncodingType PacketEncodingType { get; set; } = PacketEncodingType.UTF8; // Set default to UTF8 as that is what we are using and decoding into
    public PacketEncryptionType PacketEncryptionType { get; set; } = PacketEncryptionType.NONE;
    public long SentAt { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    
    // End of Basic Header 18 length - create dynamic header later
    

    // This apparently isnt populated by default, this always needs to be (to inform others who this was from regardless of it being shared via gossip)
    public Guid OriginPeerId { get; set; }                  // 16 Bytes
    public Guid RecipientPeerId { get; set; } = Guid.Empty; // 16 Bytes

    public byte[] OriginIP { get; set; }                    // 16 Bytes for both IPv4 and IPv6
    public ushort OriginPort { get; set; }                  // 2 Bytes
    //public byte TTL { get; set; } = 7;                      // 1 Byte(s)


    public const int HeaderSize = 68;           // Originally 18
    public Span<byte> WriteTo(Span<byte> buffer)
    {
        // Writes packet length first as thats more important 

        // Basic Header
        BinaryPrimitives.WriteInt32LittleEndian(buffer, ByteLength);                             // Ints are 4 bytes
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.Slice(4), (ushort)PacketAction);         // Slice into the buffer holding the int from the first insert
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.Slice(6), (ushort)PacketEncodingType);   // Does the same as above but includes the Packet action (adds 2 more bytes for the ushort) 
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.Slice(8), (ushort)PacketEncryptionType); // Skips last 8 to write the next 2
        BinaryPrimitives.WriteInt64LittleEndian(buffer.Slice(10), SentAt);                       // Skips first 10 to write the next 8
        // End of Basic Header - starts at 18
        
        int Offset = 18;
        OriginPeerId.ToByteArray().CopyTo(buffer.Slice(Offset));                                     // Starts at 18 then writes our SenderPeerId 
        Offset += 16;
        RecipientPeerId.ToByteArray().CopyTo(buffer.Slice(Offset));
        Offset += 16;
        OriginIP.CopyTo(buffer.Slice(Offset));
        Offset += 16;
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.Slice(Offset), OriginPort);
        Offset += 2; // 68 header size length here

       // buffer[Offset++] = TTL; // 69 header size length here

        return buffer.Slice(0, HeaderSize); // Returns the amount of bytes that we written to buffer
    }


    /// <summary>
    /// Converts our Binary Span to Memory to lay out our packet/data
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="Packets"></param>
    /// <returns></returns>
    public static bool ReadFrom(ReadOnlyMemory<byte> buffer, out PacketHeader[] Headers, out ReadOnlyMemory<byte>[] PacketData)
    {
        Headers = Array.Empty<PacketHeader>(); // Probably need to return this as something memory effient later
        PacketData = Array.Empty<ReadOnlyMemory<byte>>();

        // Handles more than 1 packet 
        if (buffer.Length > HeaderSize)
        {
            ReadOnlyMemory<byte> Header = buffer.Slice(0, HeaderSize);

            int DataLength = BinaryPrimitives.ReadInt32LittleEndian(Header.Span);
            

            if(HeaderSize + DataLength == buffer.Length) // Single packet
            {
                //Console.WriteLine("single packet");
                // Only grab from Header so that we can guarentee that we are getting the right packet
                PacketHeader Packet = new PacketHeader();
                
                // Basic Header - 18 Length
                Packet.ByteLength = DataLength;
                Packet.PacketAction = (PacketActionType)BinaryPrimitives.ReadUInt16LittleEndian(Header.Slice(4).Span);
                Packet.PacketEncodingType = (PacketEncodingType)BinaryPrimitives.ReadUInt16LittleEndian(Header.Slice(6).Span);
                Packet.PacketEncryptionType = (PacketEncryptionType)BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(8).Span);
                Packet.SentAt = BinaryPrimitives.ReadInt64LittleEndian(buffer.Slice(10).Span);
                // End of Basic Header - starts at 18

                // I dont really like doing it with the offset, makes things look sloppy
                int Offset = 18;
                Packet.OriginPeerId = new Guid(Header.Slice(Offset, 16).Span);      // Starts at 18 then writes our SenderPeerId 
                Offset += 16;
                Packet.RecipientPeerId = new Guid(Header.Slice(Offset, 16).Span); 
                Offset += 16;
                Packet.OriginIP = Header.Slice(Offset, 16).Span.ToArray();
                Offset += 16;
                Packet.OriginPort = BinaryPrimitives.ReadUInt16LittleEndian(Header.Slice(Offset).Span); 
                Offset += 2; // 68 header size length here

                //Packet.TTL = Header.Span[Offset++]; // 69 header size length here
                Headers = new PacketHeader[] { Packet };

                // Return Header and packet data
                PacketData = new ReadOnlyMemory<byte>[] { buffer.Slice(HeaderSize) };

                return true;
                //return Packet;
            }
            else if (HeaderSize + DataLength > buffer.Length) // Potentially more than 1 packet
            {
                //Console.WriteLine($"more than 1 packet");
                // We need to add support for this later
                return false;
                //return default;
            }

        }
        else if (buffer.Length == HeaderSize) // This should probably only run when data is Empty
        {
            //Console.WriteLine("single packet");
            PacketHeader Packet = new PacketHeader();
            Packet.ByteLength = BinaryPrimitives.ReadInt32LittleEndian(buffer.Span);
            Packet.PacketAction = (PacketActionType)BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(4).Span);
            Packet.PacketEncodingType = (PacketEncodingType)BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(6).Span);
            Packet.PacketEncryptionType = (PacketEncryptionType)BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(8).Span);
            Packet.SentAt = BinaryPrimitives.ReadInt64LittleEndian(buffer.Slice(10).Span);

            // I dont really like doing it with the offset, makes things look sloppy
            int Offset = 18;
            Packet.OriginPeerId = new Guid(buffer.Slice(Offset, 16).Span);      // Starts at 18 then writes our SenderPeerId 
            Offset += 16;
            Packet.RecipientPeerId = new Guid(buffer.Slice(Offset, 16).Span);
            Offset += 16;
            Packet.OriginIP = buffer.Slice(Offset, 16).Span.ToArray();
            Offset += 16;
            Packet.OriginPort = BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(Offset).Span);
            Offset += 2; // 68 header size length here

            //Packet.TTL = buffer.Span[Offset++]; // 69 header size length here
            // Return Header, packet data is probably null
            Headers = new PacketHeader[] { Packet };

            return true;
            //return Packet;
        }

        return false;
    }

    public static bool ValidateHeader(byte[] HeaderBytes, out PacketHeader Header)
    {
        Header = default;

        if (HeaderBytes.Length == HeaderSize)
        {
            Span<byte> Buffer = new Span<byte>(HeaderBytes);

            try
            {
                PacketHeader Packet = new PacketHeader();
                Packet.ByteLength = BinaryPrimitives.ReadInt32LittleEndian(Buffer);
                Packet.PacketAction = (PacketActionType)BinaryPrimitives.ReadUInt16LittleEndian(Buffer.Slice(4));
                Packet.PacketEncodingType = (PacketEncodingType)BinaryPrimitives.ReadUInt16LittleEndian(Buffer.Slice(6));
                Packet.PacketEncryptionType = (PacketEncryptionType)BinaryPrimitives.ReadUInt16LittleEndian(Buffer.Slice(8));
                Packet.SentAt = BinaryPrimitives.ReadInt64LittleEndian(Buffer.Slice(10));

                // I dont really like doing it with the offset, makes things look sloppy
                int Offset = 18;
                Packet.OriginPeerId = new Guid(Buffer.Slice(Offset, 16));      // Starts at 18 then writes our SenderPeerId 
                Offset += 16;
                Packet.RecipientPeerId = new Guid(Buffer.Slice(Offset, 16));
                Offset += 16;
                Packet.OriginIP = Buffer.Slice(Offset, 16).ToArray();
                Offset += 16;
                Packet.OriginPort = BinaryPrimitives.ReadUInt16LittleEndian(Buffer.Slice(Offset));
                Offset += 2; // 68 header size length here

                Header = Packet;
                return true;
            }
            catch (Exception Ex) { Console.WriteLine(Ex.ToString()); return false; }
        }
        else return false;
    }

    public PacketHeaderV2 ToPacketHeaderV2()
    {
        // [8-5-26]
        // Looking back now this class is a mess
        // Glad im finally upgrading the packet structure
        // Even though the original is barely usable

        return new PacketHeaderV2()
        {
            Magic = 0x4E43,
            Version = 2, // Debating on leaving this at V1, just need it as the V2 format 
            //HeaderLength = HeaderLength, Already auto defined
            PayloadLength = ByteLength,
            PacketId = PacketHeaderV2.NextPacketId(OriginPeerId),
            Action = (PacketActionV2)((byte)PacketAction),
            Encoding = (PacketEncodingV2)((byte)PacketEncodingType),
            Encryption = (PacketEncryptionV2)((byte)PacketEncryptionType),
            OriginPeerId = OriginPeerId,
            RecipientPeerId = RecipientPeerId,
            TTL = 7
        };
    }
}

