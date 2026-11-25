using NETConnect.MyExtensions;
using NETConnect.Shared.Identity;
using NETConnect.Shared.IO.Encryption.Hash;
using NETConnect.Shared.Packet.Headers;
using NETConnect.Shared.Peers.PeerMerge;
using NETConnect.Shared.Platform;
using System;
using System.Collections.Generic;

using System.Net.Sockets;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;
using System.Text;


namespace NETConnect.Shared.Packet.Transport.Security;

public sealed class Handshake
{
    //public 

    public static async Task EstablishSecureServerChannelAsync(PeerServerBase Server, Socket Client, CancellationToken ct)
    {
        // CHECK THAT THE SERVER ISNT ALREADY AUTHENTICATED WITH THIS CLIENT 
        // THIS INCLUDES ALREADY BEING AUTHENTICATED*
        // WE DONT WANT TO DO THIS PROCESS MORE THAN ONCE IF ITS NOT NEEDED.

        PeerIdentity Identity = Server.Self.Identity;
        Console.WriteLine($"[SERVER] [TCP-AUTH] [START]");
        bool IsAuthenticated = false;

        Guid clientId = Guid.Empty;
        byte[] remoteIdentityKey = Array.Empty<byte>();
        byte[] remotePublicKey = Array.Empty<byte>();
        byte[] remoteSignature = Array.Empty<byte>();
        byte[] secret = Array.Empty<byte>();
        PeerPlatform Platform = PeerPlatform.Unknown;

        PeerServerClient sClient = new PeerServerClient(ref Server, new TCPTransport(Client), ct);

        while (!IsAuthenticated)
        {
            using (var Packet = await Client.ReceiveTCPPacketAsync())
            {
                if (Packet is null || !(Packet.Header is PacketHeader pHeader) || pHeader.Type != PacketType.Control) continue; //

                if(clientId == Guid.Empty) clientId = pHeader.OriginPeerId;
                Console.WriteLine($"[SERVER] [TCP-AUTH] [{pHeader.Action}] -> {Packet.ToIndentedJSON()}");

                switch (pHeader.Action)
                {
                    case PacketAction.SYN:
                        if(!PacketSYN.TryFromBinary(Packet.GetPayloadSpan(), out PacketSYN pSyn))
                        {
                            Console.WriteLine("[SERVER] [TCP-AUTH] -> SYNPacket is invalid.");
                            continue;
                        }

                        Console.WriteLine($"[SERVER] [TCP-AUTH] [SYN-DECODE]-> {pSyn.ToIndentedJSON()}");
                        remoteIdentityKey = pSyn.IdentityKey;
                        remotePublicKey = pSyn.PublicKey;
                        remoteSignature = pSyn.Signature;
                        Platform = pSyn.Platform;

                        Console.WriteLine($"[SERVER] [TCP-EPH] -> \n{BitConverter.ToString(remotePublicKey)}\n{BitConverter.ToString(remoteSignature)}");
                        
                        if(!ECDSA_P256.IsValidSignature(remoteIdentityKey, remotePublicKey, remoteSignature))
                        {
                            Console.WriteLine("remote key data isnt valid");
                            continue; // THIS NEEDS HANDLED AS IT'LL JUST SIT HERE LOOPING
                        }

                        try
                        {
                            SignedECDHKey signedKey = Identity.GenerateSignedEphemeralPublicKey(out ECDiffieHellman PublicKeyEPH);

                            secret = Identity.GenerateSharedSecretECCDH(remotePublicKey);
                            Console.WriteLine($"[SERVER] [SECRET] -> {BitConverter.ToString(secret)}");

                            PacketSYN Syn = new PacketSYN(Identity, signedKey, PeerPlatform.Windows);
                            await sClient.SendAsync(true, ct, Syn.ToBinary(), PacketType.Control, PacketAction.SYNACK);
                        }
                        catch (Exception Ex) { Console.WriteLine(Ex.ToString()); }

                        
                        //Console.WriteLine("[SERVER] SENT BACK SYN (IT SHOULD SEND BACK SYNACK)");
                        break;
                    case PacketAction.SYNACK:
                        break;
                    case PacketAction.ACK:
                        break;
                    case PacketAction.READY:
                        break;
                }
            }
        }
    }


    //<SecureChannelResult>?
    public static async Task EstablishSecureClientChannelAsync(PeerClientBase Client, CancellationToken ct) //, 
    {
        // BEFORE CLIENT SENDS SYN TO SERVER
        // THE CLIENT NEEDS TO REFLECT ONTO SERVER PART
        // REFLECTION WILL LOOK FOR ALREADY EXCHANGED KEYS BETWEEN THE SERVER/CLIENT
        // RATHER THAN MAKING NEW ONES, IF ITS ALREADY AUTHENTICATED ON ONE END USE ITS KEYS.

        PeerIdentity Identity = Client.Self.Identity;
        Console.WriteLine($"[CLIENT] [TCP-AUTH] [START]");
        bool IsAuthenticated = false;

        Guid clientId = Guid.Empty;
        byte[] remoteIdentityKey = Array.Empty<byte>();
        byte[] remotePublicKey = Array.Empty<byte>();
        byte[] remoteSignature = Array.Empty<byte>();
        byte[] secret = Array.Empty<byte>();
        PeerPlatform Platform = PeerPlatform.Unknown;

        // CONNECTED TO SERVER, SO THATS THE SOCKET.
        Socket Server = Client.Tcp.GetSocket();
        SignedECDHKey signedKey = Identity.GenerateSignedEphemeralPublicKey(out ECDiffieHellman PublicKeyEPH);

        try
        {
            PacketSYN Syn = new PacketSYN(Identity, signedKey, PeerPlatform.Windows);
            await Client.SendAsync(true, ct, Syn.ToBinary(), PacketType.Control, PacketAction.SYN);
        }
        catch (Exception ex) { Console.WriteLine(ex.ToString()); }
        
        Console.WriteLine("[CLIENT] -> SENT SYN");
        while (!IsAuthenticated)
        {
            using (var Packet = await Server.ReceiveTCPPacketAsync())
            {
                Console.WriteLine(Packet is null);
                if (Packet is null || !(Packet.Header is PacketHeader pHeader) || pHeader.Type != PacketType.Control) continue;

                if (clientId == Guid.Empty) clientId = pHeader.OriginPeerId;
                Console.WriteLine($"[CLIENT] [TCP-AUTH] [{pHeader.Action}]");

                switch (pHeader.Action)
                {
                    //case PacketAction.SYN:
                    //    Console.WriteLine("[SERVER] SENT BACK SYN (IT SHOULD SEND BACK SYNACK)");
                    //    break; // - WE WONT NEED SYN
                    case PacketAction.SYNACK:
                        // DECODE SYN PACKET SENT FROM SERVER FOR ITS INFORMATION
                        if (!PacketSYN.TryFromBinary(Packet.GetPayloadSpan(), out PacketSYN pSyn))
                        {
                            Console.WriteLine("[CLIENT] [TCP-AUTH] -> SYNPacket is invalid.");
                            continue;
                        }

                        Console.WriteLine($"[CLIENT] [TCP-AUTH] [SYN-DECODE]-> {pSyn.ToIndentedJSON()}");
                        remoteIdentityKey = pSyn.IdentityKey;
                        remotePublicKey = pSyn.PublicKey;
                        remoteSignature = pSyn.Signature;
                        Platform = pSyn.Platform;

                        Console.WriteLine($"[CLIENT] [TCP-EPH] -> \n{BitConverter.ToString(remotePublicKey)}\n{BitConverter.ToString(remoteSignature)}");

                        if (!ECDSA_P256.IsValidSignature(remoteIdentityKey, remotePublicKey, remoteSignature))
                        {
                            Console.WriteLine("remote key data isnt valid");
                            continue; // THIS NEEDS HANDLED AS IT'LL JUST SIT HERE LOOPING
                        }

                        try
                        {
                            secret = Identity.GenerateSharedSecretECCDH(remotePublicKey);
                            Console.WriteLine($"[CLIENT] [SECRET] -> {BitConverter.ToString(secret)}");
                        }
                        catch (Exception Ex) { Console.WriteLine(Ex.ToString()); }



                        break;
                    case PacketAction.ACK:
                        break;
                    case PacketAction.READY:
                        break;
                }
            }
        }


        // ECDH exchange over client.Tcp, HKDF -> client.Crypto.SendKey/ReceiveKey
        return;
    }

}
