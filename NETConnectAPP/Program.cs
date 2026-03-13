using NAudio.Wave;
using NETConnect;
using NETConnect.CustomConsole;
using NETConnect.Encryption.Crypt;
using NETConnect.MyExtensions;
using NETConnect.MyExtensions.Encryption;
using NETConnect.Peers;
using NETConnect.Shared.Multicast;
using System.Net;
using static NETConnect.Encryption.Crypt.RSACrypt;

namespace NETConnectAPP
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //RSAExport Keys = RSACrypt.CreateExport(RSAKeySize.VerySecure);
            //string t = "this is a test";

            //byte[] Encrypted = t.ToUTF8Byte().EncryptRSA(Keys.PublicKey);
            //Console.WriteLine(Convert.ToBase64String(Encrypted));
            //byte[] decrypted = Encrypted.DecryptRSA(Keys.PrivateKey);
            //Console.WriteLine(decrypted);


            // SECOND BRANCH PUSH TEST!
            Console.WriteLine("C# NETConnect");
            Console.ReadKey();

            Peer peer = new Peer(IPAddress.Any, 0);

            while (true)
            {
                //var Helper = Client.Packer;
                //NETConnect.Audio.Audio.StartStreaming(ref Helper);
                Thread.Sleep(1000);
            }


        }
    }
}
