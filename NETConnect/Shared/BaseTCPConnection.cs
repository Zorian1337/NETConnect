using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETConnect.Shared;

public abstract class BaseTCPConnection
{
    public abstract bool SendUTF8(string Message, byte[] Buffer);

}
