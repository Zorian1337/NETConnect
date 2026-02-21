using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETConnect.Shared.Packet;

// Used to define how a packet will react if data if found to control it
public class PacketActions
{

    // Figure out way to invoke each classes events so they can be handled at any time
    //public static void HandleAction(PacketHeader Header, ReadOnlyMemory<byte> Data, PacketHelper Helper)
    //{
    //    switch(Header.PacketAction)
    //    {
    //        case PacketActionType.Ping:
    //            Helper.SendUTF8Packet("<PONG>", PacketActionType.Pong);

    //            break;

    //        case PacketActionType.Pong:
    //            break;

    //        case PacketActionType.Data: break;
    //    }
    //}
}
