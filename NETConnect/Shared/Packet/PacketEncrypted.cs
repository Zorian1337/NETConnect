using NETConnect.Encryption.Crypt;
using NETConnect.Encryption.Hash;
using NETConnect.MyExtensions.Encryption;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETConnect.Shared.Packet;


/// <summary>
/// PacketHMAC shares mostly the same structure (not sure which one that will be used but underlying packet stucture will be setup for autodecryption)
/// </summary>
public class PacketEncrypted
{
    public PacketEncrypted() { } 

    public byte[] encryptedData { get; set; }
    public byte[] Nonce { get; set; }
    public byte[] Tag { get; set; }

    public PacketEncryptionType EncryptionType { get; set; }


    public PacketEncrypted(byte[] encryptedData, byte[] nonce, byte[] tag, PacketEncryptionType encryptionType)
    {
        this.encryptedData = encryptedData;
        Nonce = nonce;
        Tag = tag;
        EncryptionType = encryptionType;
    }

    /// <summary>
    /// Converts into JSON then into UTF8 to safely send over the network
    /// </summary>
    /// <returns></returns>
    public byte[] GetAsUTF8() => this.ToJSON().ToUTF8Byte();

    ///// <summary>
    ///// Sends encrypted message down stream, Directly from the instance itsel, removing extra complexity
    ///// </summary>
    ///// <returns></returns>
    //public int SendEncryptedMessage()
    //{


    //}

    public bool Send()
    {
        return false;
    }

    public static bool TryEncryptSend(byte[] unencryptedData, PacketEncryptionType EncryptionType, PacketActionType ActionType, PacketHelper Packer)
    {
        byte[] data = default;

        // If data is null, we might aswell send the empty array 
        if(unencryptedData == null) unencryptedData = Array.Empty<byte>();

        // Handles encryption based on the keys stored inside the PacketHelper (if it cant encrypt the data, it cant be sent and returns false) 
        switch (EncryptionType)
        {
            case PacketEncryptionType.RSA:
                if (Packer.EncryptionKeys.RemoteRSAPubKey is not null) data = unencryptedData.EncryptRSA(Packer.EncryptionKeys.RemoteRSAPubKey);
                break;
        }

        if(data is not null)
        {
            int bytesSent = Packer.SendEncryptedPacket(data, EncryptionType, ActionType);

            if (bytesSent > 0) return true;
            else return false;
        }
        else return false;
    }

}
