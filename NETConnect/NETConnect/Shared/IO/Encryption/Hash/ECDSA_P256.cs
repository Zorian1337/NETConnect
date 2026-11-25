using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace NETConnect.Shared.IO.Encryption.Hash;

public class SignedECDHKey
{
    public SignedECDHKey() { }
    public SignedECDHKey(byte[] PublicKey, byte[] Signature)
    {
        this.PublicKey = PublicKey;
        this.Signature = Signature;
    }

    public byte[] PublicKey { get; set; }
    public byte[] Signature { get; set; }



}

//Elliptic Curve Digital Signature Algorithm
public static class ECDSA_P256
{
    // ExportPkcs8PrivateKey to be loaded from file later
    public static ECDsa GenerateIdentityKey() => ECDsa.Create(ECCurve.NamedCurves.nistP256);
    public static byte[] ExportPrivateKey(ECDsa Key) => Key.ExportPkcs8PrivateKey();
    public static byte[] ExportPublicKey(ECDsa Key) => Key.ExportSubjectPublicKeyInfo();
    public static ECDsa ImportPrivateKey(byte[] data)
    {
        ECDsa key = ECDsa.Create();
        key.ImportPkcs8PrivateKey(data, out _);
        return key;
    }
    public static byte[] GenerateSignature(ECDsa signerKey, byte[] dataToSign, HashAlgorithmName HashType) => signerKey.SignData(dataToSign, HashType);


    public static bool IsValidSignature(byte[] signerPublicKey, byte[] signedData, byte[] Signature, HashAlgorithmName HashType = default)
    {
        if (HashType == default) HashType = HashAlgorithmName.SHA256;

        ECDsa key = ECDsa.Create();
        key.ImportSubjectPublicKeyInfo(signerPublicKey, out _);

        try { return key.VerifyData(signedData, Signature, HashType); }
        catch (CryptographicException) { return false; }
    }

    public static bool IsValidSignature(ECDsa signerPublicKey, byte[] signedData, byte[] Signature, HashAlgorithmName HashType = default)
    {
        if (HashType == default) HashType = HashAlgorithmName.SHA256;

        try { return signerPublicKey.VerifyData(signedData, Signature, HashType); }
        catch(CryptographicException) { return false; }
    }

    public static SignedECDHKey GenerateSignedEphemeralPublicKey(ECDsa IdentityKey)
    {
        using var ephemeralEcdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);

        byte[] PublicKey = ephemeralEcdh.ExportSubjectPublicKeyInfo();
        byte[] Signature = IdentityKey.SignData(PublicKey, HashAlgorithmName.SHA256);

        return new SignedECDHKey(PublicKey, Signature);
    }

    public static SignedECDHKey GenerateSignedEphemeralPublicKey(ECDsa IdentityKey, out ECDiffieHellman ephemeralEcdh)
    {
        //Console.WriteLine("creating ephemeral");
        ephemeralEcdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        //Console.WriteLine("created ephemeral");
        byte[] PublicKey = ephemeralEcdh.ExportSubjectPublicKeyInfo();
        byte[] Signature = IdentityKey.SignData(PublicKey, HashAlgorithmName.SHA256);


        //Console.WriteLine($"Public key len={PublicKey.Length}, first bytes: " + BitConverter.ToString(PublicKey.AsSpan(0, Math.Min(16, PublicKey.Length)).ToArray()));

        return new SignedECDHKey(PublicKey, Signature);
    }

    public static byte[] GenerateSharedSecretECCDH(ECDiffieHellman PrivateLocalKey, ECDiffieHellman RemoteKey) => PrivateLocalKey.DeriveRawSecretAgreement(RemoteKey.PublicKey);
    public static byte[] GenerateSharedSecretECCDH(ECDiffieHellman PrivateLocalKey, byte[] PublicRemoteKey)
    {
        //Console.WriteLine($"Remote key len={PublicRemoteKey.Length}, first bytes: " +
        //BitConverter.ToString(PublicRemoteKey.AsSpan(0, Math.Min(16, PublicRemoteKey.Length)).ToArray()));

        using var peerEphemeralEcdh = ECDiffieHellman.Create();
        peerEphemeralEcdh.ImportSubjectPublicKeyInfo(PublicRemoteKey, out _);
        byte[] sharedSecret = PrivateLocalKey.DeriveRawSecretAgreement(peerEphemeralEcdh.PublicKey);

        return sharedSecret;
    }
}
