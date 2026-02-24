using NETConnect.Encryption.Crypt;
using NETConnect.MyExtensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

    /// <summary>
    /// This token will only directly be for current instance
    /// </summary>
    public CancellationTokenSource Token { get; private set; }

    public SecurityKey EncryptionKeys { get; private set; } 

    /// <summary>
    /// used for the client version
    /// </summary>
    /// <param name="Connection"></param>
    /// <param name="Buffers"></param>
    /// <param name="Token"></param>
    public PacketHelper(ref Socket Connection, ref NetworkBuffer Buffers, ref CancellationTokenSource Token)
    {
        this.Connection = Connection;
        this.Buffers = Buffers;

        // Init Keys
        EncryptionKeys = new SecurityKey();
    }

    /// <summary>
    /// Used for the server version
    /// </summary>
    /// <param name="Connection"></param>
    /// <param name="Buffers"></param>
    /// <param name="ClientHandle"></param>
    public PacketHelper(ref Socket Connection, ref NetworkBuffer Buffers, ref ServerClientHandle ClientHandle, ref CancellationTokenSource Token)
    {
        this.Connection = Connection;
        this.Buffers = Buffers;
        this.ClientHandle = ClientHandle;

        // Server will be incharge of the type of encryption used, so safe to generate here...
        RSAKeySize KeySize = RSAKeySize.HighSecurity;
        this.EncryptionKeys = new SecurityKey(KeySize, RSACrypt.CreateExport(KeySize));
    }


    public int SendPacket(byte[] Data, PacketActionType Type = PacketActionType.Data)
    {
        int bytesSent = -1;

        try { bytesSent = Connection.Send(Data, Type); }
        catch (Exception Ex) { Debug.WriteLine($"Error In (SendUTF8Packet): {Ex.ToString()}"); }

        return bytesSent;
    }

    public int SendUTF8Packet(string UTF8Data, PacketActionType Type = PacketActionType.Data)
    {
        int bytesSent = -1;

        try { bytesSent = Connection.Send(UTF8Data.ToUTF8Byte(), Type); }
        catch (Exception Ex) { Debug.WriteLine($"Error In (SendUTF8Packet): {Ex.ToString()}"); }

        return bytesSent;
    }

    public int SendVoicePacket(byte[] VoiceData)
    {
        int bytesSent = -1;

        try { bytesSent = Connection.Send(VoiceData, PacketActionType.Voice); }
        catch (Exception Ex) { Console.WriteLine($"Error In (SendVoicePacket): {Ex.ToString()}"); Debug.WriteLine($"Error In (SendVoicePacket): {Ex.ToString()}"); }

        return bytesSent;
    }

}
