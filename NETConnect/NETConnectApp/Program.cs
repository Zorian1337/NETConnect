using NETConnect.MyExtensions;
using NETConnect.Shared.Identity;
using NETConnect.Shared.Packet;
using NETConnect.Shared.Packet.Transport;
using NETConnect.Shared.Peers;
using NETConnect.Shared.Peers.PeerMerge;
using System.Net;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using System.Runtime.Intrinsics.Arm;
using System.Text;

namespace NETConnectApp;

internal class Program
{
    public static Peer Self { get; set; } = new Peer();

    static async Task Main(string[] args)
    {
        //var Peer = PeerIdentity.GetOrCreateExistingIdentityAsync();
        //PacketSYN syn = new PacketSYN(Peer, NETConnect.Shared.Platform.PeerPlatform.Windows);
        //Console.WriteLine(BitConverter.ToString(syn.ToBinary()));
        //return;

        //Self.InitPeer();
        //Task.Run(async () =>  await Self.StartServer());
        await Self.StartServer();

        // Keeps console open 
        for (; ; )
        {
            string input = Console.ReadLine();
            Self.LAN.Message(input);
            //Thread.Sleep(1000);
        }
    }
}
