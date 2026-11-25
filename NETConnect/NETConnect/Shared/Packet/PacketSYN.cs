using NETConnect.MyExtensions;
using NETConnect.Shared.Identity;
using NETConnect.Shared.IO.Encryption.Hash;
using NETConnect.Shared.Platform;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Channels;

namespace NETConnect.Shared.Packet;


public class PacketSYN
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PeerPlatform Platform { get; set; } = PeerPlatform.Windows;
    
    
    public byte[] IdentityKey { get; set; }
    public byte[] PublicKey { get; set; }
    public byte[] Signature { get; set; }

    public const int Size =
    4  +  // Platform
    91 +  // IdentityKey
    91 +  // PublicKey
    64;   // Signature
    //4 +  // PayloadLength
    //8 +  // SentAt
    //8 +  // PacketId
    //1 +  // Type
    //1 +  // Action
    //1 +  // Encoding
    //1 +  // Encryption
    //1 +  // Route
    //16 + // OriginPeerId
    //16 + // LastHopId
    //16 + // RecipientPeerId
    //1;   // TTL


    public PacketSYN() { }


    //public PacketSYN(PeerIdentity Identity)
    //{
    //    // EXPERIEMENTAL VERSION WHERE WE AUTO GENERATE DEVICE INFORMATION FOR LATER

    //    //PublicKey = Identity.GetPublicKey(); //- ONLY EXPOSE THE EPHEMERAL PUBLIC KEY
    //    //DetectPlatformInformation()
    //}

    //public PacketSYN(PeerPlatform platform, byte[] pubKey)
    //{
    //    Platform = platform;
    //    PublicKey = pubKey;
    //}

    //public PacketSYN(PeerIdentity Identity, PeerPlatform Platform)
    //{
    //    this.Platform = Platform;

    //    Console.WriteLine(Identity.ToIndentedJSON());
    //    //Console.WriteLine(BitConverter.ToString(Identity.IdentityKey));
    //    ECDsa IdentityKey = Identity.IdentityKey;
    //    SignedECDHKey Ephemeral = ECDSA_P256.GenerateSignedEphemeralPublicKey(Identity.IdentityKey);
    //    PublicKey = Ephemeral.PublicKey;
    //    Signature = Ephemeral.Signature;

    //    //Console.WriteLine($"EphemeralKey: {PublicKey.Length} = Sig: {Signature.Length}");

    //}

    public PacketSYN(PeerIdentity Identity, SignedECDHKey Ephemeral, PeerPlatform Platform)
    {
        IdentityKey = Identity.GetPublicKey();
        PublicKey = Ephemeral.PublicKey;
        Signature = Ephemeral.Signature;

        //Console.WriteLine($"EphemeralKey: {PublicKey.Length} = Sig: {Signature.Length}");
    }

    public PacketSYN(byte[] IdentityKey, SignedECDHKey Ephemeral, PeerPlatform Platform)
    {
        this.IdentityKey = IdentityKey;
        PublicKey = Ephemeral.PublicKey;
        Signature = Ephemeral.Signature;

        //Console.WriteLine($"EphemeralKey: {PublicKey.Length} = Sig: {Signature.Length}");
    }



    public static void DetectPlatformInformation()
    {

    }

    public byte[] ToBinary()
    {
        Span<byte> data = new byte[Size];

        int offset = 0;

        BinaryPrimitives.WriteUInt32LittleEndian(data, (uint)Platform);
        offset += 4;

        IdentityKey.CopyTo(data[offset..]);
        offset += 91;

        PublicKey.CopyTo(data[offset..]);
        offset += 91;

        Signature.CopyTo(data[offset..]);
        offset += 64;

        return data.ToArray();
    }

    public static PacketSYN FromBinary(byte[] SYNPacket)
    {
        if (SYNPacket is null || SYNPacket.Length < Size) return default;

        Span<byte> Packet = SYNPacket.AsSpan();

        int offset = 0;

        PacketSYN Syn = new PacketSYN();
        Syn.Platform = (PeerPlatform)BinaryPrimitives.ReadUInt32LittleEndian(Packet.Slice(offset, 4));
        offset += 4;

        Syn.IdentityKey = Packet.Slice(offset, 91).ToArray();
        offset += 91;

        Syn.PublicKey = Packet.Slice(offset, 91).ToArray();
        offset += 91;

        Syn.Signature = Packet.Slice(offset, 64).ToArray();
        offset += 64;

        return Syn;
    }

    public static bool TryFromBinary(Span<byte> Packet, out PacketSYN SynPacket)
    {
        SynPacket = default;

        if (Packet.Length < Size) return false;

        try
        {
            int offset = 0;

            PacketSYN Syn = new PacketSYN();
            Syn.Platform = (PeerPlatform)BinaryPrimitives.ReadUInt32LittleEndian(Packet.Slice(offset, 4));
            offset += 4;

            Syn.IdentityKey = Packet.Slice(offset, 91).ToArray();
            offset += 91;

            Syn.PublicKey = Packet.Slice(offset, 91).ToArray();
            offset += 91;

            Syn.Signature = Packet.Slice(offset, 64).ToArray();
            offset += 64;

            SynPacket = Syn;

            return true;
        }
        catch (Exception) { }// I dont care about exceptions rn 

        return false;
    }

    public static bool TryFromBinary(byte[] SYNPacket, out PacketSYN SynPacket)
    {
        SynPacket = default;

        if (SYNPacket is null || SYNPacket.Length < Size) return false;

        try
        {
            int offset = 0;
            Span<byte> Packet = SYNPacket.AsSpan();

            PacketSYN Syn = new PacketSYN();
            Syn.Platform = (PeerPlatform)BinaryPrimitives.ReadUInt32LittleEndian(Packet.Slice(offset, 4));
            offset += 4;

            Syn.IdentityKey = Packet.Slice(offset, 91).ToArray();
            offset += 91;

            Syn.PublicKey = Packet.Slice(offset, 91).ToArray();
            offset += 91;

            Syn.Signature = Packet.Slice(offset, 64).ToArray();
            offset += 64;

            SynPacket = Syn;

            return true;
        }
        catch (Exception) { }// I dont care about exceptions rn 

        return false;
    }
}
