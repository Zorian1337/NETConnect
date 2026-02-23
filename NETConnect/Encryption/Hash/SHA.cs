using NETConnect.Shared.Multicast;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Security.Cryptography;
using System.Buffers.Text;
using System.ComponentModel.Design;

namespace NETConnect.Encryption.Hash;

public enum SHAHashSize
{
    SHA256,
    SHA3_256,
    //SHA3_384,
    SHA3_512
}

public class SHA
{
    public static byte[] Hash(byte[] data, SHAHashSize Size = SHAHashSize.SHA256)
    {
        switch (Size)
        {
            case SHAHashSize.SHA256:
                using (SHA256 mySHA256 = SHA256.Create()) return mySHA256.ComputeHash(data);
            case SHAHashSize.SHA3_256:
                using (SHA3_256 mySHA3256 = SHA3_256.Create()) return mySHA3256.ComputeHash(data);
            case SHAHashSize.SHA3_512:
                using (SHA3_512 mySHA3_512 = SHA3_512.Create()) return mySHA3_512.ComputeHash(data);
        }

        return Array.Empty<byte>();
    }

    public static string HashToString(byte[] data, SHAHashSize Size = SHAHashSize.SHA256)
    {
        byte[] HashData = Array.Empty<byte>();

        switch (Size)
        {
            case SHAHashSize.SHA256:
                using (SHA256 mySHA256 = SHA256.Create()) HashData = mySHA256.ComputeHash(data); break;
            case SHAHashSize.SHA3_256:
                using (SHA3_256 mySHA3256 = SHA3_256.Create()) HashData = mySHA3256.ComputeHash(data); break;
            case SHAHashSize.SHA3_512:
                using (SHA3_512 mySHA3_512 = SHA3_512.Create()) HashData = mySHA3_512.ComputeHash(data); break;
        }

        if(HashData.Length > 0) return Convert.ToBase64String(HashData);
        else return String.Empty; 
    }

    public static bool TryHash(byte[] data, out byte[] _hashed, SHAHashSize Size = SHAHashSize.SHA256)
    {
        // Run hash like normal
        _hashed = Hash(data, Size);

        // Check for zero 
        if (_hashed.Length > 0) return true;
        else return false;
    }

    public static bool TryHashToString(byte[] data, out string _hashed, SHAHashSize Size = SHAHashSize.SHA256)
    {
        _hashed = String.Empty;

        // Run hash like normal
        byte[] _hashedData = Hash(data, Size);

        // Check for zero 
        if (_hashedData.Length > 0) { _hashed = Convert.ToBase64String(_hashedData); return true; }
        else return false;
    }
}
