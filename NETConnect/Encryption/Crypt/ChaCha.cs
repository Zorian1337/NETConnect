using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Diagnostics;


namespace NETConnect.Encryption.Crypt;

public class ChaCha
{


    public class ChaChaKeys
    {
        public ChaChaKeys() { }
        public byte[] nonce { get; set; }
        public byte[] tag { get; set; }
        public ChaChaKeys(byte[] nonce, byte[] tag)
        {
            this.nonce = nonce;
            this.tag = tag;
        }
    }

    public static byte[] Encrypt(byte[] Key, byte[] Data, out ChaChaKeys Keys)
    {
        Keys = default;

        byte[] tag = CryptUtils.GenerateRandomData(16);
        byte[] nonce = CryptUtils.GenerateRandomData(12);

        byte[] EncryptedData = Encrypt(Key, nonce, tag, Data);

        if (EncryptedData is null) return default;
        else
        {
            Keys = new ChaChaKeys(nonce, tag);
            return EncryptedData;
        }
    }


    public static byte[] Encrypt(byte[] Key, byte[] nonce, byte[] tag, byte[] data)
    {
        // I dont care for this so im leaving it default
        byte[]? associatedData = default;

        try
        {
            byte[] EncryptedData = Array.Empty<byte>();
            using ChaCha20Poly1305 ChaChaSlide = new ChaCha20Poly1305(Key);
            ChaChaSlide.Encrypt(nonce, data, EncryptedData, tag);
            return EncryptedData;
        }
        catch (Exception Ex) { Console.WriteLine(Ex.ToString()); Debug.WriteLine(Ex.ToString()); }

        return default;
    }


    public static byte[] Decrypt(byte[] Key, byte[] nonce, byte[] tag, byte[] data)
    {
        // I dont care for this so im leaving it default
        byte[]? associatedData = default;

        try
        {
            byte[] DecryptedData = Array.Empty<byte>(); 
            using ChaCha20Poly1305 ChaChaSlide = new ChaCha20Poly1305(Key);
            ChaChaSlide.Decrypt(nonce, data, tag, DecryptedData);
            return DecryptedData;
        }
        catch (Exception Ex) { Console.WriteLine(Ex.ToString()); Debug.WriteLine(Ex.ToString()); }

        return default;
    } 
}
