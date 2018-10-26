using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using DtbSynthesizerLibrary;
using DtbSynthesizerLibrary.Xhtml;
using DtbSynthesizerLibrary.Xml;

namespace DtbSynthesizerGui
{
    public partial class MainForm : Form
    {
        private XDocument xhtmlDocument;
        private AudioFormat selectedAudioFormat;

        private AudioFormat SelectedAudioFormat
        {
            get => selectedAudioFormat;
            set
            {
                selectedAudioFormat = value;
                audioFormatComboBox.SelectedItem = selectedAudioFormat;
            }
        }

        private class AudioFormat
        {
            public bool EncodeMp3 { get; set; }

            public int Mp3BitRate { get; set; }

            public override string ToString()
            {
                return EncodeMp3 ? $"MP3 {Mp3BitRate} kbps" : "WAVE PCM";
            }
        }

        public Dictionary<CultureInfo, IXmlSynthesizer> SynthesizersByLanguage { get; } 

        public void ResetSynthesizers()
        {
            SynthesizersByLanguage.Clear();
            if (XhtmlDocument != null)
            {
                SynthesizersByLanguage.Add(
                    CultureInfo.InvariantCulture,
                    Utils.GetPrefferedXmlSynthesizerForCulture(
                        XhtmlDocument
                            .Root
                            ?.Elements(Utils.XhtmlNs+"body")
                            .Select(Utils.GetLanguage)
                            .Where(v => !String.IsNullOrWhiteSpace(v))
                            .Select(lang => new CultureInfo(lang))
                            .FirstOrDefault() ?? CultureInfo.InvariantCulture));


                foreach (var key in XhtmlDocument
                    .Descendants()
                    .Select(Utils.GetLanguage)
                    .Where(v => !String.IsNullOrWhiteSpace(v))
                    .Select(lang => new CultureInfo(lang))
                    .Distinct())
                {

                    SynthesizersByLanguage.Add(key,Utils.GetPrefferedXmlSynthesizerForCulture(key, SynthesizersByLanguage[CultureInfo.InvariantCulture]));
                }
            }
            UpdateSynthesizersView();
        }

        public XDocument XhtmlDocument
        {
            get => xhtmlDocument;
            set
            {
                xhtmlDocument = value;
                UpdateBrowserView();
                UpdateMetadataView();
                ResetSynthesizers();
            }
        }

        private IEnumerable<Control> GetControls(Control control)
        {
            return control.Controls.OfType<Control>().SelectMany(ctrl => new[] {ctrl}.Union(GetControls(ctrl)));
        }

        private void UpdateMetadataView()
        {

            foreach (var textBox in GetControls(this).OfType<TextBox>())
            {
                if (textBox.Tag is string metaName && Regex.IsMatch(metaName, @"^meta:\w+:\w+$"))
                {
                    textBox.Text = Utils.GetMetaContent(XhtmlDocument, metaName.Substring(5));
                }
            }
        }

        private void UpdateBrowserView()
        {
            if (xhtmlDocument == null)
            {
                sourceXhtmlWebBrowser.DocumentText = "";
                return;
            }
            if (String.IsNullOrWhiteSpace(xhtmlDocument.BaseUri))
            {
                sourceXhtmlWebBrowser.DocumentText = xhtmlDocument.ToString();
                return;
            }
            var doc = new XDocument(xhtmlDocument);
            foreach (var img in doc.Descendants(Utils.XhtmlNs + "img"))
            {
                img.SetAttributeValue(
                    "src",
                    new Uri(new Uri(XhtmlDocument.BaseUri), img.Attribute("src")?.Value ?? ""));
            }
            sourceXhtmlWebBrowser.DocumentText = doc.ToString();
        }

        private void UpdateSynthesizersView()
        {
            synthesizersDataGridView.Rows.Clear();
            foreach (var key in SynthesizersByLanguage.Keys)
            {
                synthesizersDataGridView.Rows.Add(key, SynthesizersByLanguage[key].VoiceInfo.Description);
            }
        }

        public MainForm()
        {
            InitializeComponent();
            voiceColumn.DataSource = Utils.GetAllSynthesizers().Select(s => s.VoiceInfo.Description).ToList();
            SynthesizersByLanguage = new Dictionary<CultureInfo, IXmlSynthesizer>();
            var audioFormats = new List<AudioFormat>(
                new[] {32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320}
                    .Select(v => new AudioFormat() {EncodeMp3 = true, Mp3BitRate = v}));
            audioFormats.Add(new AudioFormat() {EncodeMp3 = false});
            audioFormatComboBox.DataSource = audioFormats;
            audioFormatComboBox.SelectedItem = audioFormats.First(af => af.EncodeMp3 && af.Mp3BitRate == 48);
        }

        private void OpenSourceButtonClickHandler(object sender, EventArgs e)
        {
            OpenSourceFile();
        }

        private void OpenSourceFile()
        {
            var ofd = new OpenFileDialog()
            {
                Title = "Open Source File",
                CheckFileExists = true,
                Multiselect = false,
                Filter = "dtbook|*.xml|html (*.htm;*.html)|*.htm;*.html",
                FilterIndex = 1
            };
            if (ofd.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    switch (ofd.FilterIndex)
                    {
                        case 1:
                        case 2:
                            var sourceDoc = XDocument.Load(ofd.FileName, LoadOptions.SetBaseUri | LoadOptions.SetLineInfo);
                            if (sourceDoc.Root == null)
                            {
                                throw new ApplicationException("Source document has no root element");
                            }
                            if (sourceDoc.Root?.Name == Utils.XhtmlNs + "html")
                            {
                                XhtmlDocument = sourceDoc;
                            }
                            else if (sourceDoc.Root?.Name?.LocalName == "dtbook")
                            {
                                XhtmlDocument = Utils.CloneWithBaseUri(Utils.TransformDtbookToXhtml(sourceDoc), sourceDoc.BaseUri);
                            }
                            else
                            {
                                throw new ApplicationException($"Unsupported root element {sourceDoc.Root.Name}");
                            }
                            break;
                        case 3:
                           throw new NotImplementedException("Epub source file loading not yet implemented");
                    }

                }
                catch (Exception e)
                {
                    MessageBox.Show(
                        this,
                        $"Could not load source document {ofd.FileName}: {e.Message}\nException {e.GetType()}\nStack Trace:\n{e.StackTrace}",
                        "Open Source File");

                }
            }
        }

        private void SynthesizeDtbButtonClickHandler(object sender, EventArgs e)
        {
            SynthesizeDtb();
        }

        private void SynthesizeDtb()
        {
            if (XhtmlDocument == null)
            {
                MessageBox.Show(
                    this,
                    "No source document is loaded",
                    "Synthesize DTB");
                return;
            }
            var fbd = new FolderBrowserDialog()
            {
                Description = "Select output directory",
                ShowNewFolderButton = true
            };
            if (fbd.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }
            if (Directory.GetFileSystemEntries(fbd.SelectedPath).Any())
            {
                if (MessageBox.Show(
                        this,
                        $"Output directory {fbd.SelectedPath} is not empty. If you continue, the directory will be emtied,",
                        "Synthesize DTB",
                        MessageBoxButtons.OKCancel,
                        MessageBoxIcon.Asterisk) == DialogResult.Cancel)
                {
                    return;
                }
            }
            if (!synthesizeBackgroundWorker.IsBusy)
            {
                openSourceButton.Enabled = false;
                synthesizeDtbButton.Enabled = false;
                cancelSynthesisButton.Visible = true;
                synthesizeProgressMessageLabel.Visible = true;
                synthesizeProgressBar.Visible = true;
                synthesizeBackgroundWorker.RunWorkerAsync(fbd.SelectedPath);
            }
        }

        private void ExitButtonClickHandler(object sender, EventArgs e)
        {
            Close();
        }

        private void SynthesizeBackgroundWorkerProgressChangedHandler(object sender, ProgressChangedEventArgs e)
        {
            synthesizeProgressMessageLabel.Text = e.UserState.ToString();
            synthesizeProgressBar.Value = e.ProgressPercentage;
            
        }

        private void SynthesizeBackgroundWorkerRunWorkerCompletedHandler(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                MessageBox.Show(
                    this,
                    "Synthesis was cancelled by the user",
                    "Synthesize DTB");
                
            }
            else if (e.Error != null)
            {
                MessageBox.Show(
                    this,
                    $"Could not synthesize DTB: {e.Error.Message} ({e.Error.GetType()})",
                    "Synthesize DTB",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                
            }
            else 
            {
                var outputDirectory = e.Result as string;
                if (outputDirectory == null)
                {
                    MessageBox.Show(
                        this,
                        $"No output directory was returned from the synthesis worker",
                        "Synthesize DTB",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
                else if (MessageBox.Show(
                        this,
                        $"DTB was succesfully synthesized to output directory {outputDirectory}. Do you wish to open the directory?",
                        "Synthesize DTB",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    Process.Start(outputDirectory);
                }
            }
            openSourceButton.Enabled = true;
            synthesizeDtbButton.Enabled = true;
            cancelSynthesisButton.Visible = false;
            synthesizeProgressMessageLabel.Text = "-";
            synthesizeProgressMessageLabel.Visible = false;
            synthesizeProgressBar.Visible = false;
            synthesizeProgressBar.Value = 0;
        }


        private void SynthesizeBackgroundWorkerDoWorkHandler(object sender, DoWorkEventArgs e)
        {
            var outputDirectory = e.Argument as string;
            var audioFormat = SelectedAudioFormat;
            if (audioFormat == null)
            {
                throw new ApplicationException("No audio format was selected");
            }
            if (!Utils.CopyXhtmlDocumentWithImages(XhtmlDocument, outputDirectory, out var xhtmlPath, (i, s) =>
            {
                synthesizeBackgroundWorker.ReportProgress(i, s);
                return synthesizeBackgroundWorker.CancellationPending;
            }))
            {
                e.Cancel = true;
                return;
            }
            var synthByCulture = new ReadOnlyDictionary<CultureInfo, IXmlSynthesizer>(SynthesizersByLanguage);
            var synthesizer = new XhtmlSynthesizer()
            {
                XhtmlDocument = XDocument.Load(xhtmlPath, LoadOptions.SetBaseUri|LoadOptions.SetLineInfo),
                EncodeMp3 = audioFormat.EncodeMp3,
                Mp3BitRate = audioFormat.Mp3BitRate,
                SynthesizerSelector = info => synthByCulture.ContainsKey(info) ? synthByCulture[info] : Utils.GetPrefferedXmlSynthesizerForCulture(info)
            };
            synthesizer.Progress += (s, a) =>
            {
                synthesizeBackgroundWorker.ReportProgress(a.ProgressPercentage, a.ProgressMessage);
                if (synthesizeBackgroundWorker.CancellationPending)
                {
                    a.Cancel = true;
                }
            };
            if (!synthesizer.GenerateDaisy202Dtb())
            {
                e.Cancel = true;
                return;
            }
            if (synthesizeBackgroundWorker.CancellationPending)
            {
                e.Cancel = true;
            }
            e.Result = outputDirectory;
        }

        private void CancelSynthesisButtonClickHandler(object sender, EventArgs e)
        {
            if (synthesizeBackgroundWorker.IsBusy && !synthesizeBackgroundWorker.CancellationPending)
            {
                synthesizeBackgroundWorker.CancelAsync();
            }
        }

        private void SynthesizersDataGridViewCellValueChangedHandler(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            var lang = synthesizersDataGridView.Rows[e.RowIndex].Cells[languageColumn.Index].Value as CultureInfo;
            var voiceDesc = synthesizersDataGridView.Rows[e.RowIndex].Cells[voiceColumn.Index].Value.ToString();
            if (e.ColumnIndex == voiceColumn.Index 
                && lang != null 
                && SynthesizersByLanguage.ContainsKey(lang) 
                && !String.IsNullOrWhiteSpace(voiceDesc))
            {
                SynthesizersByLanguage[lang] = Utils.GetAllSynthesizers()
                    .First(s => s.VoiceInfo.Description == voiceDesc);
            }
        }

        private void AudioFormatComboBoxSelectedValueChangedHandler(object sender, EventArgs e)
        {
            SelectedAudioFormat = audioFormatComboBox.SelectedItem as AudioFormat;
        }

        private void ResetSynthesizersButtonClickHandler(object sender, EventArgs e)
        {
            ResetSynthesizers();
        }

        private void MainFormFormClosingHandler(object sender, FormClosingEventArgs e)
        {
            if (synthesizeBackgroundWorker.IsBusy)
            {
                if (MessageBox.Show(
                        this,
                        "A DTB is currently being synthesized. If you close the application, the synthesis will be cancelled",
                        Text,
                        MessageBoxButtons.OKCancel,
                        MessageBoxIcon.Warning) == DialogResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
                synthesizeBackgroundWorker.CancelAsync();
                while (synthesizeBackgroundWorker.IsBusy)
                {
                    Application.DoEvents();
                }
            }
        }

        private void MetaDataTextBoxTextChangedHandler(object sender, EventArgs e)
        {
           if (XhtmlDocument != null && sender is TextBox textBox && textBox.Tag is string metaName && !String.IsNullOrWhiteSpace(metaName))
            {
                Utils.SetMeta(XhtmlDocument, metaName.Substring(5), textBox.Text);
            }
        }
    }
}
