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
    Ping,
    SYN, SYNAck, ACK,

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
    public long SentAt { get; set; }
    //public 


    public const int HeaderSize = 8;
    public Span<byte> WriteTo(Span<byte> buffer)
    {
        // Writes packet length first as thats more important 
        BinaryPrimitives.WriteInt32LittleEndian(buffer, ByteLength); // Ints are 4 bytes
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.Slice(4), (ushort)PacketAction); // Slice into the buffer holding the int from the first insert
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.Slice(6), (ushort)PacketEncodingType); // Does the same as above but includes the Packet action (adds 2 more bytes for the ushort) 
        
        
        return buffer.Slice(0, HeaderSize); // Returns the amount of bytes that we written to buffer
    }

    // ReadFrom - Converts binary to 
    public static PacketHeader ReadFrom(Span<byte> buffer, out byte[] Packets)
    {
        Packets = Array.Empty<byte>();

        // Handles more than 1 packet 
        if (buffer.Length > HeaderSize)
        {
            ReadOnlySpan<byte> Header = buffer.Slice(0, HeaderSize);

            int DataLength = BinaryPrimitives.ReadInt32LittleEndian(Header);


            if((HeaderSize + DataLength) == buffer.Length) // Single packet
            {
                // Only grab from Header so that we can guarentee that we are getting the right packet
                PacketHeader Packet = new PacketHeader();
                Packet.ByteLength = DataLength;
                Packet.PacketAction = (PacketActionType)BinaryPrimitives.ReadUInt16LittleEndian(Header.Slice(4));
                Packet.PacketEncodingType = (PacketEncodingType)BinaryPrimitives.ReadUInt16LittleEndian(Header.Slice(6));

                return Packet;
            }
            else if ((HeaderSize + DataLength) > buffer.Length) // Potentially more than 1 packet
            {
                return default;
            }

        }
        else if (buffer.Length == HeaderSize) // This should probably only run when data is Empty
        {
            PacketHeader Packet = new PacketHeader();
            Packet.ByteLength = BinaryPrimitives.ReadInt32LittleEndian(buffer);
            Packet.PacketAction = (PacketActionType)BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(4));
            Packet.PacketEncodingType = (PacketEncodingType)BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(6));

            return Packet;
        }

        return default;
    }
}
