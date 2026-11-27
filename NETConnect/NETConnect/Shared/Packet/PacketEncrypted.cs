using NETConnect.MyExtensions;
using NETConnect.Shared.IO.Encryption.Crypt;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace NETConnect.Shared.Packet;

public class PacketEncrypted
{
    public byte[] data { get; set; }
    public byte[] nonce { get; set; }
    public byte[] tag { get; set; }

    public static PacketEncrypted Encrypt(byte[] Key, byte[] data)
    {
        PacketEncrypted encryptedPacket = new PacketEncrypted();
        encryptedPacket.data = ChaCha.Encrypt(Key, data, out ChaCha.ChaChaKeys Keys);
        Console.WriteLine(Keys.ToIndentedJSON());
        encryptedPacket.nonce = Keys.nonce;
        encryptedPacket.tag = Keys.tag;

        return encryptedPacket; 
    }


    public int GetDataLength(byte[] Packet) => ((Packet.Length - 12) - 16);
    public byte[] Decrypt(byte[] Key) => ChaCha.Decrypt(Key, nonce, tag, data);


    public byte[] ToBinary()
    {
        int DataSize = this.data.Length;
        // DataSize + Nonce + Tag
        int Size = DataSize + 12 +16; 

        Span<byte> data = new byte[Size];

        int offset = 0;

        this.data.CopyTo(data[offset..]);
        offset += DataSize;

        nonce.CopyTo(data[offset..]);
        offset += 12;

        tag.CopyTo(data[offset..]);
        offset += 16;

        return data.ToArray();
    }
}
