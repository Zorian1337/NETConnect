using NETConnect.Shared.Packet.Headers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NETConnect.Shared.Packet;


public enum DeviceType
{
    Unknown = 0,        // default
    PC = 1,             // normal
    Mobile = 2,         // cant do currently
    Playstation4 = 3,   // cant do currently
    Playstation5 = 4,   // cant do currently
}

public class PacketSYN
{
    // THIS IS THE PACKET THAT WILL BE SENT ON FIRST CONNECT 
    // SHARING AUTHENTICATION DETAILS BEFORE ANY OTHER PACKET GETS SENT (READ BY THE SERVER)

    public PacketSYN() { }
    public PacketAuthentication Authentication { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DeviceType Device { get; set; } = DeviceType.Unknown;
    public string Name { get; set; } = "Unknown";
    public string OS { get; set; } = "Unknown";
    public PacketEncryption SupportedEncryption { get; set; } = PacketEncryption.RSA | PacketEncryption.ChaCha20Poly1305;


    public static byte[] GetFirstSYNPayload(string Name, string OS, DeviceType Device, byte[] RSAPubKey)
    {
        PacketSYN SYN = new PacketSYN();

        SYN.Authentication = new PacketAuthentication()
        {
            KeyData = RSAPubKey,
            EncryptionType = PacketEncryption.RSA
        };

        SYN.Device = Device;
        SYN.Name = Name;
        SYN.OS = OS;

        return SYN.ToJSON().ToUTF8Byte();
    }
}
