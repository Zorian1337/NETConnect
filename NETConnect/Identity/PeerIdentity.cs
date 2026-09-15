using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETConnect.Identity;
public sealed class PeerIdentity
{
    public Guid PeerId { get; set; }
    public DateTime CreatedAt { get; set; }
}

