using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETConnect.Shared.Packet.Headers
{
    /// <summary>
    /// Header to only expose the data neccessary
    /// </summary>
    public class PacketBasisHeader
    {
        public int DataLength { get; set; }
        public PacketEncryptionType EncryptionType { get; set; } 
    }
}
