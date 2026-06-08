using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace PdfSignerWindows.Services
{
    public sealed class CryptCpDetachedSignatureService
    {
        public string CreateSigFile(string inputPath, string outputPath, string certificateThumbprint)
        {
            string cryptCpPath = FindCryptCpPath();
            if (string.IsNullOrEmpty(cryptCpPath))
            {
                throw new InvalidOperationException("cryptcp.exe was not found. Install CryptoPro CSP command-line tools or add cryptcp.exe to PATH.");
            }

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = cryptCpPath;
            startInfo.Arguments = "-sign -der -detached -thumbprint "
                + Quote(NormalizeThumbprint(certificateThumbprint))
                + " "
                + Quote(inputPath)
                + " "
                + Quote(outputPath);
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.StandardOutputEncoding = Encoding.GetEncoding(866);
            startInfo.StandardErrorEncoding = Encoding.GetEncoding(866);

            using (Process process = Process.Start(startInfo))
            {
                string stdout = process.StandardOutput.ReadToEnd();
                string stderr = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0 || !File.Exists(outputPath))
                {
                    throw new InvalidOperationException("cryptcp.exe failed to create detached .sig signature. " + (stderr + " " + stdout).Trim());
                }
            }

            return outputPath;
        }

        private static string FindCryptCpPath()
        {
            foreach (string directory in BuildSearchDirectories())
            {
                string candidate = Path.Combine(directory, "cryptcp.exe");
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            foreach (string root in BuildCryptoProRoots())
            {
                try
                {
                    foreach (string candidate in Directory.GetFiles(root, "cryptcp.exe", SearchOption.AllDirectories))
                    {
                        return candidate;
                    }
                }
                catch
                {
                }
            }

            return null;
        }

        private static IEnumerable<string> BuildSearchDirectories()
        {
            string path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            foreach (string part in path.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                yield return part.Trim('"');
            }

            foreach (string root in BuildCryptoProRoots())
            {
                yield return Path.Combine(root, "CSP");
            }
        }

        private static IEnumerable<string> BuildCryptoProRoots()
        {
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            if (!string.IsNullOrWhiteSpace(programFiles))
            {
                yield return Path.Combine(programFiles, "Crypto Pro");
            }

            if (!string.IsNullOrWhiteSpace(programFilesX86))
            {
                yield return Path.Combine(programFilesX86, "Crypto Pro");
            }
        }

        private static string NormalizeThumbprint(string thumbprint)
        {
            return (thumbprint ?? string.Empty).Replace(" ", string.Empty).ToUpperInvariant();
        }

        private static string Quote(string value)
        {
            return "\"" + (value ?? string.Empty).Replace("\"", "\\\"") + "\"";
        }
    }
}
