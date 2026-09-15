using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETConnect.Identity;

public sealed class IdentityStore
{
    public PeerIdentity GetOrCreate()
    {
        return default;
    }

    public PeerIdentity? Load()
    {
        return default;
    }

    public void Save()
    {

    }
}
