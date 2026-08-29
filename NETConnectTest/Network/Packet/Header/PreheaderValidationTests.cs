using NETConnect.MyExtensions;
using NETConnect.Shared.Packet.Headers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETConnectTest.Network.Packet.Header;

public class PreheaderValidationTests
{
    // BUILD A BASIC TEST HEADER
    public static PacketHeader _header = new PacketHeader("This payload is here as a test".ToUTF8Byte(), PacketType.Data, PacketAction.NONE, PacketEncoding.NONE, PacketEncryption.NONE, PacketRoute.Direct, Guid.Empty, Guid.Empty, null, 7);

    public static byte[] _Header = _header.ToBinaryHeader();

    public static byte[] _Preheader = _Header[..IPacketHeaderIdentifier.PreheaderLength];

    [Fact]
    public void IsValidHeader_WithPresetValues_ReturnsTrue()
    {
        bool isValid = IPacketHeaderIdentifier.IsValidHeader(_Preheader, PacketHeader.MAGIC);
        Assert.True(isValid);
    }


    public static IEnumerable<object[]> GetCustomPreheaderTests_Success()
    {
        yield return new object[] { ((IPacketHeaderIdentifier)_header).ToPreheaderBinary() };
        yield return new object[] { ((IPacketHeaderIdentifier)new PacketHeader("1234".ToUTF8Byte(), PacketType.Data, PacketAction.NONE, PacketEncoding.NONE, PacketEncryption.RSA, PacketRoute.Direct, Guid.Empty, Guid.Empty, null, 7)).ToPreheaderBinary() };
        yield return new object[] { new byte[16] { 0x43, 0x4E, 1, 1, 1, 1, 1, 0, 00, 00, 00, 80, 0x0C, 94, 93, 01 } }; 
    }

    public static IEnumerable<object[]> GetCustomPreheaderTests_Failure()
    {
        // Fails due to empty data
        yield return new object[] { new byte[IPacketHeaderIdentifier.PreheaderLength] };
        yield return new object[] { new byte[15] { 0x4E, 0, 0, 0, 0, 0, 0, 00, 00, 00, 80, 0x0C, 94, 93, 01 } }; // Fails on invalid size
        yield return new object[] { new byte[IPacketHeaderIdentifier.PreheaderLength] { 0x64, 0xdd, 0, 0, 0, 0, 0, 0, 00, 00, 00, 80, 0x0C, 94, 93, 01 } }; // Fails due to incorrect magic
        yield return new object[] { new byte[1] { 0 } }; // Fails on invalid size
    }

    [Theory]
    [MemberData(nameof(GetCustomPreheaderTests_Success))]
    public void IsValidHeader_WithCustomPacketHeader_ReturnsTrue(byte[] Preheader) //IPacketHeaderIdentifier Header
    {
        bool isValid = IPacketHeaderIdentifier.IsValidHeader(Preheader, PacketHeader.MAGIC); // I need to make sure I have the default magic for this
        Assert.True(isValid);
    }

    [Theory]
    [MemberData(nameof(GetCustomPreheaderTests_Failure))]
    public void IsValidHeader_WithCustomPacketHeader_ReturnsFalse(byte[] Preheader) //IPacketHeaderIdentifier Header
    {
        bool isValid = IPacketHeaderIdentifier.IsValidHeader(Preheader, PacketHeader.MAGIC); // I need to make sure I have the default magic for this
        Assert.False(isValid);
    }
}
