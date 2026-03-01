using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace NETConnect.Encryption.Crypt
{
    public class X509
    {

        //File.WriteAllBytes("peerCert.pfx", certificate.Export(X509ContentType.Pfx));
        //var certificate = new X509Certificate2("peerCert.pfx");

        public static string GetPeerId(X509Certificate2 cert)
        {
            byte[] pubKey = cert.GetPublicKey();
            byte[] hash = SHA256.HashData(pubKey);
            return Convert.ToHexString(hash);
        }

        public static X509Certificate2 CreatePeerCertificate(string peerName)
        {
            using RSA rsa = RSA.Create((int)RSAKeySize.HighSecurity);

            var request = new CertificateRequest(
                subjectName: $"CN={peerName}",
                key: rsa,
                hashAlgorithm: HashAlgorithmName.SHA256,
                padding: RSASignaturePadding.Pkcs1);

            // certificate usable for TLS authentication
            request.CertificateExtensions.Add(
                new X509KeyUsageExtension(
                    X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                    critical: false));

            var cert = request.CreateSelfSigned(
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddYears(5));

            // Important: export with private key so SslStream can use it
            return X509CertificateLoader.LoadCertificate(cert.Export(X509ContentType.Pfx));
            //return new X509Certificate2(cert.Export(X509ContentType.Pfx));
        }
    }
}
