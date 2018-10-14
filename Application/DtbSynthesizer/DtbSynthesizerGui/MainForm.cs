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

        public ChangableDictionary<CultureInfo, IXmlSynthesizer> SynthesizersByLanguage { get; } 

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

        }

        public XDocument XhtmlDocument
        {
            get => xhtmlDocument;
            set
            {
                xhtmlDocument = value;
                UpdateBrowserView();
                ResetSynthesizers();
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
                synthesizersDataGridView.Rows.Add(key, SynthesizersByLanguage[key].VoiceInfo.Type, SynthesizersByLanguage[key].VoiceInfo.Name);
            }

        }

        public MainForm()
        {
            InitializeComponent();
            SynthesizersByLanguage = new ChangableDictionary<CultureInfo, IXmlSynthesizer>();
            SynthesizersByLanguage.CollectionChanged += (sender, args) => UpdateSynthesizersView();
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
                Filter = "html (*.htm;*.html)|*.htm;*.html|xml|*.xml|ePub|*.epub",
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
            synthesizeProgressBar.Value = 0;
        }

        private void SynthesizeBackgroundWorkerDoWorkHandler(object sender, DoWorkEventArgs e)
        {
            var worker = sender as BackgroundWorker;
            if (worker == null)
            {
                return;
            }
            var outputDirectory = e.Argument as string;
            if (outputDirectory == null)
            {
                throw new ApplicationException("No output directory was given");
            }
            if (!Directory.Exists(outputDirectory))
            {
                throw new ApplicationException($"Output directory {outputDirectory} does not exist");
            }
            var entries = new DirectoryInfo(outputDirectory).GetFileSystemInfos();
            for (int i = 0; i < entries.Length; i++)
            {
                worker.ReportProgress(100*i/entries.Length, $"Emptying output directory {outputDirectory} (entry {i}/{entries.Length})");
                if (worker.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }
                if (entries[i] is DirectoryInfo di)
                {
                    di.Delete(true);
                }
                if (entries[i] is FileInfo fi)
                {
                    fi.Delete();
                }
            }
            var xhtmlPath = Path.Combine(
                outputDirectory,
                Path.GetFileNameWithoutExtension(new Uri(XhtmlDocument.BaseUri).LocalPath) + ".html");
            var xhtmlUri = new Uri(xhtmlPath);
            var sourceUri = new Uri(XhtmlDocument.BaseUri);
            var imageSrcs = XhtmlDocument
                .Descendants(Utils.XhtmlNs + "img")
                .Select(img => img.Attribute("src")?.Value)
                .Where(src => !String.IsNullOrWhiteSpace(src))
                .Distinct(StringComparer.InvariantCultureIgnoreCase)
                .Where(src => Uri.IsWellFormedUriString(src, UriKind.Relative))
                .ToArray();
            for (int i = 0; i < imageSrcs.Length; i++)
            {
                worker.ReportProgress(100 * i / imageSrcs.Length, $"Copying image {imageSrcs[i]} (entry {i}/{imageSrcs.Length})");
                if (worker.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }
                var source = new Uri(sourceUri, imageSrcs[i]).LocalPath;
                var dest = new Uri(xhtmlUri, imageSrcs[i]).LocalPath;
                var destDir = Path.GetDirectoryName(dest);
                if (destDir != null && !Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }
                File.Copy(source, dest);
            }
            XhtmlDocument.Save(xhtmlPath);
            var synthByCulture = new ReadOnlyDictionary<CultureInfo, IXmlSynthesizer>(SynthesizersByLanguage);
            var synthesizer = new XhtmlSynthesizer()
            {
                XhtmlDocument = XDocument.Load(xhtmlPath, LoadOptions.SetBaseUri|LoadOptions.SetLineInfo),
                EncodeMp3 = true,
                Mp3BitRate = 48,
                SynthesizerSelector = info => synthByCulture.ContainsKey(info) ? synthByCulture[info] : Utils.GetPrefferedXmlSynthesizerForCulture(info)
            };
            synthesizer.Progress += (s, a) =>
            {
                worker.ReportProgress(a.ProgressPercentage, a.ProgressMessage);
                if (worker.CancellationPending)
                {
                    a.Cancel = true;
                }
            };
            synthesizer.Synthesize();
            synthesizer.GenerateDaisy202SmilFiles();
            synthesizer.GenerateNccDocument();
            synthesizer.NccDocument.Save(Path.Combine(outputDirectory, "ncc.html"));
            foreach (var smilFile in synthesizer.SmilFiles)
            {
                smilFile.Value.Save(Path.Combine(outputDirectory, smilFile.Key));
            }
            if (worker.CancellationPending)
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
    }
}
