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
            
            Peer peer = new Peer(IPAddress.Any, 0);

            while (true)
            {
                //var Helper = Client.Packer;
                //NETConnect.Audio.Audio.StartStreaming(ref Helper);
                Thread.Sleep(1000);



                //ConsoleKeyInfo Key = Console.ReadKey(true);
                //if (Key.Key == ConsoleKey.Tab)
                //{
                //    //// Take current Top Location
                //    //int CurrentTop = Console.CursorTop;
                    
                //    //if (Console.CursorLeft != 0)
                //    //{
                //    //    Console.CursorLeft = 0;
                //    //    Console.CursorTop = CurrentTop + 1;
                //    //}

                //    //int NewTop = Console.CursorTop;
                //    //int NewLeft = Console.CursorLeft;
                //    //Console.Write("Your Message: ");

                //    ConsoleDebugging.Print(new ConsoleDebugging.ConsoleBufferItem(Console.BufferHeight - 1, Console.CursorLeft, "Your Message: ", ConsoleDebugging.ConsoleBufferReturnPosition.NewPosition, () =>
                //    {
                //        string Type = Console.ReadLine();
                //        ConsoleDebugging.Print(Type);
                //    }));

                //    string Message = Console.ReadLine();
                //    peer.Multicast.SendUTF8Message(Message, MulticastAction.Data);
                //}



                //peer.Multicast.SendMessage(Hash, MulticastAction.Data);
            }


        }
    }
}
