using NETConnect.MyExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NETConnect.Shared.Packet;

public class PacketHelper
{
    // Store some stuff within this helper to allow us to skip reusing it


    public Socket Connection { get; private set; }
    public NetworkBuffer Buffers { get; private set; }
    public ServerClientHandle ClientHandle { get; private set; }

    public PacketHelper(ref Socket Connection, ref NetworkBuffer Buffers)
    {
        this.Connection = Connection;
        this.Buffers = Buffers;
    }

    public PacketHelper(ref Socket Connection, ref NetworkBuffer Buffers, ref ServerClientHandle ClientHandle)
    {
        this.Connection = Connection;
        this.Buffers = Buffers;
        this.ClientHandle = ClientHandle;
    }


    public void SendUTF8Packet(string UTF8Data, PacketActionType Type = PacketActionType.Data)
    {
        //Console.WriteLine("sending utf8 packet");
        ReadOnlySpan<byte> Data = UTF8Data.ToUTF8Byte(Buffers.ReadUTF8Buffer); ////Buffers.ByteBuffer
        Connection.Send(ref Buffers.WriteBuffer, Data, Type);
    }
}
