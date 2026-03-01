using NETConnect.Shared.Packet.Headers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETConnect.Shared.Packet;

public class PacketAuthentication
{
    // EncryptionType

    // KeyData

    // KeySize

    public PacketEncryptionType EncryptionType {  get; set; }
    public byte[] KeyData { get; set; }

    public byte[][] ExtraData { get; set; }


    // Settings from the server, that needs to be client synced

}
