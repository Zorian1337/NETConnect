using NETConnect.Encryption.Hash;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace NETConnect.MyExtensions;

public static class EncodingExtensions
{

    public static string ToHashString(this object Obj) => SHA.HashToString(Obj.ToJSON().ToUTF8Byte());

    public static T FromUTF8IntoJSON<T>(this byte[] data)
    {
        if (data is null || data.Length == 0) return default;

        if (data.ToUTF8String().IsValidJSON<T>(out T Item)) return Item;
        else return default;
    }

    public static string ToJSON(this object Obj, JsonSerializerOptions? Options = null)
    {
        if (Obj == null) return string.Empty;

        try { return JsonSerializer.Serialize(Obj, Options); }
        catch (Exception Ex) { Console.WriteLine(Ex.ToString()); }

        return string.Empty;
    }


    public static bool IsValidJSON<T>(this byte[] UTF8, out T data, JsonSerializerOptions? Options = null) => UTF8.ToUTF8String().IsValidJSON<T>(out data, Options);

    public static bool IsValidJSON<T>(this Span<byte> UTF8, out T data, JsonSerializerOptions? Options = null) => UTF8.ToUTF8String().IsValidJSON<T>(out data, Options);
    public static bool IsValidJSON<T>(this string JSON, out T data, JsonSerializerOptions? Options = null)
    {
        data = default;

        if (string.IsNullOrEmpty(JSON) || string.IsNullOrWhiteSpace(JSON)) return false;

        //Console.WriteLine($"IsValidJSON - {JSON}");

        try
        {
            data = JsonSerializer.Deserialize<T>(JSON, Options); 
            return true;
        }
        catch (Exception Ex) { Debug.WriteLine(Ex.ToString()); }

        return false;
    }

    public static byte[] SafeBufferCopy(this byte[] Buffer, int UsedLength)
    {
        // Make a copy of the buffer to prevent overwrite
        byte[] safeData = new byte[UsedLength];
        Array.Copy(Buffer, 0, safeData, 0, UsedLength);

        return safeData;
    }

    public static byte[] ToUTF8Byte(this string UTF8String) => Encoding.UTF8.GetBytes(UTF8String);
    public static string ToUTF8String(this byte[] UTF8Byte) => Encoding.UTF8.GetString(UTF8Byte);
    public static string ToUTF8String(this Span<byte> UTF8Byte) => Encoding.UTF8.GetString(UTF8Byte);


}
