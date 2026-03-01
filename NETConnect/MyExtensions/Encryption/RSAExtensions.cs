
using NETConnect.Encryption.Crypt;
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
