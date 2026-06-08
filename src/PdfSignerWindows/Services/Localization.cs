using System.Globalization;

namespace PdfSignerWindows.Services
{
    public sealed class Localization
    {
        private readonly bool _ru;

        public Localization()
        {
            _ru = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ru";
        }

        public string AppTitle { get { return _ru ? "PDF Signer Windows" : "PDF Signer Windows"; } }
        public string Files { get { return _ru ? "PDF-файлы" : "PDF files"; } }
        public string AddFiles { get { return _ru ? "Добавить PDF" : "Add PDFs"; } }
        public string RemoveFile { get { return _ru ? "Убрать" : "Remove"; } }
        public string ClearFiles { get { return _ru ? "Очистить" : "Clear"; } }
        public string Certificates { get { return _ru ? "Сертификаты" : "Certificates"; } }
        public string Refresh { get { return _ru ? "Обновить" : "Refresh"; } }
        public string OutputFolder { get { return _ru ? "Папка результата" : "Output folder"; } }
        public string Browse { get { return _ru ? "Выбрать" : "Browse"; } }
        public string Reason { get { return _ru ? "Назначение подписи" : "Signing reason"; } }
        public string DetachedSignature { get { return _ru ? "Создать открепленную подпись .sig" : "Create detached .sig signature"; } }
        public string SaveNextToSource { get { return _ru ? "Сохранять рядом с исходным PDF" : "Save next to source PDF"; } }
        public string DefaultReason { get { return _ru ? "Подписано в PDF Signer Windows" : "Signed with PDF Signer Windows"; } }
        public string Sign { get { return _ru ? "Подписать" : "Sign"; } }
        public string Ready { get { return _ru ? "Готово" : "Ready"; } }
        public string LoadingCertificates { get { return _ru ? "Загрузка сертификатов..." : "Loading certificates..."; } }
        public string NoFiles { get { return _ru ? "Добавьте хотя бы один PDF-файл." : "Add at least one PDF file."; } }
        public string NoCertificate { get { return _ru ? "Выберите сертификат." : "Select a certificate."; } }
        public string Done { get { return _ru ? "Подписание завершено." : "Signing completed."; } }
        public string Failed { get { return _ru ? "Ошибка" : "Error"; } }
        public string CryptoProUnavailable { get { return _ru ? "CryptoPro CAdESCOM не найден. Проверьте установку CryptoPro CSP/CAdES." : "CryptoPro CAdESCOM was not found. Check CryptoPro CSP/CAdES installation."; } }
        public string SignedCount(int done, int total) { return _ru ? "Подписано " + done + " из " + total : "Signed " + done + " of " + total; }
        public string SelectPdfFilter { get { return _ru ? "PDF-файлы (*.pdf)|*.pdf|Все файлы (*.*)|*.*" : "PDF files (*.pdf)|*.pdf|All files (*.*)|*.*"; } }
        public string SelectOutputFolder { get { return _ru ? "Выберите папку для подписанных PDF" : "Select output folder for signed PDFs"; } }
    }
}
