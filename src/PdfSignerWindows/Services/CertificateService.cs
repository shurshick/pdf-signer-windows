using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using PdfSignerWindows.Models;

namespace PdfSignerWindows.Services
{
    public sealed class CertificateService
    {
        public IList<CertificateInfo> LoadSigningCertificates()
        {
            List<CertificateInfo> certificates = new List<CertificateInfo>();
            LoadFromStore(StoreLocation.CurrentUser, certificates);
            LoadFromStore(StoreLocation.LocalMachine, certificates);

            DateTime now = DateTime.Now;

            return certificates
                .Where(c => c.HasPrivateKey)
                .Where(c => c.NotBefore <= now && c.NotAfter >= now)
                .GroupBy(c => c.StoreName + ":" + c.Thumbprint)
                .Select(g => g.First())
                .OrderBy(c => c.DisplayName)
                .ThenByDescending(c => c.NotAfter)
                .ToList();
        }

        private static void LoadFromStore(StoreLocation location, IList<CertificateInfo> result)
        {
            X509Store store = new X509Store(StoreName.My, location);
            try
            {
                store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
                foreach (X509Certificate2 certificate in store.Certificates)
                {
                    try
                    {
                        result.Add(new CertificateInfo(certificate, location));
                    }
                    catch
                    {
                        certificate.Dispose();
                    }
                }
            }
            catch
            {
            }
            finally
            {
                store.Close();
            }
        }
    }
}
