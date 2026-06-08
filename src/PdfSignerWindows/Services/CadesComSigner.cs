using System;
using System.IO;
using Microsoft.CSharp.RuntimeBinder;
using System.Runtime.InteropServices;

namespace PdfSignerWindows.Services
{
    public sealed class CadesComSigner
    {
        private const int CadescomLocalMachineStore = 1;
        private const int CadescomCurrentUserStore = 2;
        private const int CapicomStoreOpenMaximumAllowed = 2;
        private const int CapicomCertificateFindSha1Hash = 0;
        private const int CadescomBase64ToBinary = 1;
        private const int CadescomCadesBes = 1;
        private const int CapicomEncodeBase64 = 0;
        private const string CapicomMyStore = "My";

        public bool IsAvailable()
        {
            return Type.GetTypeFromProgID("CAdESCOM.CadesSignedData") != null
                && Type.GetTypeFromProgID("CAdESCOM.CPSigner") != null
                && Type.GetTypeFromProgID("CAdESCOM.Store") != null;
        }

        public byte[] SignDetached(Stream content, string certificateThumbprint)
        {
            byte[] bytes = ReadAllBytes(content);
            string base64Content = Convert.ToBase64String(bytes);
            string base64Signature = SignDetachedBase64(base64Content, certificateThumbprint);
            return Convert.FromBase64String(base64Signature);
        }

        private string SignDetachedBase64(string base64Content, string certificateThumbprint)
        {
            object store = null;
            object certificate = null;
            object signer = null;
            object signedData = null;

            try
            {
                certificate = FindCertificate(certificateThumbprint, out store);
                if (certificate == null)
                {
                    throw new InvalidOperationException("Certificate with the selected thumbprint was not found by CAdESCOM.");
                }

                signer = CreateComObject("CAdESCOM.CPSigner");
                dynamic dynamicSigner = signer;
                dynamicSigner.Certificate = certificate;
                dynamicSigner.CheckCertificate = true;

                signedData = CreateComObject("CAdESCOM.CadesSignedData");
                dynamic dynamicSignedData = signedData;
                dynamicSignedData.ContentEncoding = CadescomBase64ToBinary;
                dynamicSignedData.Content = base64Content;

                try
                {
                    return dynamicSignedData.SignCades(dynamicSigner, CadescomCadesBes, true, CapicomEncodeBase64);
                }
                catch (RuntimeBinderException)
                {
                    return dynamicSignedData.SignCades(dynamicSigner, CadescomCadesBes, true);
                }
                catch (MissingMethodException)
                {
                    return dynamicSignedData.SignCades(dynamicSigner, CadescomCadesBes, true);
                }
            }
            finally
            {
                ReleaseComObject(signedData);
                ReleaseComObject(signer);
                ReleaseComObject(certificate);
                CloseStore(store);
                ReleaseComObject(store);
            }
        }

        private static object FindCertificate(string thumbprint, out object openedStore)
        {
            string normalizedThumbprint = (thumbprint ?? string.Empty).Replace(" ", string.Empty).ToUpperInvariant();
            int[] locations = new[] { CadescomCurrentUserStore, CadescomLocalMachineStore };

            foreach (int location in locations)
            {
                object store = null;
                try
                {
                    store = CreateComObject("CAdESCOM.Store");
                    dynamic dynamicStore = store;
                    dynamicStore.Open(location, CapicomMyStore, CapicomStoreOpenMaximumAllowed);

                    dynamic certificates = dynamicStore.Certificates;
                    dynamic found = certificates.Find(CapicomCertificateFindSha1Hash, normalizedThumbprint, false);
                    if (found.Count > 0)
                    {
                        openedStore = store;
                        return found.Item(1);
                    }

                    CloseStore(store);
                    ReleaseComObject(store);
                }
                catch
                {
                    CloseStore(store);
                    ReleaseComObject(store);
                }
            }

            openedStore = null;
            return null;
        }

        private static object CreateComObject(string progId)
        {
            Type type = Type.GetTypeFromProgID(progId);
            if (type == null)
            {
                throw new InvalidOperationException("COM object is not registered: " + progId);
            }

            return Activator.CreateInstance(type);
        }

        private static byte[] ReadAllBytes(Stream stream)
        {
            MemoryStream memory = new MemoryStream();
            stream.CopyTo(memory);
            return memory.ToArray();
        }

        private static void CloseStore(object store)
        {
            if (store == null)
            {
                return;
            }

            try
            {
                dynamic dynamicStore = store;
                dynamicStore.Close();
            }
            catch
            {
            }
        }

        private static void ReleaseComObject(object value)
        {
            if (value == null)
            {
                return;
            }

            try
            {
                if (Marshal.IsComObject(value))
                {
                    Marshal.FinalReleaseComObject(value);
                }
            }
            catch
            {
            }
        }
    }
}
