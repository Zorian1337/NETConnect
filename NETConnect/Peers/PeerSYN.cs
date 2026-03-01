using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETConnect.Peers;

public class PeerSYN
{
    public PeerSYN() { }
    public List<PeerTable> DiscoveredPeers { get; set; }    
    public PeerTable MyTable { get; set; }

    public PeerSYN(PeerTable MyTable,  List<PeerTable> DiscoveredPeers)
    {
        var current = new PeerTable(MyTable.PeerId, MyTable.Address, MyTable.Port);
        current.NetStats = null;
        this.MyTable = current;

        this.DiscoveredPeers = DiscoveredPeers.Select(x =>
        {
            var table = new PeerTable(x.PeerId, x.Address, x.Port);
            table.NetStats = null;
            return table;
        }).ToList();
    }
}
