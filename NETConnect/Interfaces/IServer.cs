using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NETConnect.Interfaces
{
    public interface IServer
    {

        // All references to sockets need to be changed into a reference to the client itself 
        public event Action<Socket> OnClientConnected;
        public event Action<Socket> OnClientDisconnected;
        public event Action<ServerClientHandle, Span<byte>> OnDataReceived; 
    }
}
