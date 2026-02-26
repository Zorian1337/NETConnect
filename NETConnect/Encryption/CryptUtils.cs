using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Security.Cryptography;
using NETConnect.Encryption.Crypt;

namespace NETConnect.Encryption;

public class CryptUtils
{
    /// <summary>
    /// Calculates the max length of a byte array for a given key size
    /// </summary>
    /// <param name="SecurityLevel"></param>
    /// <returns></returns>
    public static int MaxByteLengthKeySize(RSAKeySize SecurityLevel) => ((int)SecurityLevel / 8);

    public static byte[] GenerateRandomData(int Length)
    {
        // Generates random byte array data
        byte[] Key = new byte[Length];
        RandomNumberGenerator.Fill(Key);

        return Key;
    }
}
