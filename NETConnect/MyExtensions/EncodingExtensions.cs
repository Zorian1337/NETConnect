using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETConnect.MyExtensions;

public static class EncodingExtensions
{


    public static ReadOnlySpan<char> UTF8ByteToUTF8String(this Span<byte> UTF8Byte, char[] CharBuffer)
    {
        // Reuse the Buffer to prevent GC strain

        Span<char> _CharBuffer = new Span<char>(CharBuffer);
        int charWritten = Encoding.UTF8.GetChars(UTF8Byte, _CharBuffer);

        return _CharBuffer.Slice(0, charWritten);
    }

    public static ReadOnlySpan<byte> UTF8StringToUTF8Byte(this string UTF8String, byte[] Buffer)
    {
        Span<byte> _Buffer = new Span<byte>(Buffer);
        int bytesWritten = Encoding.UTF8.GetBytes(UTF8String.AsSpan(), _Buffer);
        return _Buffer.Slice(0, bytesWritten);
    }

    //public static ReadOnlySpan<Char>
}
