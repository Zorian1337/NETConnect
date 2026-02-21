using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace NETConnect.MyExtensions;

public static class EncodingExtensions
{


    public static ReadOnlySpan<char> ToUTF8String(this ReadOnlySpan<byte> UTF8Byte, char[] CharBuffer)
    {
        // Reuse the Buffer to prevent GC strain

        Span<char> _CharBuffer = new Span<char>(CharBuffer);
        int charWritten = Encoding.UTF8.GetChars(UTF8Byte, _CharBuffer);

        return _CharBuffer.Slice(0, charWritten);
    }

    public static ReadOnlySpan<byte> ToUTF8Byte(this string UTF8String, byte[] Buffer) // Using the same buffer causes issues as to missing text being encoded
    {
        Span<byte> _Buffer = new Span<byte>(Buffer);
        int bytesWritten = Encoding.UTF8.GetBytes(UTF8String.AsSpan(), _Buffer);
        return _Buffer.Slice(0, bytesWritten);
    }

    public static byte[] ToUTF8Byte(this string UTF8String) => Encoding.UTF8.GetBytes(UTF8String);
    public static string ToUTF8String(this byte[] UTF8Byte) => Encoding.UTF8.GetString(UTF8Byte);

}
