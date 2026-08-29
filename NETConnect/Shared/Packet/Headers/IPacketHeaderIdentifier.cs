using NAudio.SoundFont;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace NETConnect.Shared.Packet.Headers;

//public interface IPacketHeader
//{
//    // "NC" - 0x4E43
//    public ushort Magic { get; set; }
//    public byte Version { get; set; }
//}

// basic format of our packets starting 8 bytes
// named packet header identifier as its to detect our packet format
// this format should be the same in the future anything else is subject to change.
public interface IPacketHeaderIdentifier
{
    public ushort Magic { get; set; }                          // 2 byte
    public byte Version { get; set; }                          // 1 byte 
    public byte HeaderLength { get; set; }                     // 1 byte
    public int PayloadLength { get; set; }                     // 4 bytes
    public long SentAt { get; set; }                           // 8 bytes

    public const int PreheaderLength =
    2 +     // Magic
    1 +     // Version
    1 +     // HeaderLength
    4 +     // PayloadLength
    8;      // SentAt

    public static IPacketHeaderIdentifier GetPacketHeaderType(byte Version)
    {
        return Version switch
        {
            2 => new PacketHeader(),
             _ => throw new NotSupportedException($"Unsupported header version: {Version}")
        };
    }




    /// <summary>
    /// Checks if our header is valid at a quick glance using the first 8 bytes to verify if its using the right format.
    /// Data passed can be more than 8 bytes in length but needs to be 8 at minium
    /// </summary>
    /// <param name="preheader"></param>
    /// <returns></returns>
    public static bool IsValidHeader(byte[] preheader, ushort ValidMagic = PacketHeader.MAGIC) => IsValidHeader(preheader, out _, ValidMagic);

    /// <summary>
    /// Checks if our header is valid at a quick glance using the first 8 bytes to verify if its using the right format.
    /// Data passed can be more than 8 bytes in length but needs to be 8 at minium
    /// </summary>
    /// <param name="preheader"></param>
    /// <returns></returns>
    public static bool IsValidHeader(ReadOnlySpan<byte> preheader, out (ushort Magic, byte Version, byte HeaderLength, int PayloadLength, long SentAt) info, ushort ValidMagic = PacketHeader.MAGIC)
    {
        info = (0, 0, 0, -1, 0);
        if (preheader.Length < PreheaderLength) return false;
        //Console.WriteLine("[DEBUG]:IsValidHeader() 8 bytes");

        //Console.WriteLine($"[DEBUG]:IsValidHeader() -> {BitConverter.ToString(preheader.ToArray())}");

        int offset = 2;
        ushort Magic = BinaryPrimitives.ReadUInt16LittleEndian(preheader);
        byte Version = preheader[offset++];
        byte HeaderLength = preheader[offset++];
        int PayloadLength = BinaryPrimitives.ReadInt32LittleEndian(preheader.Slice(offset, 4));
        offset += 4;
        long SentAt = BinaryPrimitives.ReadInt64LittleEndian(preheader.Slice(offset));
        //Console.WriteLine($"[DEBUG]:IsValidHeader() -> PayloadLength: {PayloadLength}");
        info = (Magic, Version, HeaderLength, PayloadLength, SentAt);

        if (Magic == ValidMagic)
        {
            //Console.WriteLine("[DEBUG]:IsValidHeader() Magic Valid");
            // I dont really care about the versioning I just wanna make sure everything else is proper format

            // checks header length if its longer than 8, we can assume the full header was passed
            if (preheader.Length > PreheaderLength && preheader.Length == HeaderLength) { Debug.WriteLine("only header"); return true; } // only header
            else if (preheader.Length > PreheaderLength && preheader.Length == (HeaderLength + PayloadLength)) { Debug.WriteLine("header + payload"); return true; } // header + payload together
            else if (preheader.Length == PreheaderLength) { Debug.WriteLine("preheader"); return true; } // Only preheader was passed
            else return false;

        }
        else return false;
    }


    // NOTE: FOR THIS "BuildFullHeaders" - I dont actually need to implement them into their own PacketHeader class I can just do it all here
    // 
    public static byte[] MergeIntoFull((ushort Magic, byte Version, byte HeaderLength, int PayloadLength, long SentAt) preheader, byte[] header)
    {
        return Array.Empty<byte>();
    }


    /// <summary>
    /// Merges the information we extracted from the first 8 bytes of the packet 
    /// </summary>
    /// <param name="info"></param>
    /// <param name="header"></param>
    /// <returns></returns>
    public byte[] BuildFullHeader((ushort Magic, byte Version, byte HeaderLength, int PayloadLength, long SentAt) info, byte[] header); // implement per packet class/struct 

    /// <summary>
    /// Merges the information we extracted from the first 8 bytes of the packet 
    /// </summary>
    /// <param name="info"></param>
    /// <param name="header"></param>
    /// <returns></returns>
    public byte[] BuildFullHeader((ushort Magic, byte Version, byte HeaderLength, int PayloadLength, long SentAt) info, Span<byte> header); // implement per packet class/struct 

    public byte[] ToPreheaderBinary()
    {
        byte[] preheader = new byte[PreheaderLength];
        Span<byte> span = preheader.AsSpan();

        int offset = 0;
        int inc = 2;

        BinaryPrimitives.WriteUInt16LittleEndian(span[offset..(offset+ inc)], Magic);
        offset += inc;

        span[offset++] = Version;
        span[offset++] = HeaderLength;

        inc = 4;
        BinaryPrimitives.WriteInt32LittleEndian(span[offset..(offset + inc)], PayloadLength);
        offset += inc;

        inc = 8;
        BinaryPrimitives.WriteInt64LittleEndian(span[offset..(offset + inc)], SentAt);
        offset += inc;

        Debug.Assert(offset == PreheaderLength, $"Wrote {offset} bytes but PreheaderLength is {PreheaderLength}");

        return preheader;
    }
}