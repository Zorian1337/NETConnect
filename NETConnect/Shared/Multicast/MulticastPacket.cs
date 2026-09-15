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
    public int Version { get; set; }
    public Guid SenderId { get; set; } 
    public byte[] Data { get; set; }

    public MulticastAction Action { get; set; }


    public MulticastPacket(int Version, Guid SenderId, byte[] data, MulticastAction action)
    {
        this.Version = Version;
        this.SenderId = SenderId;
        this.Data = data;
        this.Action = action;
    }
}
