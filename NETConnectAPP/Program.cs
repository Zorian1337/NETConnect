using NETConnect.MyExtensions;
using NETConnect;

using System.Net;

namespace NETConnectAPP
{
    internal class Program
    {
        static void Main(string[] args)
        {

            // Start a NETConnect server
            BaseTCPServer Server = new BaseTCPServer(IPAddress.Any, 5000);
            BaseTCPClient Client = new BaseTCPClient();

            Task.Run(() => Server.StartServer());


            Thread.Sleep(1000);
            Task.Run(() => 
            {
                if (Client.TryConnect("127.0.0.1", 5000))
                {
                    Console.WriteLine("(NETApp)");
                    //Client.SocketClient.Send("I have connected this is a message test!".UTF8StringToUTF8Byte(Client.NetworkBuffer.ByteBuffer));
                }
            });





            while (true) { Thread.Sleep(1000); }



        }
    }
}
