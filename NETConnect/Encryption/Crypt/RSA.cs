using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace NETConnect.Encryption.Crypt;



public enum RSAKeySize : int
{
    Weak = 1024,
    Minium = 2048,
    Default = 3072,
    HighSecurity = 4096,
    VerySecure = 8192
}




public class RSACrypt
{
    public record RSAExport(byte[] PrivateKey, byte[]? PublicKey);

    // clean this shit up later


    public static RSA CreatePKCS8(byte[] Key, bool IsPrivate)
    {
        RSA rsa = RSA.Create();

        if(!IsPrivate) rsa.ImportSubjectPublicKeyInfo(Key, out _);
        else rsa.ImportPkcs8PrivateKey(Key, out _);

        return rsa;
    }

    public static int GetPKCS8KeySize(byte[] Key, bool IsPrivate)
    {
        using RSA rsa = CreatePKCS8(Key, IsPrivate);
        return rsa.KeySize;
    }


    public static RSA Create(RSAKeySize SecurityLevel = RSAKeySize.Default) => RSA.Create((int)SecurityLevel);
    public static RSAParameters CreateParams(RSAKeySize SecurityLevel = RSAKeySize.Default)
    {
        using RSA rsa = RSA.Create((int)SecurityLevel);

        RSAParameters Params = rsa.ExportParameters(true);

        return Params;
    }

    public static RSAExport CreateExport(RSAKeySize SecurityLevel = RSAKeySize.Default) => CreateExport((int)SecurityLevel);
    public static RSAExport CreateExport(int KeySize)
    {
        using RSA rsa = RSA.Create(KeySize);
        return new RSAExport(rsa.ExportPkcs8PrivateKey(), rsa.ExportSubjectPublicKeyInfo());
    }

    

    public static string PemEncode(string label, byte[] data)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"-----BEGIN {label}-----");
        builder.AppendLine(Convert.ToBase64String(data, Base64FormattingOptions.InsertLineBreaks));
        builder.AppendLine($"-----END {label}-----");
        return builder.ToString();
    }


}
