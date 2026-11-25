using System;
using System.Collections.Generic;
using System.Text;

namespace NETConnect.Shared.Packet.Transport;

//
public sealed class CryptoContext
{
    public byte[] SendKey { get; set; }
    public byte[] ReceiveKey { get; set; }
    public long SendCounter;
    public long ReceiveCounter;
    public bool IsEstablished { get; set; }

    public byte[] Encrypt(ReadOnlySpan<byte> plaintext, out byte[] tag)
    {
        tag = default;

        return Array.Empty<byte>();
    }
    public byte[] Decrypt(ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> tag)
    {
        tag = default;

        return Array.Empty<byte>();
    }
}
