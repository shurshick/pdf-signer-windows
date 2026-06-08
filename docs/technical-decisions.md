# Technical Decisions

## Target Operating Systems

The product target is Windows 7 SP1, Windows 8.1, Windows 10, and Windows 11.

Important constraints:

- Go supports Windows 7 and Windows 8 only through Go 1.20. Go 1.21 and newer require Windows 10 or newer.
- .NET Framework 4.8 is installable on Windows 7 SP1 and Windows 8.1, and is available on modern Windows versions.
- NSIS can create installers compatible with Windows 7 through Windows 11.

## Stack Choice

Chosen MVP stack:

- UI: WinForms on .NET Framework 4.8.
- Signing: CryptoPro CAdESCOM COM objects.
- Certificate list: Windows Certificate Store through `System.Security.Cryptography.X509Certificates`.
- PDF container and visible stamp: iTextSharp 5.
- Installer: NSIS.

Rejected or deferred:

- Modern Go/Fyne: attractive because the Linux project uses Go/Fyne, but Windows 7 support would require pinning old Go/toolkit versions and accepting higher GUI/runtime risk.
- .NET 6/8/9 WinForms: unsupported on Windows 7.
- WPF: technically possible on .NET Framework 4.8, but WinForms is simpler, lighter, and easier to keep compatible with older Windows machines.
- WiX: useful for MSI, but heavier than needed for the MVP and less convenient for a portable-first release.

## CryptoPro Integration

The MVP signs through CAdESCOM:

1. The app discovers certificates from Windows certificate stores.
2. Expired and not-yet-valid certificates are hidden from the selectable list.
3. The user selects a certificate by thumbprint.
4. During PDF signing, iTextSharp provides the PDF byte-range stream.
5. The app passes the byte-range bytes to `CAdESCOM.CadesSignedData`.
6. CAdESCOM creates a detached CAdES-BES signature with `CAdESCOM.CPSigner`.
7. iTextSharp embeds the signature bytes into the PDF signature container after drawing the visible stamp on every page.
8. If requested, the app also creates a detached `.sig` CAdES signature file for the selected source PDF through CryptoPro `cryptcp.exe -sign -der -detached` and prints the SHA-256 hash of that `.sig` on the visible stamp.

Planned fallback work:

- Detect `certmgr.exe`, `csptest.exe`, and `cryptcp.exe` in common CryptoPro installation paths.
- Add a diagnostic screen with CryptoPro version, provider availability, and certificate/key-container status.
- Evaluate whether `cryptcp.exe` can also provide a reliable command-line PDF signing fallback for installations where CAdESCOM is unavailable.
- Evaluate direct CryptoAPI/CSP integration if command-line fallback proves too limited.

## PDF Signature Notes

The MVP uses `/ETSI.CAdES.detached` and an estimated signature container size of 65536 bytes. If production certificates include very long chains, OCSP/CRL data, or timestamps, this size may need to become configurable or dynamically retried.

The visible stamp is blue and is placed on every page in the lower-right corner before the document is signed, so the stamp content is covered by the signature. It includes signer name, signing date, reason, SHA-256 of the selected source PDF, the SHA-256 hash of the detached `.sig` when that option is enabled, and the certificate SHA-1 thumbprint.

The final CMS/CAdES signature value is only known while the PDF byte-range signature container is being finalized. Updating visible page content after that point would invalidate the PDF signature, so the MVP prints the source data hash and certificate thumbprint on the stamp instead of mutating the signed PDF after signing.

Later versions should support:

- page selection;
- drag-and-drop stamp placement;
- configurable stamp text/template;
- timestamp authority settings;
- signature validation report.

## Sources Checked

- Go Windows support matrix: https://go.dev/wiki/Windows
- .NET Framework system requirements: https://learn.microsoft.com/en-us/dotnet/framework/get-started/system-requirements
- CryptoPro CAdESCOM documentation: https://docs.cryptopro.ru/cades/reference/cadescom
- CryptoPro CAdESCOM Store documentation: https://docs.cryptopro.ru/cades/reference/cadescom/cadescom_class/store
- CryptoPro CAdES signing examples: https://docs.cryptopro.ru/cades/dotnetcades/dotnetcades-samples/dotnetcades-sign-verify
- NSIS compatibility/features: https://nsis.sourceforge.io/Features
- Inno Setup old versions/support notes: https://jrsoftware.org/isdl-old.php
