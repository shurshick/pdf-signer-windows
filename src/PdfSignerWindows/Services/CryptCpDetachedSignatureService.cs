using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Win32;

namespace PdfSignerWindows.Services
{
    public sealed class CryptCpDetachedSignatureService
    {
        public string CreateSigFile(string inputPath, string outputPath, string certificateThumbprint, string configuredCryptCpPath)
        {
            string cryptCpPath = ResolveCryptCpPath(configuredCryptCpPath);
            if (string.IsNullOrEmpty(cryptCpPath))
            {
                throw new InvalidOperationException("cryptcp.exe was not found. Select cryptcp.exe manually or install CryptoPro CSP command-line tools.");
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

        public string ResolveCryptCpPath(string configuredCryptCpPath)
        {
            if (File.Exists(configuredCryptCpPath))
            {
                return configuredCryptCpPath;
            }

            return FindCryptCpPath();
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

            foreach (string directory in BuildRegistryDirectories())
            {
                yield return directory;
            }

            string system = Environment.GetFolderPath(Environment.SpecialFolder.System);
            if (!string.IsNullOrWhiteSpace(system))
            {
                yield return system;
            }

            string windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            if (!string.IsNullOrWhiteSpace(windows))
            {
                yield return Path.Combine(windows, "SysWOW64");
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

            foreach (string directory in BuildRegistryDirectories())
            {
                yield return directory;
            }
        }

        private static IEnumerable<string> BuildRegistryDirectories()
        {
            foreach (RegistryView view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                foreach (string directory in BuildRegistryDirectories(view))
                {
                    yield return directory;
                }
            }
        }

        private static IEnumerable<string> BuildRegistryDirectories(RegistryView view)
        {
            RegistryHive[] hives = new[] { RegistryHive.LocalMachine, RegistryHive.CurrentUser };
            foreach (RegistryHive hive in hives)
            {
                RegistryKey baseKey = null;
                try
                {
                    baseKey = RegistryKey.OpenBaseKey(hive, view);
                }
                catch
                {
                    continue;
                }

                using (baseKey)
                {
                    foreach (string directory in ReadCryptoProRegistryDirectories(baseKey))
                    {
                        yield return directory;
                    }
                }
            }
        }

        private static IEnumerable<string> ReadCryptoProRegistryDirectories(RegistryKey baseKey)
        {
            foreach (string path in new[] { @"SOFTWARE\Crypto Pro", @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\cryptcp.exe" })
            {
                using (RegistryKey key = baseKey.OpenSubKey(path))
                {
                    foreach (string directory in ReadDirectoriesFromKey(key))
                    {
                        yield return directory;
                    }
                }
            }

            foreach (string uninstallRoot in new[] { @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall" })
            {
                using (RegistryKey root = baseKey.OpenSubKey(uninstallRoot))
                {
                    if (root == null)
                    {
                        continue;
                    }

                    foreach (string subKeyName in root.GetSubKeyNames())
                    {
                        using (RegistryKey subKey = root.OpenSubKey(subKeyName))
                        {
                            string displayName = Convert.ToString(subKey == null ? null : subKey.GetValue("DisplayName"));
                            if (!IsCryptoProName(displayName))
                            {
                                continue;
                            }

                            foreach (string directory in ReadDirectoriesFromKey(subKey))
                            {
                                yield return directory;
                            }
                        }
                    }
                }
            }
        }

        private static IEnumerable<string> ReadDirectoriesFromKey(RegistryKey key)
        {
            if (key == null)
            {
                yield break;
            }

            foreach (string valueName in new[] { null, "Path", "InstallDir", "InstallLocation", "DisplayIcon" })
            {
                string value = Convert.ToString(key.GetValue(valueName));
                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                string directory = NormalizeDirectory(value);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    yield return directory;
                    yield return Path.Combine(directory, "CSP");
                }
            }
        }

        private static bool IsCryptoProName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return value.IndexOf("CryptoPro", StringComparison.OrdinalIgnoreCase) >= 0
                || value.IndexOf("Crypto Pro", StringComparison.OrdinalIgnoreCase) >= 0
                || value.IndexOf("КриптоПро", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string NormalizeDirectory(string value)
        {
            string normalized = (value ?? string.Empty).Trim().Trim('"');
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return null;
            }

            if (File.Exists(normalized))
            {
                return Path.GetDirectoryName(normalized);
            }

            if (Directory.Exists(normalized))
            {
                return normalized;
            }

            string withoutArgs = normalized;
            int exeIndex = normalized.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
            if (exeIndex >= 0)
            {
                withoutArgs = normalized.Substring(0, exeIndex + 4).Trim().Trim('"');
                if (File.Exists(withoutArgs))
                {
                    return Path.GetDirectoryName(withoutArgs);
                }
            }

            return normalized;
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
