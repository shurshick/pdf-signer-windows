using System;
using System.IO;
using System.Security.Cryptography;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.security;
using AppCertificateInfo = PdfSignerWindows.Models.CertificateInfo;

namespace PdfSignerWindows.Services
{
    public sealed class PdfSigningService
    {
        private readonly CadesComSigner _signer;
        private readonly CryptCpDetachedSignatureService _cryptCpDetachedSignatureService = new CryptCpDetachedSignatureService();

        public PdfSigningService(CadesComSigner signer)
        {
            _signer = signer;
        }

        public string SignPdf(string inputPath, string outputDirectory, AppCertificateInfo certificate, string reason, bool createDetachedSignature)
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
            string documentHash = ComputeSha256(inputPath);
            string detachedSignaturePath = null;
            string detachedSignatureHash = null;

            if (createDetachedSignature)
            {
                detachedSignaturePath = CreateDetachedSignaturePath(inputPath, outputDirectory);
                _cryptCpDetachedSignatureService.CreateSigFile(inputPath, detachedSignaturePath, certificate.Thumbprint);
                detachedSignatureHash = ComputeSha256(detachedSignaturePath);
            }

            try
            {
                using (PdfReader reader = new PdfReader(inputPath))
                using (FileStream output = new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    PdfStamper stamper = PdfStamper.CreateSignature(reader, output, '\0', null, true);
                    DateTime signDate = DateTime.Now;
                    DrawStampOnAllPages(stamper, reader, certificate, reason, signDate, documentHash, detachedSignatureHash);

                    PdfSignatureAppearance appearance = stamper.SignatureAppearance;
                    appearance.Reason = reason;
                    appearance.Location = Environment.MachineName;
                    appearance.SignDate = signDate;

                    IExternalSignatureContainer container = new CadesExternalSignatureContainer(_signer, certificate.Thumbprint);
                    MakeSignature.SignExternalContainer(appearance, container, 65536);
                }
            }
            catch
            {
                DeleteFileIfCreated(outputPath);
                if (!string.IsNullOrEmpty(detachedSignaturePath))
                {
                    DeleteFileIfCreated(detachedSignaturePath);
                }
                throw;
            }

            return outputPath;
        }

        private static void DrawStampOnAllPages(PdfStamper stamper, PdfReader reader, AppCertificateInfo certificate, string reason, DateTime signDate, string documentHash, string detachedSignatureHash)
        {
            string stampText = BuildStampText(certificate, reason, signDate, documentHash, detachedSignatureHash);
            BaseFont baseFont = CreateStampFont();
            BaseColor stampBlue = new BaseColor(0, 74, 173);

            for (int pageNumber = 1; pageNumber <= reader.NumberOfPages; pageNumber++)
            {
                Rectangle page = reader.GetPageSizeWithRotation(pageNumber);
                Rectangle stamp = BuildStampRectangle(page);
                PdfContentByte canvas = stamper.GetOverContent(pageNumber);

                canvas.SaveState();
                canvas.SetColorFill(BaseColor.WHITE);
                canvas.SetColorStroke(stampBlue);
                canvas.SetLineWidth(1.2f);
                canvas.RoundRectangle(stamp.Left, stamp.Bottom, stamp.Width, stamp.Height, 4f);
                canvas.FillStroke();

                ColumnText column = new ColumnText(canvas);
                Font font = new Font(baseFont, 7.2f, Font.NORMAL, stampBlue);
                column.SetSimpleColumn(
                    new Phrase(stampText, font),
                    stamp.Left + 6f,
                    stamp.Bottom + 5f,
                    stamp.Right - 6f,
                    stamp.Top - 5f,
                    8.8f,
                    Element.ALIGN_LEFT);
                column.Go();
                canvas.RestoreState();
            }
        }

        private static string BuildStampText(AppCertificateInfo certificate, string reason, DateTime signDate, string documentHash, string detachedSignatureHash)
        {
            string name = certificate.DisplayName;
            string date = signDate.ToString("yyyy-MM-dd HH:mm:ss");
            string text = "Digitally signed by: " + name + Environment.NewLine
                + "Date: " + date + Environment.NewLine
                + "Reason: " + reason + Environment.NewLine
                + "Data SHA-256: " + FormatHash(documentHash) + Environment.NewLine;

            if (!string.IsNullOrWhiteSpace(detachedSignatureHash))
            {
                text += "SIG SHA-256: " + FormatHash(detachedSignatureHash) + Environment.NewLine;
            }

            return text + "Cert SHA-1: " + FormatHash(certificate.Thumbprint);
        }

        private static BaseFont CreateStampFont()
        {
            string arial = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
            if (File.Exists(arial))
            {
                return BaseFont.CreateFont(arial, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
            }

            return BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
        }

        private static Rectangle BuildStampRectangle(Rectangle page)
        {
            const float width = 220f;
            const float height = 94f;
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

        private static string CreateDetachedSignaturePath(string inputPath, string outputDirectory)
        {
            string fileName = Path.GetFileNameWithoutExtension(inputPath);
            string candidate = Path.Combine(outputDirectory, fileName + ".sig");
            int index = 2;

            while (File.Exists(candidate))
            {
                candidate = Path.Combine(outputDirectory, fileName + "-" + index + ".sig");
                index++;
            }

            return candidate;
        }

        private static string ComputeSha256(string path)
        {
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (SHA256 sha256 = SHA256.Create())
            {
                return BitConverter.ToString(sha256.ComputeHash(stream)).Replace("-", string.Empty).ToUpperInvariant();
            }
        }

        private static void DeleteFileIfCreated(string path)
        {
            try
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
            }
        }

        private static string FormatHash(string hash)
        {
            if (string.IsNullOrWhiteSpace(hash))
            {
                return string.Empty;
            }

            string normalized = hash.Replace(" ", string.Empty).ToUpperInvariant();
            if (normalized.Length <= 16)
            {
                return normalized;
            }

            return normalized.Substring(0, 16) + "..." + normalized.Substring(normalized.Length - 8);
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
