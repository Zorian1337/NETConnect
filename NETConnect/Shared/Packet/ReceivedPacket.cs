using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace NETConnect.Shared.Packet
{
    // CREATED A RECEIVEDPACKET CLASS SO I CAN DISPOSE OF THE BYTE ARRAY ALLOCATED FROM THE RECEIVE METHOD
    // MADE IT GENERIC SO THAT I CAN STILL STORE HEADERS NO MATTER THE VERSION, THIS CAN STORE ALL OUR DATA AND REMOVE ANYTHING ALLOCATED ELSEWHERE
    // NOTE: ArrayPool is new to me so I really have no clue what im doing
    public sealed class ReceivedPacket<THeader> : IDisposable where THeader : class
    {
        private byte[]? _FullPacket;
        private bool _disposed;

        public THeader Header { get; }
        public Memory<byte> FullPacket { get; }
        public Memory<byte> HeaderBytes { get; }
        public Memory<byte> Payload { get; }

        public ReceivedPacket(byte[] Packet, THeader Header, int HeaderLength)
        {
            _FullPacket = Packet ?? throw new ArgumentNullException(nameof(Packet));
            this.Header = Header ?? throw new ArgumentNullException(nameof(Header));

            // Create spans once
            FullPacket = Packet.AsMemory();
            HeaderBytes = Packet.AsMemory(0, HeaderLength);
            Payload = Packet.AsMemory(HeaderLength, (Packet.Length-HeaderLength));
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
                ArrayPool<byte>.Shared.Return(_FullPacket);
                _FullPacket = null;
                _disposed = true;
            }
        }
    }
}
