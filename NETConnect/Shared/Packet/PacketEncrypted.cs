using NETConnect.Encryption.Crypt;
using NETConnect.Encryption.Hash;
using NETConnect.MyExtensions.Encryption;
using NETConnect.Shared.Packet.Headers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace NETConnect.Shared.Packet;


/// <summary>
/// PacketHMAC shares mostly the same structure (not sure which one that will be used but underlying packet stucture will be setup for autodecryption)
/// </summary>
public class PacketEncrypted
{
    public PacketEncrypted() { }

    public byte[] encryptedData { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public byte[] Nonce { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
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


    public byte[] Decrypt(byte[] Key)
    {
        switch (EncryptionType)
        {
            case PacketEncryptionType.RSA: return this.encryptedData.DecryptRSA(Key);
            case PacketEncryptionType.ChaCha20Poly1305: return this.encryptedData.DecryptChaCha(Key, this.Nonce, this.Tag);
        }

        return Array.Empty<byte>();
    }

    public byte[] Decrypt(PacketHelper Packer, bool IsRemote = false, bool IsPrivate = false) => Decrypt(Packer.EncryptionKeys.GetSecurityKey(EncryptionType, IsRemote, IsPrivate));

    public bool TryDecrypt(PacketHelper Packer, out byte[] Decrypted, bool IsRemote = false, bool IsPrivate = false) => TryDecrypt(Packer.EncryptionKeys.GetSecurityKey(EncryptionType, IsRemote, IsPrivate), out Decrypted);
    public bool TryDecrypt(byte[] Key, out byte[] Decrypted)
    {
        Decrypted = Decrypt(Key);

        if (Decrypted is null || Decrypted.Length == 0) return false;
        else return true;
    }

  
    public T DecryptInto<T>(byte[] Key)
    {
        byte[] decryptedData = Decrypt(Key);

        if (decryptedData.ToUTF8String().IsValidJSON(out T data)) return data;
        else return default;
    }

    public bool TryDecryptInto<T>(byte[] Key, out T data)
    {
        data = DecryptInto<T>(Key);

        if (data is not null) return true;
        else return false;
    }


    public static byte[] EncryptUT8Bytes(byte[] data, byte[] Key, PacketEncryptionType EncryptionType)
    {
        if(Key is not null)
        {
            PacketEncrypted encrypted = new PacketEncrypted();
            encrypted.EncryptionType = EncryptionType;

            switch (EncryptionType)
            {
                case PacketEncryptionType.RSA: encrypted.encryptedData = data.EncryptRSA(Key); break;
                case PacketEncryptionType.ChaCha20Poly1305:
                    encrypted.encryptedData = data.EncryptChaCha(Key, out ChaCha.ChaChaKeys Keys);
                    encrypted.Nonce = Keys.nonce;
                    encrypted.Tag = Keys.tag;
                    break;
            }

            
            return encrypted.GetAsUTF8();
        }

        return Array.Empty<byte>();
    }

    //public static byte[] DecryptUTF8Bytes(byte[] data, byte[] Key, PacketEncryptionType EncryptionType)
    //{

    //}


    public static bool TryEncryptSend(byte[] unencryptedData, PacketEncryptionType EncryptionType, PacketActionType ActionType, PacketHelper Packer)
    {
        byte[] data = default;

        // If data is null, we might aswell send the empty array 
        if(unencryptedData == null) unencryptedData = Array.Empty<byte>();

        // Handles encryption based on the keys stored inside the PacketHelper (if it cant encrypt the data, it cant be sent and returns false) 
        switch (EncryptionType)
        {
            case PacketEncryptionType.RSA: data = EncryptUT8Bytes(unencryptedData, Packer.EncryptionKeys.RemoteRSAPubKey, EncryptionType); break;
            case PacketEncryptionType.ChaCha20Poly1305: data = EncryptUT8Bytes(unencryptedData, Packer.EncryptionKeys.ChaChaKey, EncryptionType); break;
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
