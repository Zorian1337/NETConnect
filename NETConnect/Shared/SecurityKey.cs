using NETConnect.Encryption.Crypt;
using NETConnect.MyExtensions.Encryption;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETConnect.Shared;

public class SecurityKey
{
    // RSA Key Size

    // RSA Exported Key Data


    // My Security Keys


    // Remote Security Keys - (Will only have the public RSA key)
    public RSAKeySize RSAKeySize { get; set; }

    public RSACrypt.RSAExport LocalRSAKeys { get; set; }

    public byte[] RemoteRSAPubKey { get; set; } 
    

    public int AESKeySize { get; set; }
    public byte[] AESKey { get; set; }


    public SecurityKey(RSAKeySize RSAKeySize, RSACrypt.RSAExport LocalRSAKeys)
    {
        this.RSAKeySize = RSAKeySize;
        this.LocalRSAKeys = LocalRSAKeys;
    }

    public SecurityKey() { }

    public void UpdateLocalRSAKeys(int RSAKeySize, RSACrypt.RSAExport LocalRSAKeys)
    {
        this.RSAKeySize = RSAKeySize.GetRSASecurityLevel();
        this.LocalRSAKeys = LocalRSAKeys;
    }

    public void UpdateLocalRSAKeys(RSAKeySize RSAKeySize, RSACrypt.RSAExport LocalRSAKeys)
    {
        this.RSAKeySize = RSAKeySize;
        this.LocalRSAKeys = LocalRSAKeys;
    }

    public void SetRemoteRSAKey(byte[] RemoteRSAKey)
    {
        //Console.WriteLine("setting key");
        this.RemoteRSAPubKey = RemoteRSAKey;
        //Console.WriteLine("Set RemoteRSAKey");
    }

}
