using System;
using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.security;
using AppCertificateInfo = PdfSignerWindows.Models.CertificateInfo;

namespace PdfSignerWindows.Services
{
    public sealed class PdfSigningService
    {
        private readonly CadesComSigner _signer;

        public PdfSigningService(CadesComSigner signer)
        {
            _signer = signer;
        }

        public string SignPdf(string inputPath, string outputDirectory, AppCertificateInfo certificate, string reason)
        {
            if (string.IsNullOrWhiteSpace(inputPath))
            {
                throw new ArgumentException("Input path is required.", "inputPath");
            }

            if (certificate == null)
            {
                throw new ArgumentNullException("certificate");
            }

            Directory.CreateDirectory(outputDirectory);
            string outputPath = CreateOutputPath(inputPath, outputDirectory);

            using (PdfReader reader = new PdfReader(inputPath))
            using (FileStream output = new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                PdfStamper stamper = PdfStamper.CreateSignature(reader, output, '\0', null, true);
                PdfSignatureAppearance appearance = stamper.SignatureAppearance;
                appearance.Reason = reason;
                appearance.Location = Environment.MachineName;
                appearance.SignDate = DateTime.Now;
                appearance.Acro6Layers = true;
                appearance.Layer2Text = BuildStampText(certificate, reason);
                appearance.Layer2Font = new Font(Font.FontFamily.HELVETICA, 8f, Font.NORMAL, BaseColor.BLACK);

                Rectangle page = reader.GetPageSizeWithRotation(1);
                appearance.SetVisibleSignature(BuildStampRectangle(page), 1, "sig_" + Guid.NewGuid().ToString("N"));

                IExternalSignatureContainer container = new CadesExternalSignatureContainer(_signer, certificate.Thumbprint);
                MakeSignature.SignExternalContainer(appearance, container, 65536);
            }

            return outputPath;
        }

        private static string BuildStampText(AppCertificateInfo certificate, string reason)
        {
            string name = certificate.DisplayName;
            string date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            return "Digitally signed by: " + name + Environment.NewLine
                + "Date: " + date + Environment.NewLine
                + "Reason: " + reason;
        }

        private static Rectangle BuildStampRectangle(Rectangle page)
        {
            const float width = 220f;
            const float height = 72f;
            const float margin = 36f;

            float left = Math.Max(margin, page.Right - margin - width);
            float bottom = margin;
            return new Rectangle(left, bottom, left + width, bottom + height);
        }

        private static string CreateOutputPath(string inputPath, string outputDirectory)
        {
            string fileName = Path.GetFileNameWithoutExtension(inputPath);
            string extension = Path.GetExtension(inputPath);
            string candidate = Path.Combine(outputDirectory, fileName + "-signed" + extension);
            int index = 2;

            while (File.Exists(candidate))
            {
                candidate = Path.Combine(outputDirectory, fileName + "-signed-" + index + extension);
                index++;
            }

            return candidate;
        }

        private sealed class CadesExternalSignatureContainer : IExternalSignatureContainer
        {
            private readonly CadesComSigner _signer;
            private readonly string _thumbprint;

            public CadesExternalSignatureContainer(CadesComSigner signer, string thumbprint)
            {
                _signer = signer;
                _thumbprint = thumbprint;
            }

            public byte[] Sign(Stream data)
            {
                return _signer.SignDetached(data, _thumbprint);
            }

            public void ModifySigningDictionary(PdfDictionary signDic)
            {
                signDic.Put(PdfName.FILTER, PdfName.ADOBE_PPKLITE);
                signDic.Put(PdfName.SUBFILTER, new PdfName("ETSI.CAdES.detached"));
            }
        }
    }
}
