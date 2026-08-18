using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace NETConnect.Encryption.Hash
{
    public static class ECDSA_P256
    {
        // ExportPkcs8PrivateKey to be loaded from file later
        public static ECDsa GenerateIdentityKey() => ECDsa.Create(ECCurve.NamedCurves.nistP256);
        public static byte[] ExportPrivateKey(ECDsa Key) => Key.ExportPkcs8PrivateKey();
        public static byte[] ExportPublicKey(ECDsa Key) => Key.ExportSubjectPublicKeyInfo();
        public static ECDsa ImportPrivateKey(byte[] data)
        {
            ECDsa key = ECDsa.Create();
            key.ImportPkcs8PrivateKey(data, out _);
            return key;
        }
        public static byte[] GenerateSignature(ECDsa Key, byte[] Data, HashAlgorithmName HashType) => Key.SignData(Data, HashType);
        public static bool IsValidSignature(ECDsa Key, byte[] Signature, byte[] Data, HashAlgorithmName HashType) => Key.VerifyData(Data, Signature, HashType); 

    }
}
