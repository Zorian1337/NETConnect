using NAudio.Wave;
using NETConnect;
using NETConnect.CustomConsole;
using NETConnect.Encryption.Crypt;
using NETConnect.MyExtensions;
using NETConnect.MyExtensions.Encryption;
using NETConnect.Peers;
using NETConnect.Shared.Multicast;
using NETConnect.Shared.Packet.Headers;
using System.Net;
using static NETConnect.Encryption.Crypt.RSACrypt;

namespace NETConnectAPP
{
    internal class Program
    {
        static async Task Main(string[] args)
        {

            // SECOND BRANCH PUSH TEST!
            Console.WriteLine("C# NETConnect");
            //Console.ReadKey();

            Peer peer = new Peer(IPAddress.Any, 0);

            while (true)
            {
                //var Helper = Client.Packer;
                //NETConnect.Audio.Audio.StartStreaming(ref Helper);
                //Thread.Sleep(1000);
                await Task.Delay(1);
            }


        }
    }
}
