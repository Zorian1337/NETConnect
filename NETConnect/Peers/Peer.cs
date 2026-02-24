using NETConnect.Shared.Multicast;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NETConnect.Peers;

public class Peer
{
    // So each peer is a server and a client

    // Locally clients will use udp multicast groups to look for groups

    // List of clients inside the peer so that each peer can be connected to others
    public List<BaseTCPClient> Clients { get; set; } = new List<BaseTCPClient>();
    public BaseTCPServer TCPServer { get; set; }
    public Multicast Multicast { get; set; }


    public Peer(IPAddress Address, int Port)
    {
        // Join multicast group immediately, then later scout for information (peer related)
        //Multicast = new Multicast();
        //Multicast.ReadMulticast(); // Scout for other peers on the network for our TCPClient to connect to (data exchange) - might need to rework some stuff later regarding this

        // Init our server/client
        //TCPClient = new BaseTCPClient();  
        var Self = this;
        TCPServer = new BaseTCPServer(ref Self, Address, Port);

        // Start our server, as having multicast up and our TCPServer is the most important (client is used to connect to other Peer Servers) - might need to change some plans around later 
        TCPServer.StartServer();
    }


    //public void Start()
    //{


    //    // Host a server on the local port 

    //    //
    //}


}

