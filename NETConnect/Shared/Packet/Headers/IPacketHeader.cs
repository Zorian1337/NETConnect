using System;
using System.Buffers.Binary;
using System.Collections.Generic;
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
    
    /// <summary>
    /// Checks if our header is valid at a quick glance using the first 8 bytes to verify if its using the right format.
    /// Data passed can be more than 8 bytes in length but needs to be 8 at minium
    /// </summary>
    /// <param name="preheader"></param>
    /// <returns></returns>
    public bool IsValidHeader(byte[] preheader, ushort ValidMagic = PacketHeader.MAGIC) => IsValidHeader(preheader, out _, ValidMagic);

    /// <summary>
    /// Checks if our header is valid at a quick glance using the first 8 bytes to verify if its using the right format.
    /// Data passed can be more than 8 bytes in length but needs to be 8 at minium
    /// </summary>
    /// <param name="preheader"></param>
    /// <returns></returns>
    public bool IsValidHeader(ReadOnlySpan<byte> preheader, out (ushort Magic, byte Version, byte HeaderLength, int PayloadLength) info, ushort ValidMagic = PacketHeader.MAGIC)
    {
        info = (0, 0, 0, -1);
        if (preheader.Length < 8) return false;

        int offset = 2;
        ushort Magic = BinaryPrimitives.ReadUInt16LittleEndian(preheader);
        byte Version = preheader[offset++];
        byte HeaderLength = preheader[offset++];
        int PayloadLength = BinaryPrimitives.ReadInt32LittleEndian(preheader.Slice(offset));
        info = (Magic, Version, HeaderLength, PayloadLength);

        if (Magic == ValidMagic)
        {
            // I dont really care about the versioning I just wanna make sure everything else is proper format

            // checks header length if its longer than 8, we can assume the full header was passed
            if (preheader.Length > 8 && preheader.Length == HeaderLength) return true; // only header
            else if (preheader.Length > 8 && preheader.Length == (HeaderLength + PayloadLength)) return true; // header + payload together
            else return false;

        }
        else return false;
    }

    /// <summary>
    /// Merges the information we extracted from the first 8 bytes of the packet 
    /// </summary>
    /// <param name="info"></param>
    /// <param name="header"></param>
    /// <returns></returns>
    public byte[] BuildFullHeader((ushort Magic, byte Version, byte HeaderLength, int PayloadLength) info, byte[] header); // implement per packet class/struct 
}