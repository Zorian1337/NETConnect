using NETConnect.MyExtensions;
using NETConnect.Shared.Packet.Headers;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace NETConnect.Shared.Packet;

// CREATED A ReceivedPacket CLASS SO I CAN DISPOSE OF THE BYTE ARRAY ALLOCATED FROM THE RECEIVE METHOD
// MADE IT GENERIC SO THAT I CAN STILL STORE HEADERS NO MATTER THE VERSION, THIS CAN STORE ALL OUR DATA AND REMOVE ANYTHING ALLOCATED ELSEWHERE
// NOTE: ArrayPool is new to me so I really have no clue what im doing
// NOTE: Few days after this isnt too bad, just learned about ArrayPool ownerships, its not that complicated just dont return the pool in the current function and pass it to another 
// Added support for both ArrayPools and Normal data to be safely disposed of, just need to set in the constructor whether or not the Array is a shared pool or is normal
// Months later coming back (I probably forgot most of ArrayPools shit but it isnt too complicated at least, I will have references to work with)
public sealed class ReceivedPacket<THeader> : IDisposable where THeader : class, IPacketHeaderIdentifier
{
    private byte[]? _FullPacket;
    private bool _disposed;

    public THeader Header { get; }
    public Memory<byte> FullPacket => _FullPacket != null ? _FullPacket[..(Header.HeaderLength + Header.PayloadLength)] : throw new ObjectDisposedException(nameof(ReceivedPacket<THeader>));
    public Memory<byte> HeaderBytes => FullPacket[..Header.HeaderLength];
    public Memory<byte> Payload => FullPacket[Header.HeaderLength..];

    public bool IsPooled { get; }

    public ReceivedPacket(byte[] Packet, THeader Header, bool IsPooled)
    {
        _FullPacket = Packet ?? throw new ArgumentNullException(nameof(Packet));
        this.Header = Header ?? throw new ArgumentNullException(nameof(Header));
        this.IsPooled = IsPooled;

        if(_FullPacket is null) Console.WriteLine("is null");
    }


    public Span<byte> GetFullPacketSpan() => FullPacket.Span;
    public Span<byte> GetHeaderSpan() => HeaderBytes.Span;
    public Span<byte> GetPayloadSpan() => Payload.Span;

    public byte[] GetPayloadCopy() => Payload.ToArray();
    public byte[] GetFullPacketCopy() => FullPacket.ToArray();
    public byte[] GetHeaderCopy() => HeaderBytes.ToArray();

    public void Dispose()
    {
        if (!_disposed && _FullPacket != null)
        {
            // ALWAYS SET THIS TO TRUE IF ITS FROM A POOL SOURCE, SO THAT THIS CAN BE RETURNED 
            if (IsPooled) ArrayPool<byte>.Shared.Return(_FullPacket);

            _FullPacket = null;
            _disposed = true;
        }
    }
}
