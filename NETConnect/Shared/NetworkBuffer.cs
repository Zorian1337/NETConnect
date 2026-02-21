using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETConnect.Shared
{
    // Creates an instance of a buffer per networking item
    public class NetworkBuffer
    {
        const int bufferSize = 64 * 1024; // 64 KB
        public byte[] UTF8Bufer = new byte[bufferSize];

        public byte[] ByteBuffer = new byte[bufferSize];

        public char[] CharBuffer = new char[bufferSize];

    }
}
