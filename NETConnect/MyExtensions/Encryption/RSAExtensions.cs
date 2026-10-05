
using NETConnect.Encryption.Crypt;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using static NETConnect.Encryption.Crypt.RSACrypt;
namespace NETConnect.MyExtensions.Encryption;

// NOTE FOR THE FUTURE:
// 10/5/26 originally planned to use BouncyCastle for RSA, but ended up decided to just phase out RSA for X25519 to pair with ChaCha20Poly1305.
// My implementation of RSA resulted in issues on wine, due to compatability, it would be easier just to switch to another much better and faster encryption method

public static class RSAExtensions
{
    public static RSAKeySize GetRSASecurityLevel(this int KeySize)
    {
        if (Enum.IsDefined(typeof(RSAKeySize), KeySize)) return (RSAKeySize)KeySize;
        else return default; // this'll need fixed later
    }

    public static RSAKeySize GetRSASecurityLevel(this byte[] RSAPubKey)
    {
        using RSA rsa = RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(RSAPubKey, out _);

        return GetRSASecurityLevel(rsa.KeySize);
    }

    public static byte[] EncryptRSA(this byte[] Data, byte[] PublicKey)
    {
        try
        {
            using RSA rsa = CreatePKCS8(PublicKey, false);
            rsa.ImportSubjectPublicKeyInfo(PublicKey, out _);

            RSAEncryptionPadding Padding = RSAEncryptionPadding.OaepSHA256;

            return rsa.Encrypt(Data, Padding);
        }
        catch (CryptographicException CEx) { }

        catch (Exception Ex) { Console.WriteLine(Ex.ToString()); Debug.WriteLine(Ex.ToString()); }

        return Array.Empty<byte>();
    }

    public static byte[] DecryptRSA(this byte[] Data, byte[] PrivateKey)
    {
        try
        {
            using RSA rsa = CreatePKCS8(PrivateKey, true);

            RSAEncryptionPadding Padding = RSAEncryptionPadding.OaepSHA256;
            return rsa.Decrypt(Data, Padding);
        }
        catch (Exception Ex) { Console.WriteLine(Ex.ToString()); Debug.WriteLine(Ex.ToString()); }

        return Array.Empty<byte>();
    }


    public static RSAExport GetRSAKeys(this RSA Crypt)
    {
        return new RSAExport(Crypt.ExportPkcs8PrivateKey(), Crypt.ExportSubjectPublicKeyInfo());
    }


}
