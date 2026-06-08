# PDF Signer Windows

Настольное приложение для Windows 7, 8.1, 10 и 11, которое подписывает PDF-файлы с помощью CryptoPro CSP и добавляет видимый штамп подписи в документ.

Это отдельный Windows-проект семейства `shurshick/pdf-signer`. Он не заменяет и не смешивается с существующей Linux-версией.

## Описание

PDF Signer Windows предназначен для пользователей, которым нужно быстро подписывать один или несколько PDF-документов на ПК с Windows и CryptoPro CSP. Приложение показывает действующие сертификаты из хранилища Windows, позволяет выбрать PDF-файлы, подписывает их встроенной отсоединенной CAdES-подписью и размещает синий видимый штамп на всех страницах.

MVP поддерживает:

- выбор одного или нескольких PDF-файлов;
- выбор действующего сертификата из хранилищ `CurrentUser\My` и `LocalMachine\My`;
- подписание PDF через CryptoPro CAdESCOM;
- видимый штамп подписи на всех страницах PDF;
- отпечаток сертификата на штампе;
- опциональное создание открепленной CAdES-подписи в формате `.sig`;
- режим `.sig` без встроенной подписи в PDF: PDF получает только видимый штамп, а криптографическая подпись создается отдельным файлом;
- сохранение результата в указанную папку или рядом с исходным PDF;
- пакетное подписание нескольких файлов за один запуск;
- автоматический русский или английский интерфейс по языку системы;
- сборку portable ZIP и установщика NSIS через GitHub Actions.

Требования к запуску:

- Windows 7 SP1, Windows 8.1, Windows 10 или Windows 11;
- .NET Framework 4.8;
- установленный CryptoPro CSP с доступным CAdESCOM;
- сертификат подписи с доступным закрытым ключом в хранилище Windows.

CryptoPro CSP, сертификаты и ключевые контейнеры не входят в поставку приложения.

---

Desktop PDF signing and visible stamp tool for Windows 7, 8.1, 10, and 11 with CryptoPro CSP.

This is a separate Windows project for the `shurshick/pdf-signer` family. It is not a replacement for the existing Linux repository.

## Status

MVP:

- select one or more PDF files;
- select a currently valid certificate from the Windows `CurrentUser\My` or `LocalMachine\My` certificate stores;
- create an embedded detached CAdES PDF signature through CryptoPro CAdESCOM;
- add a blue visible signature stamp on every page;
- include the certificate thumbprint on the stamp;
- optionally create a detached CAdES `.sig` signature;
- detached `.sig` mode without embedding a signature into the PDF: the PDF gets only a visible stamp, while the cryptographic signature is created as a separate file;
- save outputs either to the selected output folder or next to each source PDF;
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
