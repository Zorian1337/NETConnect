using NETConnect.Shared.Multicast;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETConnect.Peers;

public class Peer
{
    // So each peer is a server and a client

    // Locally clients will use udp multicast groups to look for groups

    public BaseTCPClient TCPClient { get; set; }
    public BaseTCPServer TCPCServer { get; set; }
    public Multicast Multicast { get; set; }


    public Peer()
    {
        // Create multicast to work immediately
        Multicast = new Multicast();
        Multicast.ReadMulticast();
    }


    //public void Start()
    //{


    //    // Host a server on the local port 

    //    //
    //}


}

