using NETConnect.Encryption.Crypt;
using NETConnect.Encryption.Hash;
using NETConnect.MyExtensions.Encryption;
using NETConnect.Shared.Packet.Headers;
using Org.BouncyCastle.Crypto.Agreement;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace NETConnect.Shared;


public record X25519KeyParams(X25519PrivateKeyParameters PrivateKey, X25519PublicKeyParameters PublicKey)
{
    public static X25519KeyParams Generate()
    {
        var privateKey = new X25519PrivateKeyParameters(new SecureRandom());
        var publicKey = privateKey.GeneratePublicKey();
        return new X25519KeyParams(privateKey, publicKey);
    }

    public X25519PublicKeyParameters RemotePublicKey { get; set; }

    public bool SetRemoteKey(byte[] PublicKey, out byte[] ChaChaKey)
    {
        ChaChaKey = Array.Empty<byte>();

        if (PublicKey is null || PublicKey.Length != 32) { Debug.WriteLine("Invalid remote public key"); return false; }

        try
        {
            RemotePublicKey = new X25519PublicKeyParameters(PublicKey, 0);

            var agreement = new X25519Agreement();
            agreement.Init(PrivateKey);
            byte[] sharedSecret = new byte[32];
            agreement.CalculateAgreement(RemotePublicKey, sharedSecret, 0);

            ChaChaKey = sharedSecret;
            return true;
        }
        catch(Exception Ex) { Debug.WriteLine($"Error setting remote public key: {Ex.Message}");  }

        return false;
    }


}

public class SecurityKey
{
    // RSA Key Size

    // RSA Exported Key Data


    // My Security Keys


    public ECDsa PublicSigningKey { get; set; }

    // Remote Security Keys - (Will only have the public RSA key)
    public RSAKeySize RSAKeySize { get; set; }
    public RSACrypt.RSAExport LocalRSAKeys { get; set; }

    public byte[] RemoteRSAPubKey { get; set; } 
    

    public int AESKeySize { get; set; }
    public byte[] AESKey { get; set; }
    public byte[] ChaChaKey { get; set; }
    public X25519KeyParams X25519Key { get; set; }


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


    public byte[] GetSecurityKey(PacketEncryption EncryptionType, bool IsRemote = false, bool IsPrivate = false)
    {
        byte[] Key = Array.Empty<byte>();

        switch (EncryptionType)
        {
            case PacketEncryption.RSA:

                if (IsRemote) Key = RemoteRSAPubKey;
                else if (IsPrivate) Key = LocalRSAKeys.PrivateKey;
                else if (!IsPrivate) Key = LocalRSAKeys.PublicKey;
                break;
            case PacketEncryption.ChaCha20Poly1305: Key = ChaChaKey; break;
        }

        return Key;
    }

    public bool TryGetKey(PacketEncryption EncryptionType, out byte[] Key, bool IsRemote = false, bool IsPrivate = false)
    {
        Key = GetSecurityKey(EncryptionType, IsRemote, IsPrivate);

        if (Key is null || Key.Length == 0) return false;
        else return true;
    }
}
