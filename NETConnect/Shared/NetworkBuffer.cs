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

        // Used as network buffers
        public byte[] ReadBuffer = new byte[bufferSize];  // Used when trying to read socket data
        public byte[] WriteBuffer = new byte[bufferSize]; // Used when trying to send socket data

        // Used as serialization buffers
        public byte[] ReadUTF8Buffer = new byte[bufferSize]; // Used when converting Byte to UTF8  (From UTF8 to Byte)
        public byte[] WriteUTF8Bufer = new byte[bufferSize]; // Used when converting UTF8 to Byte (From Byte to UTF8)

        // Used as string buffers (byte conversions to string)
        public char[] ReadCharBuffer = new char[bufferSize]; 
        public char[] WriteCharBuffer = new char[bufferSize];
    }
}
