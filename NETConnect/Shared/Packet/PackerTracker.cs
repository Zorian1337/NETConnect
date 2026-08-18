using NETConnect.Peers;
using NETConnect.Shared.Packet.Headers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace NETConnect.Shared.Packet
{
    public record SeenPacket(Guid OriginPeerId, long SentAt, ulong PacketId);

    public class PackerTracker
    {
        private static readonly Dictionary<Guid, ulong> _packetCounters = new();
        private static readonly ConcurrentDictionary<(Guid PeerId, ulong PacketId), SeenPacket> seenPacket = new();


        private static readonly int _maxSize;
        //private static readonly TimeSpan _expiryTime = TimeSpan.FromMinutes(2);

        public static ulong NextPacketId(Guid peerId)
        {
            if (_packetCounters.TryGetValue(peerId, out ulong current))
            {
                current++;
                //Console.WriteLine($"{peerId}:{current}");
                _packetCounters[peerId] = current;
                return current;
            }

            _packetCounters[peerId] = 1;
            return 1;
        }

        public static bool IsPacketSeen(Guid OriginPeerId, ulong PacketId, long SentAt)
        {
            // REMOVE PACKETS OVER 60seconds 
            var ExpiredPackets = seenPacket.Where(x => GetCurrentAge(x.Value.SentAt) > 60 * 1000);

            // REMOVE INVALID PACKETS
            foreach (var ExpiredPacket in ExpiredPackets)
            {
                seenPacket.TryRemove(ExpiredPacket.Key, out _);
            }

            if (seenPacket.TryGetValue((OriginPeerId, PacketId), out SeenPacket? PacketData))
            {
                Console.WriteLine("packet was seen already");
                return true;
            }
            else
            {
                // ADD PACKET SAYING WE LOGGED IT
                seenPacket.TryAdd((OriginPeerId, PacketId), new SeenPacket(OriginPeerId, SentAt, PacketId));

                // RETURN THAT WE HAVENT SEEN THIS BEFORE
                return false;
            }
        }

        public static long GetCurrentAge(long SentAt)
        {
            long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long age = currentTime - SentAt;

            return age;
        }
    }
}
