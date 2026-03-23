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
        this.Self = Self;
        this.Connection = Connection;
        
        // Init Keys
        // Originally just EncryptionKeys = new SecurityKey();
        this.EncryptionKeys = new SecurityKey(); // This probably causes our keys to get set as the wrong type
    }

    /// <summary>
    /// Used for the server version
    /// </summary>
    /// <param name="Connection"></param>
    /// <param name="Buffers"></param>
    /// <param name="ClientHandle"></param>
    public PacketHelper(ref Socket Connection, ref Peer Self, ref ServerClientHandle ClientHandle, ref CancellationTokenSource Token)
    {
        this.Self = Self;
        this.Connection = Connection;
        //this.Buffers = Buffers;
        this.ClientHandle = ClientHandle;

        // Server will be incharge of the type of encryption used, so safe to generate here...
        RSAKeySize KeySize = RSAKeySize.VerySecure;
        this.EncryptionKeys = new SecurityKey(KeySize, RSACrypt.CreateExport(KeySize));
    }



    //public int SendStandardPacket(string UTF8String, PacketActionType ActionType) => SendStandardPacket(UTF8String.ToUTF8Byte(), ActionType)
    //public int SendStandardPacket(byte[] UTF8Data, PacketActionType ActionType)
    //{
    //    // data will either be encrypted or not encrypted but its all based on the current peer settings


    //    if(Self.Settings is not null)
    //    {

    //    }


    //    return -1;
    //}

    public int SendUTF8Packet(string UTF8Data, PacketActionType Type = PacketActionType.Data, bool IsEncryptionEnabled = true, PacketEncryptionType EncryptionType = PacketEncryptionType.NONE) => SendPacket(UTF8Data.ToUTF8Byte(), Type, IsEncryptionEnabled, EncryptionType);
    public int SendPacket(byte[] Data, PacketActionType Type = PacketActionType.Data, bool IsEncryptionEnabled = true, PacketEncryptionType EncryptionType = PacketEncryptionType.NONE)
    {
        int bytesSent = -1;

        //// Create a basic header here
        //PacketHeader header = new PacketHeader(0, Type, EncryptionType);
        //header.send - maybe I can avoid altering these functions 

        try
        {
            // data will either be encrypted or not encrypted but its all based on the current peer settings

            if (IsEncryptionEnabled)
            {
                // Use custom encryption type but keys need to be stored for this
                if (EncryptionType != PacketEncryptionType.NONE) bytesSent = SendEncryptedPacket(Data, EncryptionType, Type, false);
                // Also check for TLS completed before trying to send auto encrypted messages
                else if (Self.Settings is not null && Self.Settings.IsEncryptionEnabled) // if encryption is enabled, we need to guarentee our data gets autodecrypted
                {
                    //Console.WriteLine($"Sending [{Type}] as [{Self.Settings.EncryptionType}]");
                    //Console.WriteLine($"Sending [{Type}] as encrypted data"); 
                    if (Self.Settings.EncryptionType != PacketEncryptionType.NONE) {  bytesSent = SendEncryptedPacket(Data, Self.Settings.EncryptionType, Type, false); }
                    else {  bytesSent = Connection.Send(Data, Type); }
                }
                else bytesSent = Connection.Send(Data, Type);
            }
            else bytesSent = Connection.Send(Data, Type);

        }
        catch (Exception Ex) { Debug.WriteLine($"Error In (SendPacket): {Ex.ToString()}"); }

        return bytesSent;
    }

    public int SendPacketWithHeader(byte[] Data, PacketHeader PremadeHeader, bool IsEncryptionEnabled)
    {
        int bytesSent = -1;

        // Just pass it through as is (only thing this function does is just add the data size to header)
        if (!IsEncryptionEnabled) bytesSent = Connection.SendWithHeader(Data, PremadeHeader);
        else 
        {
            if (PremadeHeader.PacketEncryptionType != PacketEncryptionType.NONE) { bytesSent = SendEncryptedPacketWithHeader(Data, PremadeHeader, false); }
            else if (Self.Settings is not null && Self.Settings.IsEncryptionEnabled) 
            {
                // Sets our encryption type here - if its not set already
                PremadeHeader.PacketEncryptionType = Self.Settings.EncryptionType;

                if (Self.Settings.EncryptionType != PacketEncryptionType.NONE) { bytesSent = SendEncryptedPacketWithHeader(Data, PremadeHeader, false); }
                // unencrypted version of IsEncryptionEncrypted when its enabled
                else bytesSent = Connection.SendWithHeader(Data, PremadeHeader);
            }
            // unencrypted version of IsEncryptionEncrypted when its enabled
            else bytesSent = Connection.SendWithHeader(Data, PremadeHeader);
        }

        return bytesSent;
    }

    /// <summary>
    /// Sends already encrypted messages over the socket 
    /// </summary>
    /// <param name="Data">JSON of "PacketEncrypted" with metadata to help us decrypt this data later</param>
    /// <param name="EncryptionType">Type of encryption used </param>
    /// <param name="ActionType">The area that this data will be used at</param>
    /// <returns></returns>
    public int SendEncryptedPacket(byte[] Data, PacketEncryptionType EncryptionType, PacketActionType ActionType, bool IsAlreadyEncryptedData = true)
    {
        int bytesSent = -1;

        // at some point we need to modify this to allow for empty packets, so we can just use PacketActionType without anything else
        if (Data is null || Data.Length == 0) return bytesSent;

        if (IsAlreadyEncryptedData) bytesSent = Connection.Send(Data, ActionType, EncryptionType);
        else
        {
            //Console.WriteLine("Manually encrypting data");
            // Manually encrypt the data here using stored encryption keys

            byte[] Key = Array.Empty<byte>();
            //Console.WriteLine($"Using encryption type {EncryptionType}");
            switch (EncryptionType)
            {
                case PacketEncryptionType.RSA: Key = EncryptionKeys.RemoteRSAPubKey; break;
                case PacketEncryptionType.ChaCha20Poly1305: Key = EncryptionKeys.ChaChaKey; break;
            }

            if (Key is not null && Key.Length > 0)
            {
                //Console.WriteLine("Key was greator than 0");
                Data = PacketEncrypted.EncryptUT8Bytes(Data, Key, EncryptionType);
                bytesSent = Connection.Send(Data, ActionType, EncryptionType);
                //Console.WriteLine("SendEncryptedPacket - Encrypted and sent data manually");
            }
            // Sends unenecrypted version if key not valid (we probably dont want this in the end)
            else bytesSent = Connection.Send(Data, ActionType);


        }

        return bytesSent;
    }

    public int SendEncryptedPacketWithHeader(byte[] Data, PacketHeader premadeHeader, bool IsAlreadyEncryptedData = true) 
    {
        int bytesSent = -1;

        // at some point we need to modify this to allow for empty packets, so we can just use PacketActionType without anything else
        if (Data is null || Data.Length == 0) return bytesSent;

        if (IsAlreadyEncryptedData) bytesSent = Connection.SendWithHeader(Data, premadeHeader);
        else
        {
            byte[] Key = Array.Empty<byte>();
            //Console.WriteLine($"Using encryption type {EncryptionType}");
            switch (premadeHeader.PacketEncryptionType)
            {
                case PacketEncryptionType.RSA: Key = EncryptionKeys.RemoteRSAPubKey; break;
                case PacketEncryptionType.ChaCha20Poly1305: Key = EncryptionKeys.ChaChaKey; break;
            }

            if (Key is not null && Key.Length > 0)
            {
                Data = PacketEncrypted.EncryptUT8Bytes(Data, Key, premadeHeader.PacketEncryptionType);
                bytesSent = Connection.SendWithHeader(Data, premadeHeader);
                //Console.WriteLine("SendEncryptedPacket - Encrypted and sent data manually");
            }
            // Sends unenecrypted version (we probably dont want this in the end)
            else bytesSent = Connection.SendWithHeader(Data, premadeHeader);
        }

        return bytesSent;
    }

    /// <summary>
    /// we want to pas the key and the encryption type, so it can encrypted and put into a class in this function
    /// </summary>
    /// <typeparam name="KeyDataForPacket"></typeparam>
    /// <param name="unencrypted"></param>
    /// <param name="Func"></param>
    /// <param name="Type"></param>
    /// <param name="RequireEncryption"></param>
    /// <returns></returns>
    //public int SendunEncryptedPacket(byte[] unencrypted, byte[] Key, PacketEncryptionType EncryptionType, PacketActionType Type = PacketActionType.Data, bool RequireEncryption = true)
    //{

    //    // Automatically wrap our byte arrays in encrypted streams based on our params and existing saved settings (meaning it will fail encryption if none are set)

    //    // Goal of this function is to help automatically send and decrypt encrypted packets so when they are read from stream they arent even encrypted
    //    int bytesSent = -1;


    //    byte[] dataToSend = default;
    //    PacketHeader? Header = default;

    //    PacketHMAC HMAC = default;

    //    switch (EncryptionType)
    //    {
    //        case PacketEncryptionType.RSA:

    //            break;

    //        case PacketEncryptionType.ChaCha20Poly1305:
    //            byte[] encrypted = unencrypted.EncryptChaCha(Key, out ChaCha.ChaChaKeys Keys);
    //            HMAC = new PacketHMAC(Key, encrypted, Keys.nonce, Keys.tag);
    //            dataToSend = HMAC.ToJSON().ToUTF8Byte();
    //            Header = new PacketHeader(dataToSend.Length, Type, PacketEncodingType.JSON);
    //            break;
    //    }

    //    // Key meta data that we can decrypt our message with
    //    //KeyDataForPacket KeyData = Func.DynamicInvoke();
        
    //    try { bytesSent = Connection.Send(dataToSend, Type, Header); }
    //    catch (Exception Ex) { Console.WriteLine($"Error In (SendVoicePacket): {Ex.ToString()}"); Debug.WriteLine($"Error In (SendVoicePacket): {Ex.ToString()}"); }

    //    return bytesSent;
    //}

    public int SendVoicePacket(byte[] VoiceData)
    {
        int bytesSent = -1;

        try { bytesSent = Connection.Send(VoiceData, PacketActionType.Voice); }
        catch (Exception Ex) { Console.WriteLine($"Error In (SendVoicePacket): {Ex.ToString()}"); Debug.WriteLine($"Error In (SendVoicePacket): {Ex.ToString()}"); }

        return bytesSent;
    }

}
