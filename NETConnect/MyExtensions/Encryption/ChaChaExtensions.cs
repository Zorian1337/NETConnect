using NETConnect.Encryption.Crypt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NETConnect.Encryption.Crypt.ChaCha;

namespace NETConnect.MyExtensions.Encryption;

public static class ChaChaExtensions
{

    public static byte[] EncryptChaChaToUTF8Byte(this string UTF8String, byte[] Key, out ChaChaKeys Keys) => UTF8String.ToUTF8Byte().EncryptChaCha(Key, out Keys);

    public static byte[] EncryptChaCha(this byte[] data, byte[] Key, out ChaChaKeys Keys)
    {
        byte[] EncryptedData = Encrypt(Key, data, out Keys);

        if (EncryptedData is null) return default;
        else return EncryptedData;
    }


    public static string DecryptChaChaToUTF8String(this byte[] data, byte[] Key, byte[] nonce, byte[] tag) => DecryptChaCha(data, Key, nonce, tag).ToUTF8String();
    public static byte[] DecryptChaCha(this byte[] data, byte[] Key, byte[] nonce, byte[] tag)
    {
        byte[] DecryptedData = Decrypt(Key, nonce, tag, data);

        if (DecryptedData is null) return default;
        else return DecryptedData;
    }
}
