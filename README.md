# PDF Signer Windows

Desktop PDF signing and visible stamp tool for Windows 7, 8.1, 10, and 11 with CryptoPro CSP.

This is a separate Windows project for the `shurshick/pdf-signer` family. It is not a replacement for the existing Linux repository.

## Status

MVP:

- select one or more PDF files;
- select a certificate from the Windows `CurrentUser\My` or `LocalMachine\My` certificate stores;
- create an embedded detached CAdES PDF signature through CryptoPro CAdESCOM;
- add a visible signature stamp on the first page;
- batch-sign multiple PDFs in one run;
- choose Russian or English UI automatically from the system UI language;
- build a portable ZIP and an NSIS installer with GitHub Actions.

## Platform Decision

Windows 7 support rules out many modern desktop stacks. Go 1.20 is the final Go release that supports Windows 7 and Windows 8, while current Go/Fyne releases require newer Go versions. For this repository the Windows MVP uses:

- C# WinForms on .NET Framework 4.8;
- CryptoPro CAdESCOM COM API for CAdES signing;
- Windows Certificate Store for certificate discovery;
- iTextSharp 5 for PDF visible signature placement and byte-range signing;
- NSIS for a Windows 7-compatible installer.

See [docs/technical-decisions.md](docs/technical-decisions.md) for the compatibility notes and follow-up work.

## Runtime Requirements

- Windows 7 SP1, Windows 8.1, Windows 10, or Windows 11.
- .NET Framework 4.8.
- CryptoPro CSP with CAdESCOM available on the machine.
- A signing certificate with an accessible private key in the current user or local machine `My` store.

CryptoPro CSP and certificates are not bundled with this application.

## Build Locally

On a Windows machine with Visual Studio Build Tools:

```powershell
msbuild src\PdfSignerWindows\PdfSignerWindows.csproj /restore /p:Configuration=Release
```

The executable is produced under:

```text
src\PdfSignerWindows\bin\Release\net48\
```

## Release Artifacts

The GitHub Actions workflow builds:

- `pdf-signer-windows-<version>-portable.zip`
- `pdf-signer-windows-<version>-setup.exe`

Pushing a tag like `v0.1.0` creates a GitHub Release with both artifacts.

## License

AGPL-3.0-or-later. The MVP uses iTextSharp 5, which is AGPL/commercial licensed.
