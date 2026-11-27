using NETConnect.Shared.Packet.Headers;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;

namespace NETConnect.Shared.IO;

// POTENTIAL CLASS FOR SAVING AND REUSING FIXED OFFSETS
// HARD CODED DYNAMICALLY PULLED

public record FixedOffset(string Name, object Type, int Length, bool Optional = false); //int Offset, 

public class FixedOffsets
{
    public Dictionary<string, FixedOffset[]> FixedArrayOffsets { get; set; } = new Dictionary<string, FixedOffset[]>();

    public string Name { get; set; }
    public FixedOffset[] Items { get; set; }

    public int SelectedItemIndex { get; set; } = 0;
    public FixedOffset SelectedOffset { get; set; } 

    public int GetMaxItemIndex => Items.Length-1;
    public (long Min, long Max)? GetOffsetRange()
    {
        if (Items is null || Items.Length == 0) return (0, 0);

        int Length = Items.Select(x => x.Length).Sum();
        return (0, Length);
    }

    public FixedOffsets(string Name, params FixedOffset[] Items)
    {
        this.Name = Name;
        this.Items = Items;

        FixedArrayOffsets.TryAdd(Name, Items);
    }

    public byte[] ToBinary()
    {
        (long Min, long Max)? Range = GetOffsetRange();
        Console.WriteLine($"Range -> {Range.Value.Min}:{Range.Value.Max}");

        long Size = Range.Value.Max;
        Span<byte> data = new byte[Size];

        int offset = 0;
        foreach(var item in Items)
        {
            Type currentType = item.Type.GetType();
            string lower = currentType.Name.ToLower();

            //Console.WriteLine($"{item.Name} - {lower} - Length: {item.Length} - Offset: {offset}");

            switch (lower)
            {
                //
                case "byte": data[offset] = (byte)item.Type; break;
                case "uint16": BinaryPrimitives.WriteUInt16LittleEndian(data[offset..], (ushort)item.Type); break;
                case "int32": BinaryPrimitives.WriteInt32LittleEndian(data[offset..], (int)item.Type); break;
                case "int64": BinaryPrimitives.WriteInt64LittleEndian(data[offset..], (long)item.Type); break;
                case "uint64": BinaryPrimitives.WriteUInt64LittleEndian(data[offset..], (ulong)item.Type); break;

                //
                case "guid": ((Guid)item.Type).TryWriteBytes(data[offset..]); break;

                default: Console.WriteLine($"Missing binary implementation for \n -> {item.Name} - {lower} - Length: {item.Length} - Offset: {offset}"); break;
            }

            offset += item.Length;
        }

        return data.ToArray();
    }

}


