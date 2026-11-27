using System;
using System.Collections.Generic;
using System.Text;

namespace NETConnect.Shared.Packet.Transport.Security;


public class AuthenticatedSession 
{
    public byte[] remoteIdentityKey { get; set; }
    public byte[] remotePublicKey { get; set; }
    public byte[] remoteSignature { get; set; }
    public byte[] Secret { get; set; }
    public DateTime ConnectedAt { get; set; }
    //public DateTime AuthenticatedAt { get; set; }

    public AuthenticatedSession(byte[] remoteIdentityKey, byte[] remotePublicKey, byte[] remoteSignature, byte[] secret, DateTime connectedAt) //, DateTime authenticatedAt
    {
        this.remoteIdentityKey = remoteIdentityKey;
        this.remotePublicKey = remotePublicKey;
        this.remoteSignature = remoteSignature;
        Secret = secret;
        ConnectedAt = connectedAt;
        //AuthenticatedAt = authenticatedAt;
    }


}
