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

        private const int RECEIVE_BUFFER_SIZE = 65536; // 64KB
        private const ushort MAGIC = PacketHeader.MAGIC;
        private const int MAX_MESSAGE_SIZE = 10 * 1024 * 1024;

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

        public async Task ReceiveAsync()
        {
            try
            {
                while (!CancellationTokenSource.IsCancellationRequested && !_disposed && _socket.Connected)
                {
                    int bytesRead = await _socket.ReceiveAsync(_buffer, SocketFlags.None);

                    if (bytesRead <= 0)
                    {
                        Console.WriteLine("Remote disconnect");
                        break;
                    }

                    // DATA COPY
                    byte[] _received = new byte[bytesRead];
                    Array.Copy(_buffer, 0, _received, 0, bytesRead);

                    // VALIDATE PACKET HEADER BY LENGTH, OUTPUT MUTLIPLE
                    ValidatePackets(_received);
                }
            }
            catch(Exception Ex) 
            {
                Console.WriteLine(Ex.ToString());
                Dispose();
            }



        }

        public void ValidatePackets(byte[] Data)
        {
            using (var stream = new MemoryStream(Data))
            using (var reader = new BinaryReader(stream))
            {
                // MAKE A LOOP THAT GOES THROUGH OUR DATA
                while (stream.Position < stream.Length)
                {
                    byte[] preheader = reader.ReadBytes(IPacketHeaderIdentifier.PreheaderLength);
                    if (!IPacketHeaderIdentifier.IsValidHeader(preheader, out (ushort Magic, byte Version, byte HeaderLength, int PayloadLength, long SentAt) info))
                    {
                        stream.Position -= 7; // resets but stays one ahead
                        Debug.WriteLine("[DEBUG] Failed to validate header");
                        continue;
                    }

                    // CHECK IF ARRAY CONTAINS ALLOF THIS HEADER AND ITS PAYLOAD
                    int RestOFPacketSize = (info.HeaderLength- IPacketHeaderIdentifier.PreheaderLength) + info.PayloadLength;

                    if (!(stream.Position + RestOFPacketSize <= stream.Length))
                    {
                        // NOT ENOUGH ROOM TO CAPTURE THE COMPLETE PACKET
                        // WE'LL NEED TO DO ANOTHER RECEIVE FOR THIS 
                        // LEAVE THIS FOR NOW TO LATER DO IT 
                        // NOTE: probably just have an if and an else that has a loop waiting for the amount of data to exist

                        Debug.WriteLine("[DEBUG]:ValidatePackets -> PACKET CANNOT BE FULLY CAPTURED IN ONE RECEIVE METHOD");
                        continue;
                    }

                    // GRABS OUR DATA AND CREATES IT INTO A PACKET FOR US TO HANDLE

                    byte[] RestOFHeader = reader.ReadBytes(info.HeaderLength - IPacketHeaderIdentifier.PreheaderLength);
                    byte[] FullHeader = new byte[info.HeaderLength]; // Contains preheader and data after it

                    Array.Copy(preheader, 0, FullHeader, 0, preheader.Length);
                    Array.Copy(RestOFHeader, 0, FullHeader, IPacketHeaderIdentifier.PreheaderLength, RestOFHeader.Length);

                    byte[] Payload = reader.ReadBytes(info.PayloadLength);

                    Debug.WriteLine($"[DEBUG]::ValidatePackets -> \nFullHeader: {BitConverter.ToString(FullHeader)}\n\nPayload: {BitConverter.ToString(Payload)}");

                    // GET HEADER TYPE BASED ON VERSION FOR RECEIVED PACKET
                    //IPacketHeaderIdentifier Header = IPacketHeaderIdentifier.GetPacketHeaderType(info.Version);
                    
                    //ReceivedPacket<nameof(Header)>()
                }
            }


            

            

            // 
        }

        //public async Task<ReceivedPacket<IPacketHeaderIdentifier>> 

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
