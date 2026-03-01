using NETConnect.Encryption.Crypt;
using NETConnect.MyExtensions.Encryption;
using NETConnect.Shared.Packet.Headers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace NETConnect.Shared;

public class SecurityKey
{
    // RSA Key Size

    // RSA Exported Key Data


    // My Security Keys


    // Remote Security Keys - (Will only have the public RSA key)
    public RSAKeySize RSAKeySize { get; set; }

    public RSACrypt.RSAExport LocalRSAKeys { get; set; }

    public byte[] RemoteRSAPubKey { get; set; } 
    

    public int AESKeySize { get; set; }
    public byte[] AESKey { get; set; }

    public byte[] ChaChaKey { get; set; }


    public SecurityKey(RSAKeySize RSAKeySize, RSACrypt.RSAExport LocalRSAKeys)
    {
        this.RSAKeySize = RSAKeySize;
        this.LocalRSAKeys = LocalRSAKeys;
    }

    public SecurityKey() { }
    public void GenerateLocalRSAKeys(int KeySize) => GenerateLocalRSAKeys((RSAKeySize)KeySize);
    public void GenerateLocalRSAKeys(RSAKeySize RSAKeySize)
    {
        int KeySize = (int)RSAKeySize;
        UpdateLocalRSAKeys(KeySize, RSACrypt.CreateExport(KeySize));
    }



    public void UpdateLocalRSAKeys(int RSAKeySize, RSACrypt.RSAExport LocalRSAKeys)
    {
        this.RSAKeySize = RSAKeySize.GetRSASecurityLevel();
        this.LocalRSAKeys = LocalRSAKeys;
    }

    public void UpdateLocalRSAKeys(RSAKeySize RSAKeySize, RSACrypt.RSAExport LocalRSAKeys)
    {
        this.RSAKeySize = RSAKeySize;
        this.LocalRSAKeys = LocalRSAKeys;
    }

    public void SetRemoteRSAKey(byte[] RemoteRSAKey)
    {
        //Console.WriteLine("setting key");
        this.RemoteRSAPubKey = RemoteRSAKey;
        //Console.WriteLine("Set RemoteRSAKey");
    }


    public byte[] GetSecurityKey(PacketEncryptionType EncryptionType, bool IsRemote = false, bool IsPrivate = false)
    {
        byte[] Key = Array.Empty<byte>();

        switch (EncryptionType)
        {
            case PacketEncryptionType.RSA:

                if (IsRemote) Key = RemoteRSAPubKey;
                else if (IsPrivate) Key = LocalRSAKeys.PrivateKey;
                else if (!IsPrivate) Key = LocalRSAKeys.PublicKey;
                break;
            case PacketEncryptionType.ChaCha20Poly1305: Key = ChaChaKey; break;
        }

        return Key;
    }

    public bool TryGetKey(PacketEncryptionType EncryptionType, out byte[] Key, bool IsRemote = false, bool IsPrivate = false)
    {
        Key = GetSecurityKey(EncryptionType, IsRemote, IsPrivate);

        if (Key is null || Key.Length == 0) return false;
        else return true;
    }
}
