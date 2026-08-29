using NETConnect.Shared.Packet;
using NETConnect.Shared.Packet.Headers;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace NETConnect.Shared
{
    // EACH CONNECTION TO GET ITS OWN ArrayPoolBuffer
    public class ArrayPoolBuffer : IDisposable
    {
        // seperate pool buffers in dicts by socket
        // set an expiry time to reset the buffer if clients/servers disconnect and they arent detected

        public static ConcurrentDictionary<IntPtr, ArrayPoolBuffer> ExistingPools = new ConcurrentDictionary<IntPtr, ArrayPoolBuffer>();
       
        private readonly Socket _socket;
        private readonly ArrayPool<byte> _pool = ArrayPool<byte>.Shared;
        private readonly byte[] _buffer;

        private const int RECEIVE_BUFFER_SIZE = 65536; // 64KB - 
        //private const ushort MAGIC = PacketHeader.MAGIC;
        //private const int MAX_MESSAGE_SIZE = 10 * 1024 * 1024;

        public CancellationTokenSource CancellationTokenSource { get; private set; }
        private bool _disposed = false;

        public ArrayPoolBuffer(Socket socket)
        {
            _socket = socket;
            _buffer = _pool.Rent(RECEIVE_BUFFER_SIZE);
            CancellationTokenSource = new CancellationTokenSource();

            // Dispose if we have duplicates of this socket
            if (!ExistingPools.TryAdd(_socket.Handle, this)) 
            { 
                Dispose();
            }
        }

        public static ArrayPoolBuffer GetNewOrExistingArrayPool(Socket socket)
        {
            if(ExistingPools.TryGetValue(socket.Handle, out ArrayPoolBuffer pool)) return pool;
            else return new ArrayPoolBuffer(socket);
        }


        public void Dispose()
        {
            if (_disposed) return;

            ExistingPools.Remove(_socket.Handle, out _);

            _pool.Return(_buffer);
            _disposed = true;
        }
    }
}
