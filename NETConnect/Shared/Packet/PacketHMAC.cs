using NETConnect.MyExtensions.Encryption;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace NETConnect.Shared.Packet
{
    public class PacketHMAC
    {
        public PacketHMAC() { }
        public byte[] data { get;set; }
        public byte[] nonce { get;set; }
        public byte[] tag { get;set; }

        /// <summary>
        /// Used in this case to verify integrity 
        /// </summary>
        public byte[] HMAC { get; set; }

        public PacketHMAC(byte[] Key, byte[] data, byte[] nonce, byte[] tag)
        {
            this.data = data;
            this.nonce = nonce;
            this.tag = tag;

            HMAC = ComputeHMAC(Key);
        }

        public PacketHMAC(string Key, byte[] data, byte[] nonce, byte[] tag)
        {
            this.data = data;
            this.nonce = nonce;
            this.tag = tag;

            HMAC = ComputeHMAC(Key.ToUTF8Byte());
        }

        public byte[] ComputeHMAC(byte[] Key)
        {
            using var hmac = new HMACSHA256(Key);
            return hmac.ComputeHash(this.ToJSON().ToUTF8Byte());
        }

        public bool IsVerifiedHMAC(byte[] Key)
        {
            if (HMAC == ComputeHMAC(Key)) return true;
            else return false;
        }

        public byte[] DecryptChaCha(byte[] Key) => data.DecryptChaCha(Key, nonce, tag);
    }
}
