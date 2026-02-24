using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NETConnect.Shared;

public class NetworkUtils
{

    public static IPAddress GetLocalLanIp()
    {
        // Try interfaces with a default gateway (usually connected to LAN)
        var ip = NetworkInterface.GetAllNetworkInterfaces()
            .Where(ni => ni.OperationalStatus == OperationalStatus.Up &&
                         ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .SelectMany(ni => ni.GetIPProperties().UnicastAddresses
                .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork &&
                            IsPrivateIPv4(a.Address)))
            .FirstOrDefault(a => niHasGateway(a));

        if (ip != null)
            return ip.Address;

        // Fallback: any private IPv4 on an active interface
        ip = NetworkInterface.GetAllNetworkInterfaces()
            .Where(ni => ni.OperationalStatus == OperationalStatus.Up &&
                         ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .SelectMany(ni => ni.GetIPProperties().UnicastAddresses
                .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork &&
                            IsPrivateIPv4(a.Address)))
            .FirstOrDefault();

        if (ip != null)
            return ip.Address;

        throw new InvalidOperationException("No local LAN IPv4 address found.");
    }

    private static bool IsPrivateIPv4(IPAddress ip)
    {
        var b = ip.GetAddressBytes();
        return b[0] == 10 ||
               (b[0] == 172 && b[1] >= 16 && b[1] <= 31) ||
               (b[0] == 192 && b[1] == 168);
    }

    private static bool niHasGateway(UnicastIPAddressInformation ipInfo)
    {
        var ni = NetworkInterface.GetAllNetworkInterfaces()
            .FirstOrDefault(n =>
                n.GetIPProperties().UnicastAddresses.Contains(ipInfo));

        return ni != null &&
               ni.GetIPProperties().GatewayAddresses.Any(g => g.Address.AddressFamily == AddressFamily.InterNetwork);
    }

}
