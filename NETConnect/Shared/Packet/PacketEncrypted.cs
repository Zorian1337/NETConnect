using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETConnect.Shared.Packet;

public class PacketEncrypted
{
    public PacketEncrypted() {  }
    public byte[] EncryptedData { get; set; }
    public byte[] nonce { get; set; }
    public byte[] tag { get; set; }
    public PacketEncrypted(byte[] encryptedData, byte[] nonce, byte[] tag)
    {
        EncryptedData = encryptedData;
        this.nonce = nonce;
        this.tag = tag;
    }
}
