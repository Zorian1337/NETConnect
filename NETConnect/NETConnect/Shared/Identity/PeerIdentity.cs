using NETConnect.MyExtensions;
using NETConnect.Shared.IO.Encryption.Hash;
using NETConnect.Shared.Peers;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace NETConnect.Shared.Identity;



public class PeerIdentity
{
    public static string IdentityStorage = Path.Combine(AppContext.BaseDirectory, "Identity.json");

    public static Dictionary<string, PeerIdentity> PeerIdentities = new Dictionary<string, PeerIdentity>();

    public string IdentityName { get; set; }
    public Guid PeerId { get; set; }
    [JsonIgnore]
    public ECDsa IdentityKey { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    private byte[] IdentityPrivateKey { get; set; }


    [JsonIgnore]
    public ECDiffieHellman EphemeralECDH { get; set; }
    [JsonIgnore]
    public SignedECDHKey PublicEphemeral { get; set; }

    public DateTime IdentityCreated { get; set; }
    public string Hash { get; set; }

    //public static PeerIdentity CreateIdentity(string IdentityName = "default")
    //{




    //    return Identity;
    //}


    public byte[] GetPublicKey() => ECDSA_P256.ExportPublicKey(IdentityKey);
    public string GetIdentityJSON()
    {
        IdentityPrivateKey = ECDSA_P256.ExportPrivateKey(IdentityKey);
        return this.ToJSON();
    }
    

    public SignedECDHKey GenerateSignedEphemeralPublicKey(out ECDiffieHellman ephemeralECDH)
    {
        //Console.WriteLine($"IdentityKey: {IdentityKey is null}");
        //Console.WriteLine("generating signed keys");
        SignedECDHKey Ephemeral = ECDSA_P256.GenerateSignedEphemeralPublicKey(IdentityKey, out ephemeralECDH);

        EphemeralECDH = ephemeralECDH;
        PublicEphemeral = Ephemeral;
        //Console.WriteLine("finished generating signed keys");
        return Ephemeral;
    }

    public byte[] GenerateSharedSecretECCDH(byte[] remotePublicKey) => ECDSA_P256.GenerateSharedSecretECCDH(EphemeralECDH, remotePublicKey);











    public static PeerIdentity GetOrCreateExistingIdentityAsync(string IdentityName = "default", bool Unique = true)
    {
        if (PeerIdentities.Count() == 0) LoadProfiles();

        if (!PeerIdentities.TryGetValue(IdentityName, out PeerIdentity? Identity))
        {
            //Console.WriteLine("Identity doesnt exist");
            Identity = new PeerIdentity();
            Identity.PeerId = Guid.NewGuid();

            if(!Unique) Identity.IdentityName = IdentityName;
            else Identity.IdentityName = Identity.PeerId.ToString();

            Identity.IdentityKey = ECDSA_P256.GenerateIdentityKey();
            Identity.IdentityCreated = DateTime.UtcNow;

            PeerIdentities.Add(IdentityName, Identity);
            if(!Unique) SaveIdentity(Identity);
        }
        //else Console.WriteLine("Identity Exists");

        //Console.WriteLine($"PeerId: {Identity.PeerId}\n{Convert.ToBase64String(ECDSA_P256.ExportPublicKey(Identity.IdentityKey))}");

        return Identity;
    }

    public static void LoadProfiles()
    {
        // load all identities into app on start

        try
        {
            using (FileStream fs = new FileStream(IdentityStorage, FileMode.Open, FileAccess.Read))
            using (var reader = new StreamReader(fs))
            using (var writer = new StreamWriter(fs))
            {
                while (reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (line.IsNullWhiteSpaceOrEmpty()) continue;
                    else if (line.IsValidJSON(out PeerIdentity peerIdentity)) PeerIdentities.Add(peerIdentity.IdentityName, peerIdentity);
                }
            }
        }
        catch(Exception Ex) { Debug.WriteLine(Ex.ToString());  }

    }

    public static void SaveIdentity(PeerIdentity? Identity)
    {
        try
        {
            Console.WriteLine("loaded save");
            using (FileStream fs = new FileStream(IdentityStorage, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            using (var reader = new StreamReader(fs))
            using (var writer = new StreamWriter(fs))
            {
                List<PeerIdentity> foundPeers = new List<PeerIdentity>();
                if (PeerIdentities.Count() > 1)
                {
                    Console.WriteLine("count > 1");
                    // SEARCH FILE FOR EXISTING PROFILE
                    while (reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        if (line.IsNullWhiteSpaceOrEmpty()) continue;
                        else if (line.IsValidJSON(out PeerIdentity peerIdentity)) foundPeers.Add(peerIdentity);
                    }

                    // IF IT DOESNT EXIST ITS NOW SAFE TO ADD PROFILE
                    var excludedProfiles = PeerIdentities.Where(x => !foundPeers.Any(a => x.Value.IdentityName == a.IdentityName));
                    foreach (var excluded in excludedProfiles)
                    {
                        string JSON = excluded.Value.GetIdentityJSON();
                        writer.WriteLine(JSON);
                        Console.WriteLine("saved JSON to Identity.json");
                    }

                    // IF IT EXISTS AND ITS BEEN UPDATED, WE CAN MODIFY IT
                    // - MODIFICATIONS ARE DETECTED VIA COMPARING HASH FOR A DIFFERENCE
                }
                else writer.WriteLine(Identity.GetIdentityJSON());



            }

        }
        catch (Exception Ex) { Debug.WriteLine(Ex.ToString()); }
    }

    
}
