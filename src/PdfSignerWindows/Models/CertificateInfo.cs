using System;
using System.Security.Cryptography.X509Certificates;

namespace PdfSignerWindows.Models
{
    public sealed class CertificateInfo
    {
        public CertificateInfo(X509Certificate2 certificate, StoreLocation storeLocation)
        {
            Certificate = certificate;
            StoreLocation = storeLocation;
            Thumbprint = NormalizeThumbprint(certificate.Thumbprint);
            Subject = certificate.Subject;
            Issuer = certificate.Issuer;
            NotBefore = certificate.NotBefore;
            NotAfter = certificate.NotAfter;
            HasPrivateKey = certificate.HasPrivateKey;
            DisplayName = BuildDisplayName(certificate);
        }

        public X509Certificate2 Certificate { get; private set; }

        public StoreLocation StoreLocation { get; private set; }

        public string Thumbprint { get; private set; }

        public string Subject { get; private set; }

        public string Issuer { get; private set; }

        public DateTime NotBefore { get; private set; }

        public DateTime NotAfter { get; private set; }

        public bool HasPrivateKey { get; private set; }

        public string DisplayName { get; private set; }

        public string StoreName
        {
            get { return StoreLocation == StoreLocation.CurrentUser ? "CurrentUser" : "LocalMachine"; }
        }

        private static string BuildDisplayName(X509Certificate2 certificate)
        {
            string cn = certificate.GetNameInfo(X509NameType.SimpleName, false);
            if (string.IsNullOrWhiteSpace(cn))
            {
                cn = certificate.Subject;
            }

            return cn;
        }

        private static string NormalizeThumbprint(string thumbprint)
        {
            return (thumbprint ?? string.Empty).Replace(" ", string.Empty).ToUpperInvariant();
        }
    }
}
