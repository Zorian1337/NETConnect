using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NETConnect.Network
{
    public class SSWTesting
    {
        void Main()
        {
            var Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IPv4);
            using (var SSW = new SecureSocketWrapper(Socket, Encryption.Crypt.RSAKeySize.HighSecurity))
            {
                



            }
        }
    }
}
