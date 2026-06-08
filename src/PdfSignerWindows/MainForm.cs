using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using PdfSignerWindows.Models;
using PdfSignerWindows.Services;

namespace PdfSignerWindows
{
    public sealed class MainForm : Form
    {
        private readonly Localization _text = new Localization();
        private readonly CertificateService _certificateService = new CertificateService();
        private readonly CadesComSigner _cadesSigner = new CadesComSigner();
        private readonly BindingList<string> _files = new BindingList<string>();
        private readonly BindingList<CertificateRow> _certificateRows = new BindingList<CertificateRow>();

        private ListBox _fileList;
        private DataGridView _certificateGrid;
        private TextBox _outputFolder;
        private TextBox _reason;
        private CheckBox _detachedSignature;
        private Button _signButton;
        private ProgressBar _progress;
        private Label _status;

        public MainForm()
        {
            InitializeComponent();
            LoadCertificates();
        }

        private void InitializeComponent()
        {
            Text = _text.AppTitle;
            MinimumSize = new Size(820, 560);
            StartPosition = FormStartPosition.CenterScreen;

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 1;
            root.RowCount = 6;
            root.Padding = new Padding(12);
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 34));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 122));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            Controls.Add(root);

            root.Controls.Add(BuildFilesPanel(), 0, 0);
            root.Controls.Add(BuildFileButtons(), 0, 1);
            root.Controls.Add(BuildCertificatesPanel(), 0, 2);
            root.Controls.Add(BuildOptionsPanel(), 0, 3);
            root.Controls.Add(BuildActionPanel(), 0, 4);

            _status = new Label();
            _status.Dock = DockStyle.Fill;
            _status.TextAlign = ContentAlignment.MiddleLeft;
            _status.Text = _text.Ready;
            root.Controls.Add(_status, 0, 5);
        }

        private Control BuildFilesPanel()
        {
            GroupBox group = new GroupBox();
            group.Text = _text.Files;
            group.Dock = DockStyle.Fill;

            _fileList = new ListBox();
            _fileList.Dock = DockStyle.Fill;
            _fileList.DataSource = _files;
            group.Controls.Add(_fileList);
            return group;
        }

        private Control BuildFileButtons()
        {
            FlowLayoutPanel panel = new FlowLayoutPanel();
            panel.Dock = DockStyle.Fill;
            panel.FlowDirection = FlowDirection.LeftToRight;
            panel.Padding = new Padding(0, 6, 0, 0);

            Button add = new Button();
            add.Text = _text.AddFiles;
            add.Width = 120;
            add.Click += delegate { AddPdfFiles(); };
            panel.Controls.Add(add);

            Button remove = new Button();
            remove.Text = _text.RemoveFile;
            remove.Width = 90;
            remove.Click += delegate
            {
                string selected = _fileList.SelectedItem as string;
                if (selected != null)
                {
                    _files.Remove(selected);
                }
            };
            panel.Controls.Add(remove);

            Button clear = new Button();
            clear.Text = _text.ClearFiles;
            clear.Width = 90;
            clear.Click += delegate { _files.Clear(); };
            panel.Controls.Add(clear);

            return panel;
        }

        private Control BuildCertificatesPanel()
        {
            GroupBox group = new GroupBox();
            group.Text = _text.Certificates;
            group.Dock = DockStyle.Fill;

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 1;
            layout.RowCount = 2;
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            group.Controls.Add(layout);

            _certificateGrid = new DataGridView();
            _certificateGrid.Dock = DockStyle.Fill;
            _certificateGrid.AutoGenerateColumns = false;
            _certificateGrid.AllowUserToAddRows = false;
            _certificateGrid.AllowUserToDeleteRows = false;
            _certificateGrid.ReadOnly = true;
            _certificateGrid.MultiSelect = false;
            _certificateGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _certificateGrid.DataSource = _certificateRows;
            _certificateGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "CN", DataPropertyName = "Name", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            _certificateGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Store", DataPropertyName = "Store", Width = 110 });
            _certificateGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Valid to", DataPropertyName = "ValidTo", Width = 120 });
            _certificateGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Thumbprint", DataPropertyName = "Thumbprint", Width = 220 });
            layout.Controls.Add(_certificateGrid, 0, 0);

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.Dock = DockStyle.Fill;
            buttons.FlowDirection = FlowDirection.LeftToRight;
            buttons.Padding = new Padding(0, 6, 0, 0);

            Button refresh = new Button();
            refresh.Text = _text.Refresh;
            refresh.Width = 100;
            refresh.Click += delegate { LoadCertificates(); };
            buttons.Controls.Add(refresh);
            layout.Controls.Add(buttons, 0, 1);

            return group;
        }

        private Control BuildOptionsPanel()
        {
            TableLayoutPanel panel = new TableLayoutPanel();
            panel.Dock = DockStyle.Fill;
            panel.ColumnCount = 3;
            panel.RowCount = 3;
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));

            Label outputLabel = new Label();
            outputLabel.Text = _text.OutputFolder;
            outputLabel.TextAlign = ContentAlignment.MiddleLeft;
            panel.Controls.Add(outputLabel, 0, 0);

            _outputFolder = new TextBox();
            _outputFolder.Dock = DockStyle.Fill;
            _outputFolder.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Signed PDFs");
            panel.Controls.Add(_outputFolder, 1, 0);

            Button browse = new Button();
            browse.Text = _text.Browse;
            browse.Click += delegate { SelectOutputFolder(); };
            panel.Controls.Add(browse, 2, 0);

            Label reasonLabel = new Label();
            reasonLabel.Text = _text.Reason;
            reasonLabel.TextAlign = ContentAlignment.MiddleLeft;
            panel.Controls.Add(reasonLabel, 0, 1);

            _reason = new TextBox();
            _reason.Dock = DockStyle.Fill;
            _reason.Text = _text.DefaultReason;
            panel.Controls.Add(_reason, 1, 1);
            panel.SetColumnSpan(_reason, 2);

            _detachedSignature = new CheckBox();
            _detachedSignature.Text = _text.DetachedSignature;
            _detachedSignature.Dock = DockStyle.Fill;
            _detachedSignature.TextAlign = ContentAlignment.MiddleLeft;
            panel.Controls.Add(_detachedSignature, 1, 2);
            panel.SetColumnSpan(_detachedSignature, 2);

            return panel;
        }

        private Control BuildActionPanel()
        {
            TableLayoutPanel panel = new TableLayoutPanel();
            panel.Dock = DockStyle.Fill;
            panel.ColumnCount = 2;
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            _signButton = new Button();
            _signButton.Text = _text.Sign;
            _signButton.Width = 120;
            _signButton.Height = 32;
            _signButton.Click += async delegate { await SignSelectedFiles(); };
            panel.Controls.Add(_signButton, 0, 0);

            _progress = new ProgressBar();
            _progress.Dock = DockStyle.Fill;
            _progress.Minimum = 0;
            panel.Controls.Add(_progress, 1, 0);

            return panel;
        }

        private void AddPdfFiles()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = _text.SelectPdfFilter;
            dialog.Multiselect = true;
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            foreach (string fileName in dialog.FileNames)
            {
                if (!_files.Contains(fileName))
                {
                    _files.Add(fileName);
                }
            }
        }

        private void SelectOutputFolder()
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = _text.SelectOutputFolder;
            dialog.SelectedPath = _outputFolder.Text;
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                _outputFolder.Text = dialog.SelectedPath;
            }
        }

        private void LoadCertificates()
        {
            _status.Text = _text.LoadingCertificates;
            _certificateRows.Clear();

            foreach (CertificateInfo certificate in _certificateService.LoadSigningCertificates())
            {
                _certificateRows.Add(new CertificateRow(certificate));
            }

            _status.Text = _cadesSigner.IsAvailable() ? _text.Ready : _text.CryptoProUnavailable;
        }

        private CertificateInfo GetSelectedCertificate()
        {
            if (_certificateGrid.CurrentRow == null)
            {
                return null;
            }

            CertificateRow row = _certificateGrid.CurrentRow.DataBoundItem as CertificateRow;
            return row == null ? null : row.Certificate;
        }

        private async Task SignSelectedFiles()
        {
            if (_files.Count == 0)
            {
                MessageBox.Show(this, _text.NoFiles, _text.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            CertificateInfo certificate = GetSelectedCertificate();
            if (certificate == null)
            {
                MessageBox.Show(this, _text.NoCertificate, _text.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!_cadesSigner.IsAvailable())
            {
                MessageBox.Show(this, _text.CryptoProUnavailable, _text.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string[] files = _files.ToArray();
            string outputFolder = _outputFolder.Text;
            string reason = _reason.Text;
            bool createDetachedSignature = _detachedSignature.Checked;
            _progress.Maximum = files.Length;
            _progress.Value = 0;
            _signButton.Enabled = false;

            try
            {
                PdfSigningService pdfSigningService = new PdfSigningService(_cadesSigner);
                for (int i = 0; i < files.Length; i++)
                {
                    string file = files[i];
                    _status.Text = _text.SignedCount(i, files.Length) + ": " + Path.GetFileName(file);
                    await Task.Run(delegate { pdfSigningService.SignPdf(file, outputFolder, certificate, reason, createDetachedSignature); });
                    _progress.Value = i + 1;
                    _status.Text = _text.SignedCount(i + 1, files.Length);
                }

                _status.Text = _text.Done;
                MessageBox.Show(this, _text.Done, _text.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _status.Text = _text.Failed + ": " + ex.Message;
                MessageBox.Show(this, ex.Message, _text.Failed, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _signButton.Enabled = true;
            }
        }

        private sealed class CertificateRow
        {
            public CertificateRow(CertificateInfo certificate)
            {
                Certificate = certificate;
                Name = certificate.DisplayName;
                Store = certificate.StoreName;
                ValidTo = certificate.NotAfter.ToString("yyyy-MM-dd");
                Thumbprint = certificate.Thumbprint;
            }

            public CertificateInfo Certificate { get; private set; }
            public string Name { get; private set; }
            public string Store { get; private set; }
            public string ValidTo { get; private set; }
            public string Thumbprint { get; private set; }
        }
    }
}
