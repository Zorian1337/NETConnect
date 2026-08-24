using NETConnect.Encryption.Crypt;
using NETConnect.MyExtensions;
using NETConnect.MyExtensions.Encryption;
using NETConnect.Peers;
using NETConnect.Shared.Packet.Headers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace NETConnect.Shared.Packet;

public class PacketHelper
{
    // Store some stuff within this helper to allow us to skip reusing it


    public Socket Connection { get; private set; }
    //public NetworkBuffer Buffers { get; private set; }
    public Peer Self {  get; private set; }
    public ServerClientHandle ClientHandle { get; private set; }

    /// <summary>
    /// This token will only directly be for current instance
    /// </summary>
    public CancellationTokenSource Token { get; private set; }

    // This can be later named to IdentityKeys, as the keys are different per identity rather than just seperate encryption keys
    public SecurityKey EncryptionKeys { get; private set; }


    //public Action onAuthenticated { get; set; }
    public bool IsAuthenticated { get; set; } = false;
    public bool IsAuthenticating { get; set; } = false;

    /// <summary>
    /// used for the client version
    /// </summary>
    /// <param name="Connection"></param>
    /// <param name="Buffers"></param>
    /// <param name="Token"></param>
    public PacketHelper(ref Socket Connection, ref Peer Self, ref CancellationTokenSource Token)
    {
        // THIS IS FOR THE CLIENT

        this.Self = Self;
        this.Connection = Connection;
        this.Token = Token; // WAS MISSING

        // PRETTY SURE THIS IS SET APON CONNECTION
        this.EncryptionKeys = new SecurityKey(); 
    }

    public bool IsServer()
    {
        if (ClientHandle is not null) return true;
        else return false;
    }

    /// <summary>
    /// Used for the server version
    /// </summary>
    /// <param name="Connection"></param>
    /// <param name="Buffers"></param>
    /// <param name="ClientHandle"></param>
    public PacketHelper(ref Socket Connection, ref Peer Self, ref ServerClientHandle ClientHandle, ref CancellationTokenSource Token)
    {
        // THIS IS FOR THE SERVER 

        this.Self = Self;
        this.Connection = Connection;
        //this.Buffers = Buffers;
        this.ClientHandle = ClientHandle;
        this.Token = Token; // WAS MISSING

        // Server will be incharge of the type of encryption used, so safe to generate here...
        RSAKeySize KeySize = RSAKeySize.Minium;
        this.EncryptionKeys = new SecurityKey(KeySize, RSACrypt.CreateExport(KeySize));
    }

    /// <summary>
    /// Relays the packet the same way it was received but slightly modified
    /// </summary>
    /// <param name="Packet"></param>
    /// <returns></returns>
    public int Forward(byte[] Packet) => Connection.Send(Packet);

    // this is for sending data directly and packaging it while sending it automatically, it cannot be used for packet forwarding as it tries to do everything over again
    public int SendPacket(byte[] Payload, PacketType Type, PacketAction Action, PacketEncoding Encoding, PacketEncryption Encryption, PacketRoute Route, Guid? RecipientPeerId = null)
    {
        // IF WE HAVE NO CLIENTS OR PEERS DROP THIS PACKET SENDING AS ERROR 
        //if (!(Self.ConnectedPeers.Count() > 0 || Self.TCPServer.Clients.Count() > 0)) return -1; // Disabled as this wont let our first packet through

        // DO EVERYTHING IN THIS ONE FUNCTION

        // - BUILD HEADER - PAYLOAD LENGTH WOULD NEED RECALCULATED AFTER ENCRYPTED
        PacketHeader header = new PacketHeader(Payload, Type, Action, Encoding, Encryption, Route, Self.PeerId, Self.PeerId, RecipientPeerId, 7);
        //Console.WriteLine($"[DEBUG]:SendPacket:Header -> \n{header.ToJSON(new System.Text.Json.JsonSerializerOptions() { WriteIndented = true})}");

        // -- ENCRYPT DATA IF NEED BE
        // IF ENCRYPTION ENABLED AND RETURNS A NEGATIVE OR NULL VALUE RETURN -1
        byte[] Encrypted = [];
        if (Encryption != PacketEncryption.NONE)
        {
            // THIS WAS ORIGINALLY WORKING NOW CLIENT CHACHAKEYS ARENT VALID ?
            // THE SCOPE OF THIS IS A BIT CONFUSING, THE SERVER HAS THE KEYS BUT IS IT PULLING FROM THE RIGHT PACKER?

            //Self.TCPServer.InvokeDebugMessage($"ChaChaKey: {EncryptionKeys.ChaChaKey.Length}");
            //if (EncryptionKeys.ChaChaKey is null) Self.TCPServer.InvokeDebugMessage($"ChaChaKey: reported null - IsServer: {IsServer()}");
            //else Self.TCPServer.InvokeDebugMessage($"ChaChaKey: reported not null - IsServer: {IsServer()}");
            switch (Encryption)
            {
                //case PacketEncryption.AES: break;
                case PacketEncryption.RSA: Encrypted = PacketEncrypted.EncryptUT8Bytes(Payload, EncryptionKeys.RemoteRSAPubKey, PacketEncryption.RSA); break;
                case PacketEncryption.ChaCha20Poly1305: Encrypted = PacketEncrypted.EncryptUT8Bytes(Payload, EncryptionKeys.ChaChaKey, PacketEncryption.ChaCha20Poly1305);  break; //Self.TCPServer.InvokeDebugMessage($"EncryptedData: {Encrypted?.Length} - ChaChaKey: {EncryptionKeys.ChaChaKey.Length}");
            }

            if (Encrypted is null || Encrypted.Length == 0)
            {
                // IF PACKET IS NOT ENCRYPTED THEN IT RUINS OUR PACKET
                //Self.TCPServer.InvokeDebugMessage("encryption is null");

                //Self.TCPServer.InvokeDebugMessage($"Payload: {BitConverter.ToString(Payload)}");
                byte[] test = PacketEncrypted.EncryptUT8Bytes(Payload, EncryptionKeys.ChaChaKey, PacketEncryption.ChaCha20Poly1305);
                //Self.TCPServer.InvokeDebugMessage($"TestEncryption: {test.Length} : {IsServer()}");
                return -1;
            }
            else header.PayloadLength = Encrypted.Length;
        } // DO THIS LATER SO WE CAN MAKE SURE EVERYTHING AT LEAST WORKS 

        // --- FINALIZE DATA TO BE SENT
        byte[] Finalized = new byte[header.HeaderLength + header.PayloadLength];
        byte[] HeaderArray = header.ToBinaryHeader();
        HeaderArray.CopyTo(Finalized, 0);

        if ((Encrypted is null || Encrypted.Length == 0))
        {
            //Self.TCPServer.InvokeDebugMessage("using normal payload");
            Finalized = new byte[header.HeaderLength + header.PayloadLength];
            HeaderArray = header.ToBinaryHeader();
            HeaderArray.CopyTo(Finalized, 0);

            Array.Copy(Payload, 0, Finalized, HeaderArray.Length, Payload.Length);

        }
        else
        {
            //Self.TCPServer.InvokeDebugMessage("using encrypted payload");
            header.PayloadLength = Encrypted.Length;
            Finalized = new byte[header.HeaderLength + header.PayloadLength];
            HeaderArray = header.ToBinaryHeader();
            HeaderArray.CopyTo(Finalized, 0);

            Array.Copy(Encrypted, 0, Finalized, HeaderArray.Length, Encrypted.Length);
        }

        // ---- HANDLE ROUTING TYPES 
        // DISPLAY DEBUG PACKET OUTPUT
        //Console.WriteLine($"[DEBUG]:SendPacket -> \n{BitConverter.ToString(Finalized)}");

        // directly send to the current connection, intented for them.
        if (Route == PacketRoute.Direct && RecipientPeerId is null) return Connection.Send(Finalized);
        else
        {
            // IF DIRECT + RECIPIENT VALID; 
            if(Self.ConnectedPeers.Any(x => x.PeerId == RecipientPeerId))
            {
                // IF WE ARE CONNECTED TO THIS PEER
                // SET ROUTE TO DIRECT REGARDLESS OF WHAT IT IS (dont actually waste resources doing this)
                // WE'LL JUST IGNORE ANY NEW ROUTING IN THE RECEIVE PEER
                return Connection.Send(Finalized);
            }
            else if (Route == PacketRoute.Broadcast) return Connection.Send(Finalized);
            else if (Route == PacketRoute.Gossip) return Connection.Send(Finalized);
        }

        // THIS IS LEFT COMPLETELY UNFINISHED, COME BACK HERE AND FINISH IT LATER 
        Self.TCPServer.InvokeDebugMessage("got to the end so -1 by default");
        return -1;
    }

    //public int SendStandardPacket(string UTF8String, PacketAction ActionType) => SendStandardPacket(UTF8String.ToUTF8Byte(), ActionType)
    //public int SendStandardPacket(byte[] UTF8Data, PacketAction ActionType)
    //{
    //    // data will either be encrypted or not encrypted but its all based on the current peer settings


    //    if(Self.Settings is not null)
    //    {

    //    }


    //    return -1;
    //}

    //public int SendUTF8Packet(string UTF8Data, PacketAction Type = PacketAction.Data, bool IsEncryptionEnabled = true, PacketEncryption EncryptionType = PacketEncryption.NONE) => SendPacket(UTF8Data.ToUTF8Byte(), Type, IsEncryptionEnabled, EncryptionType);
    //public int SendPacket(byte[] Data, PacketAction Type = PacketAction.Data, bool IsEncryptionEnabled = true, PacketEncryption EncryptionType = PacketEncryption.NONE)
    //{
    //    int bytesSent = -1;

    //    //// Create a basic header here
    //    //PacketHeader header = new PacketHeader(0, Type, EncryptionType);
    //    //header.send - maybe I can avoid altering these functions 

    //    try
    //    {
    //        // data will either be encrypted or not encrypted but its all based on the current peer settings

    //        if (IsEncryptionEnabled)
    //        {
    //            // Use custom encryption type but keys need to be stored for this
    //            if (EncryptionType != PacketEncryption.NONE) bytesSent = SendEncryptedPacket(Data, EncryptionType, Type, false);
    //            // Also check for TLS completed before trying to send auto encrypted messages
    //            else if (Self.Settings is not null && Self.Settings.IsEncryptionEnabled) // if encryption is enabled, we need to guarentee our data gets autodecrypted
    //            {
    //                //Console.WriteLine($"Sending [{Type}] as [{Self.Settings.EncryptionType}]");
    //                //Console.WriteLine($"Sending [{Type}] as encrypted data"); 
    //                if (Self.Settings.EncryptionType != PacketEncryption.NONE) {  bytesSent = SendEncryptedPacket(Data, Self.Settings.EncryptionType, Type, false); }
    //                else {  bytesSent = Connection.Send(Data, Type); }
    //            }
    //            else bytesSent = Connection.Send(Data, Type);
    //        }
    //        else bytesSent = Connection.Send(Data, Type);

    //    }
    //    catch (Exception Ex) { Debug.WriteLine($"Error In (SendPacket): {Ex.ToString()}"); }

    //    return bytesSent;
    //}

    //public int SendPacketWithHeader(byte[] Data, PacketHeader PremadeHeader, bool IsEncryptionEnabled)
    //{
    //    int bytesSent = -1;

    //    // Just pass it through as is (only thing this function does is just add the data size to header)
    //    if (!IsEncryptionEnabled) bytesSent = Connection.SendWithHeader(Data, PremadeHeader);
    //    else 
    //    {
    //        if (PremadeHeader.Encryption != PacketEncryption.NONE) { bytesSent = SendEncryptedPacketWithHeader(Data, PremadeHeader, false); }
    //        else if (Self.Settings is not null && Self.Settings.IsEncryptionEnabled) 
    //        {
    //            //Console.WriteLine("Self settings not null and IsEncryptedEnabled");

    //            // Sets our encryption type here - if its not set already
    //            PremadeHeader.Encryption = Self.Settings.EncryptionType;

    //            if (Self.Settings.EncryptionType != PacketEncryption.NONE) { bytesSent = SendEncryptedPacketWithHeader(Data, PremadeHeader, false); } //Console.WriteLine("as encrypted");
    //            // unencrypted version of IsEncryptionEncrypted when its enabled
    //            else { bytesSent = Connection.SendWithHeader(Data, PremadeHeader); } //Console.WriteLine("as not encrypted");
    //        }
    //        // unencrypted version of IsEncryptionEncrypted when its enabled
    //        else bytesSent = Connection.SendWithHeader(Data, PremadeHeader);

    //        //Console.WriteLine($"Actual Data Length: {Data.Length} - {PremadeHeader.PacketAction.ToString()}");
    //        //Console.WriteLine($"SendPacketWithHeader-DEBUG - \n${PremadeHeader.ToJSON()}\n\nBytesSent: {bytesSent}");
    //    }

    //    return bytesSent;
    //}

    ///// <summary>
    ///// Sends already encrypted messages over the socket 
    ///// </summary>
    ///// <param name="Data">JSON of "PacketEncrypted" with metadata to help us decrypt this data later</param>
    ///// <param name="EncryptionType">Type of encryption used </param>
    ///// <param name="ActionType">The area that this data will be used at</param>
    ///// <returns></returns>
    //public int SendEncryptedPacket(byte[] Data, PacketEncryption EncryptionType, PacketAction ActionType, bool IsAlreadyEncryptedData = true)
    //{
    //    int bytesSent = -1;

    //    // at some point we need to modify this to allow for empty packets, so we can just use PacketAction without anything else
    //    if (Data is null || Data.Length == 0) return bytesSent;

    //    if (IsAlreadyEncryptedData) bytesSent = Connection.Send(Data, ActionType, EncryptionType);
    //    else
    //    {
    //        //Console.WriteLine("Manually encrypting data");
    //        // Manually encrypt the data here using stored encryption keys

    //        byte[] Key = Array.Empty<byte>();
    //        //Console.WriteLine($"Using encryption type {EncryptionType}");
    //        switch (EncryptionType)
    //        {
    //            case PacketEncryption.RSA: Key = EncryptionKeys.RemoteRSAPubKey; break;
    //            case PacketEncryption.ChaCha20Poly1305: Key = EncryptionKeys.ChaChaKey; break;
    //        }

    //        if (Key is not null && Key.Length > 0)
    //        {
    //            //Console.WriteLine("Key was greator than 0");
    //            Data = PacketEncrypted.EncryptUT8Bytes(Data, Key, EncryptionType);
    //            bytesSent = Connection.Send(Data, ActionType, EncryptionType);
    //            //Console.WriteLine("SendEncryptedPacket - Encrypted and sent data manually");
    //        }
    //        // Sends unenecrypted version if key not valid (we probably dont want this in the end)
    //        else bytesSent = Connection.Send(Data, ActionType);


    //    }

    //    return bytesSent;
    //}

    //public int SendEncryptedPacketWithHeader(byte[] Data, PacketHeader premadeHeader, bool IsAlreadyEncryptedData = true) 
    //{
    //    int bytesSent = -1;

    //    // at some point we need to modify this to allow for empty packets, so we can just use PacketAction without anything else
    //    if (Data is null || Data.Length == 0) { return bytesSent; } //Console.WriteLine("SendEncryptedPacketWithHeader - data is null, Data = 0"); 



    //    if (IsAlreadyEncryptedData) { bytesSent = Connection.SendWithHeader(Data, premadeHeader); } //Console.WriteLine("data is already encrypted"); 
    //    else
    //    {
    //        //Console.WriteLine("SendEncryptedPacketWithHeader - IsNotEncrypted");
    //        byte[] Key = Array.Empty<byte>();
    //        //Console.WriteLine($"SendEncryptedPacketWithHeader - encryption type {premadeHeader.PacketEncryption}");
    //        switch (premadeHeader.Encryption)
    //        {
    //            case PacketEncryption.RSA: Key = EncryptionKeys.RemoteRSAPubKey; break;
    //            case PacketEncryption.ChaCha20Poly1305: Key = EncryptionKeys.ChaChaKey; break;
    //        }


    //        if (Key is not null && Key.Length > 0)
    //        {
    //            //Console.WriteLine("SendEncryptedPacketWithHeader - KeyNotNull, Key>0");
    //            Data = PacketEncrypted.EncryptUT8Bytes(Data, Key, premadeHeader.Encryption);
    //            bytesSent = Connection.SendWithHeader(Data, premadeHeader);
    //            //Console.WriteLine("SendEncryptedPacket - Encrypted and sent data manually");
    //        }
    //        // Sends unenecrypted version (we probably dont want this in the end)
    //        else bytesSent = Connection.SendWithHeader(Data, premadeHeader);
    //    }

    //    return bytesSent;
    //}

    // DISABLED FOR NOW JUST TO PREVENT ERRORS
    // WE WANT THIS TO BE HERE JUST  AS A REFERENCE FOR LATER
    //public int SendVoicePacket(byte[] VoiceData)
    //{
    //    int bytesSent = -1;

    //    try { bytesSent = Connection.Send(VoiceData, PacketAction.Voice); }
    //    catch (Exception Ex) { Console.WriteLine($"Error In (SendVoicePacket): {Ex.ToString()}"); Debug.WriteLine($"Error In (SendVoicePacket): {Ex.ToString()}"); }

    //    return bytesSent;
    //}

}
