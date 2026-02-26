using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace NETConnect.Shared.Packet;


public enum PacketActionType : ushort // ushort is 2 bytes
{
    Empty = 0,
    Ping, Pong,
    SYN, SYNAck, ACK,
    Data, Voice,

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
    PeerLeave
    #endregion
}

public enum PacketEncodingType : ushort
{
    UTF8, 
    JSON, 
    XML,
    BINARY
}

public enum PacketEncryptionType : ushort 
{
    NONE,
    AES, 
    RSA
}

public struct PacketHeader
{
    public PacketHeader(int ByteLength, PacketActionType PacketAction, PacketEncodingType PacketEncodingType)
    {
        this.ByteLength = ByteLength;
        this.PacketAction = PacketAction;
        this.PacketEncodingType = PacketEncodingType;
    }

    public int ByteLength { get; set; }
    public PacketActionType PacketAction { get; set; }
    public PacketEncodingType PacketEncodingType { get; set; }
    public long SentAt { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    //public 


    public const int HeaderSize = 16;
    public Span<byte> WriteTo(Span<byte> buffer)
    {
        // Writes packet length first as thats more important 
        BinaryPrimitives.WriteInt32LittleEndian(buffer, ByteLength); // Ints are 4 bytes
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.Slice(4), (ushort)PacketAction); // Slice into the buffer holding the int from the first insert
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.Slice(6), (ushort)PacketEncodingType); // Does the same as above but includes the Packet action (adds 2 more bytes for the ushort) 
        BinaryPrimitives.WriteInt64LittleEndian(buffer.Slice(8), SentAt); // Skips first 8 to write the next 8
        
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
            

            if((HeaderSize + DataLength) == buffer.Length) // Single packet
            {
                //Console.WriteLine("single packet");
                // Only grab from Header so that we can guarentee that we are getting the right packet
                PacketHeader Packet = new PacketHeader();
                Packet.ByteLength = DataLength;
                Packet.PacketAction = (PacketActionType)BinaryPrimitives.ReadUInt16LittleEndian(Header.Slice(4).Span);
                Packet.PacketEncodingType = (PacketEncodingType)BinaryPrimitives.ReadUInt16LittleEndian(Header.Slice(6).Span);
                Packet.SentAt = BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(8).Span);

                Headers = new PacketHeader[] { Packet };

                // Return Header and packet data
                PacketData = new ReadOnlyMemory<byte>[] { buffer.Slice(HeaderSize) };

                return true;
                //return Packet;
            }
            else if ((HeaderSize + DataLength) > buffer.Length) // Potentially more than 1 packet
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
            Packet.SentAt = BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(8).Span);

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

        if (HeaderBytes.Length == PacketHeader.HeaderSize)
        {
            Span<byte> Buffer = new Span<byte>(HeaderBytes);

            try
            {
                PacketHeader Packet = new PacketHeader();
                Packet.ByteLength = BinaryPrimitives.ReadInt32LittleEndian(Buffer);
                Packet.PacketAction = (PacketActionType)BinaryPrimitives.ReadUInt16LittleEndian(Buffer.Slice(4));
                Packet.PacketEncodingType = (PacketEncodingType)BinaryPrimitives.ReadUInt16LittleEndian(Buffer.Slice(6));
                Packet.SentAt = BinaryPrimitives.ReadInt64LittleEndian(Buffer.Slice(8));

                Header = Packet;
                return true;
            }
            catch (Exception Ex) { Console.WriteLine(Ex.ToString()); return false; }
        }
        else return false;
    }
}
