using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETConnect.Shared.Multicast;

public enum MulticastAction
{
    Join, Leave, Data
}

public class MulticastPacket
{
    public Guid SenderId { get; set; } 
    public byte[] Data { get; set; }

    public MulticastAction Action { get; set; }


    public MulticastPacket(Guid senderId, byte[] data, MulticastAction action)
    {
        SenderId = senderId;
        Data = data;
        Action = action;
    }
}
