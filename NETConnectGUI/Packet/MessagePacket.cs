using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETConnectGUI.Packet;

public class MessagePacket
{
    public string Author { get; set; }
    public string Message { get; set; }
    public DateTime SentAt { get; set; }

    public MessagePacket() { }

    public MessagePacket(string author, string message) { 
        SentAt = DateTime.UtcNow;
        Author = author;
        Message = message;
    }
}
