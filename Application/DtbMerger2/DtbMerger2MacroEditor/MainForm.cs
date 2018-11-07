using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using DtbMerger2Library.Actions;
using DtbMerger2Library.Daisy202;
using Uri = System.Uri;

namespace DtbMerger2MacroEditor
{
    public partial class MainForm : Form
    {

        public MainForm()
        {
            InitializeComponent();
        }

        #region Undo/Redo

        private readonly Stack<IAction> undoStack = new Stack<IAction>();
        private readonly Stack<IAction> redoStack = new Stack<IAction>();
        private XDocument macro;

        public void DoAction(IAction action)
        {
            if (action.CanExecute)
            {
                SuspendLayout();
                try
                {
                    var postSelCand = GetPostActionSelectedMacroElementCandidates();
                    try
                    {
                        action.Execute();
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(
                            this,
                            $"An unexpected {e.GetType()} occured while performing action '{action.Description}': {e.Message}",
                            action.Description,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return;
                    }

                    if (action.CanUnExecute)
                    {
                        undoStack.Push(action);
                    }
                    else
                    {
                        undoStack.Clear();
                        redoStack.Clear();
                    }
                    SelectedMacroElement = postSelCand.FirstOrDefault(CanSelectMacroElement) ?? SelectedMacroElement;
                    UpdateEntryManipulationControls();
                }
                finally
                {
                    ResumeLayout();
                }
            }
        }

        private IEnumerable<XElement> GetPostActionSelectedMacroElementCandidates()
        {
            return new[]
            {
                SelectedMacroElement,
                SelectedMacroElement?.ElementsBeforeSelf().LastOrDefault(),
                SelectedMacroElement?.ElementsAfterSelf().FirstOrDefault(),
                SelectedMacroElement?.Parent
            }.Where(e => e != null);
        }

        public void Undo()
        {
            if (CanUndo)
            {
                SuspendLayout();
                try
                {
                    var postSelCand = GetPostActionSelectedMacroElementCandidates();
                    var action = undoStack.Pop();
                    try
                    {
                        action.UnExecute();
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(
                            this,
                            $"An unexpected {e.GetType()} occured while undoing action '{action.Description}':\n{e.Message}\nUndo/redo stacks will be cleared",
                            $"Undo '{action.Description}'",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        undoStack.Clear();
                        redoStack.Clear();
                        return;

                    }
                    redoStack.Push(action);
                    SelectedMacroElement = postSelCand.FirstOrDefault(CanSelectMacroElement) ?? SelectedMacroElement;
                    UpdateEntryManipulationControls();

                }
                finally
                {
                    ResumeLayout();
                }
            }
        }

        public void Redo()
        {
            if (CanRedo)
            {
                SuspendLayout();
                try
                {
                    var postSelCand = GetPostActionSelectedMacroElementCandidates();
                    var action = redoStack.Pop();
                    try
                    {
                        action.Execute();
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(
                            this,
                            $"An unexpected {e.GetType()} occured while redoing action '{action.Description}':\n{e.Message}.\nUndo/redo stacks will be cleared",
                            $"Undo '{action.Description}'",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        undoStack.Clear();
                        redoStack.Clear();
                    }
                    undoStack.Push(action);
                    SelectedMacroElement = postSelCand.FirstOrDefault(CanSelectMacroElement) ?? SelectedMacroElement;
                    UpdateEntryManipulationControls();

                }
                finally
                {
                    ResumeLayout();
                }
            }
        }

        public bool CanUndo => undoStack.Any() && undoStack.Peek().CanUnExecute;

        public bool CanRedo => redoStack.Any() && redoStack.Peek().CanExecute;
        

        #endregion

        private bool hasMacroChanged = false;

        public bool HasMacroChanged
        {
            get => hasMacroChanged;
            set
            {
                hasMacroChanged = value;
                Text = Text.TrimEnd('*') + (hasMacroChanged ? "*" : "");
            }
        }

        public XDocument Macro
        {
            get => macro;
            set
            {
                if (macro != value)
                {
                    if (macro != null)
                    {
                        macro.Changed -= MacroChangedHandler;
                    }
                    macro = value;
                    if (macro != null)
                    {
                        macro.Changed += MacroChangedHandler;
                    }
                    undoStack.Clear();
                    redoStack.Clear();
                    HasMacroChanged = false;
                    UpdateMacroView();
                    UpdatePropertiesView();
                    UpdateEntryManipulationControls();
                }
            }
        }

        private void MacroChangedHandler(Object o, XObjectChangeEventArgs xObjectChangeEventArgs)
        {
            HasMacroChanged = true;
            if (o is XAttribute attr)
            {
                if (attr.Parent == Macro?.Root)
                {
                    UpdateMacroView();
                }
                else
                {
                    UpdatePropertiesView();
                }
            }
            else if (o is XElement)
            {
                switch (xObjectChangeEventArgs.ObjectChange)
                {
                    case XObjectChange.Add:
                    case XObjectChange.Remove:
                    case XObjectChange.Value:
                        UpdateMacroView();
                        break;
                    case XObjectChange.Name:
                        UpdatePropertiesView();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            UpdateEntryManipulationControls();
        }

        public string MacroFileName =>
            String.IsNullOrWhiteSpace(Macro?.BaseUri)
                ? null
                : new Uri(Macro.BaseUri).LocalPath;

        public void UpdateMacroView()
        {
            var selectedMacroElement = SelectedMacroElement;
            macroTreeView.Nodes.Clear();
            if (Macro?.Root != null)
            {
                macroTreeView.Nodes.Add(new TreeNode(
                    $"Macro [{Macro.Root.Attribute("name")?.Value??"Unnamed"}]",
                    Macro.Root.Elements().Select(elem => new MacroEntry(elem)).Select(GetMacroEntryTreeNode).ToArray())
                    {
                        Tag = Macro.Root
                    });

                var selectedTreeNode = FindTreeNodeForMacroElement(selectedMacroElement);
                if (selectedTreeNode != null)
                {
                    macroTreeView.SelectedNode = selectedTreeNode;
                }
                UpdateMacroTreeNodeStates(macroTreeView.Nodes[0]);
            }
            Text = $"{Application.ProductName} {Application.ProductVersion} [{MacroFileName}]{(HasMacroChanged?"*":"")}";
            UpdateEntryManipulationControls();
        }

        private void UpdateMacroTreeNodeStates(TreeNode node)
        {
            if (node.Tag is MacroEntry macroEntry)
            {
                if (macroEntry.SourceElement?.Annotations<TreeNodeOpenAnnotation>().Any()??false)
                {
                    node.Expand();
                }
                else
                {
                    node.Collapse();
                }
            }

            foreach (TreeNode childNode in node.Nodes)
            {
                UpdateMacroTreeNodeStates(childNode);
            }
        }

        public bool CanSelectMacroElement(XElement elem)
        {
            return FindTreeNodeForMacroElement(elem) != null;
        }

        private TreeNode FindTreeNodeForMacroElement(XElement elem, TreeNode currentNode = null)
        {
            if (elem == Macro?.Root && macroTreeView.Nodes.Count > 0)
            {
                return macroTreeView.Nodes[0];
            }
            if (currentNode == null)
            {
                if (macroTreeView.Nodes.Count == 0)
                {
                    return null;
                }
                currentNode = macroTreeView.Nodes[0];
            }
            if (elem == null)
            {
                return null;
            }
            if (currentNode.Tag is MacroEntry macroEntry && macroEntry.SourceElement == elem)
            {
                return currentNode;
            }
            foreach (TreeNode node in currentNode.Nodes)
            {
                var candidateNode = FindTreeNodeForMacroElement(elem, node);
                if (candidateNode != null)
                {
                    return candidateNode;
                }
            }

            return null;
        }

        static TreeNode GetMacroEntryTreeNode(MacroEntry entry)
        {
            return new TreeNode(
                entry.ToString(),
                entry.SourceElement.Elements().Select(elem => new MacroEntry(elem)).Select(GetMacroEntryTreeNode)
                    .ToArray())
            {
                Tag = entry
            };
        }

        private void OpenMacroClickHandler(object sender, EventArgs e)
        {
            OpenMacro();
        }

        private void OpenMacro()
        {
            if (HasMacroChanged)
            {
                if (MessageBox.Show(
                        "The current macro has unsaved changed. If you continue the changes will be lost",
                        "EOpen Macro",
                        MessageBoxButtons.OKCancel,
                        MessageBoxIcon.Information) == DialogResult.Cancel)
                {
                    return;
                }
            }
            var ofd = new OpenFileDialog
            {
                Title = "Select macro file",
                CheckFileExists = true,
                Multiselect = false,
                ShowReadOnly = false,
                Filter = "Macro xml file|*.xml",
                FilterIndex = 0
            };
            if (ofd.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    Macro = XDocument.Load(ofd.FileName, LoadOptions.SetBaseUri | LoadOptions.SetLineInfo);
                    macroTreeView.ExpandAll();
                }
                catch (Exception e)
                {
                    MessageBox.Show(
                        this,
                        $"An {e.GetType()} occurred while loading macro {ofd.FileName}: {e.Message}",
                        "Load Macro",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }


        private void SaveMacro()
        {
            if (Macro == null)
            {
                return;
            }
            if (MacroFileName == null)
            {
                SaveMacroAs();
            }
            else
            {
                try
                {
                    Macro.Save(MacroFileName);
                    HasMacroChanged = false;
                    UpdateMacroView();
                }
                catch (Exception e)
                {
                    MessageBox.Show(
                        this,
                        $"Could not save macro as {Macro.BaseUri} due to an unexpected {e.GetType()}: {e.Message}",
                        "Save Macro",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        private void SaveMacroAs()
        {
            if (Macro == null)
            {
                return;
            }
            var sfd = new SaveFileDialog
            {
                Title = "Save Macro",
                CreatePrompt = true,
                OverwritePrompt = true,
                SupportMultiDottedExtensions = true,
                AddExtension = true,
                DefaultExt = "xml",
                Filter = "Xml files|*.xml|All files|*.*",
                FilterIndex = 0
            };
            if (sfd.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    Macro.Save(sfd.FileName);
                    Macro = XDocument.Load(sfd.FileName, LoadOptions.SetBaseUri | LoadOptions.SetLineInfo);
                }
                catch (Exception e)
                {
                    MessageBox.Show(
                        this,
                        $"Could not save macro as {sfd.FileName} due to an unexpected {e.GetType()}: {e.Message}",
                        "Save Macro",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        public MacroEntry SelectedMacroEntry => macroTreeView.SelectedNode?.Tag as MacroEntry;

        public XElement SelectedMacroElement
        {
            get => SelectedMacroEntry?.SourceElement??(macroTreeView.SelectedNode?.Tag as XElement);
            set => macroTreeView.SelectedNode = FindTreeNodeForMacroElement(value);
        }

        private void UpdatePropertiesView()
        {
            propertiesDataGridView.Rows.Clear();
            var macroElem = SelectedMacroElement;
            if (macroElem != null)
            {
                if (macroElem != Macro?.Root)
                {
                    propertiesDataGridView.Rows.Add(macroElem, "LocalName", macroElem.Name.LocalName);
                    propertiesDataGridView.Rows.Add(macroElem, "NameSpace", macroElem.Name.NamespaceName);
                }
                foreach (var attr in macroElem.Attributes())
                {
                    propertiesDataGridView.Rows.Add(attr, attr.Name, attr.Value);
                }
            }
        }

        private void MacroTreeViewAfterSelectHandler(object sender, TreeViewEventArgs e)
        {
            UpdatePropertiesView();
            UpdateEntryManipulationControls();
        }

        private void PropertiesDataGridViewCellValueChangedHandler(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex > -1 && e.ColumnIndex == 2)
            {
                var row = propertiesDataGridView.Rows[e.RowIndex];
                if (row.Cells[0].Value is XAttribute attr)
                {
                    DoAction(new SetAttributeAction(attr.Parent, attr.Name, row.Cells[2].Value.ToString()));
                }

                if (row.Cells[0].Value is XElement elem)
                {
                    var val = row.Cells[2].Value?.ToString().Trim()??"";
                    XName newName = null;
                    switch (row.Cells[1].Value.ToString())
                    {
                        case "LocalName":
                            newName = elem.Name.Namespace+val;
                            break;
                        case "NameSpace":
                            XNamespace ns = val;
                            newName = ns + elem.Name.LocalName;
                            break;
                    }
                    if (newName != elem.Name)
                    {
                        DoAction(new RenameElementAction(elem, newName));
                    }
                }
            }
        }

        private void Exit()
        {
            Close();
        }

        private void ExitClickHandler(object sender, EventArgs e)
        {
            Exit();
        }

        private void MoveEntryUpClickHandler(object sender, EventArgs e)
        {
            MoveEntryUp();
        }

        private void MoveEntryDownClickHandler(object sender, EventArgs e)
        {
            MoveEntryDown();
        }

        private void MoveEntryOutClickHandler(object sender, EventArgs e)
        {
            MoveEntryOut();
        }

        private void MoveEntryInClickHandler(object sender, EventArgs e)
        {
            MoveEntryIn();
        }

        public bool CanMoveUp => 
            SelectedMacroElement != null && new MoveElementUpAction(SelectedMacroElement).CanExecute;

        public bool CanMoveDown =>
            SelectedMacroElement != null && new MoveElementDownAction(SelectedMacroElement).CanExecute;

        public bool CanDeleteEntry =>
            SelectedMacroElement != null && new DeleteElementsAction(SelectedMacroElement).CanExecute;

        public bool CanMoveIn =>
            SelectedMacroElement != null && new MoveElementInAction(SelectedMacroElement).CanExecute;

        public bool CanMoveOut =>
            SelectedMacroElement != null && new MoveElementOutAction(SelectedMacroElement).CanExecute;

        public bool CanGenerateMergedDTB => Macro?.Root?.Elements().Any() ?? false;

        public bool CanInsertEntries =>
            SelectedMacroElement != null &&
            new AddElementsAction(SelectedMacroElement, new XElement[0], AddModes.InsertBefore).CanExecute;

        private void MoveEntryUp()
        {
            DoAction(new MoveElementUpAction(SelectedMacroElement));
        }

        private void MoveEntryDown()
        {
            DoAction(new MoveElementDownAction(SelectedMacroElement));
        }

        private void MoveEntryIn()
        {
            DoAction(new MoveElementInAction(SelectedMacroElement));
        }

        private void MoveEntryOut()
        {
            DoAction(new MoveElementOutAction(SelectedMacroElement));
        }

        public void UpdateEntryManipulationControls()
        {
            moveEntryDownButton.Enabled = CanMoveDown;
            moveEntryDownToolStripMenuItem.Enabled = CanMoveDown;
            moveEntryUpButton.Enabled = CanMoveUp;
            moveEntryUpToolStripMenuItem.Enabled = CanMoveUp;
            moveEntryInButton.Enabled = CanMoveIn;
            moveEntryInToolStripMenuItem.Enabled = CanMoveIn;
            moveEntryOutButton.Enabled = CanMoveOut;
            moveEntryOutToolStripMenuItem.Enabled = CanMoveOut;
            deleteEntryToolStripMenuItem.Enabled = CanDeleteEntry;
            deleteEntryButton.Enabled = CanDeleteEntry;
            insertDTBButton.Enabled = CanInsertEntries;
            insertDTBToolStripMenuItem.Enabled = CanInsertEntries;
            undoToolStripMenuItem.Enabled = CanUndo;
            undoButton.Enabled = CanUndo;
            mainToolTip.SetToolTip(
                undoButton,
                undoStack.Any() ? undoStack.Select(a => a.Description).Aggregate((t, d) => $"{t}\n{d}") : "");
            redoToolStripMenuItem.Enabled = CanRedo;
            redoButton.Enabled = CanRedo;
            mainToolTip.SetToolTip(
                redoButton,
                redoStack.Any() ? redoStack.Select(a => a.Description).Aggregate((t, d) => $"{t}\n{d}") : "");
            generateMergedDtbToolStripMenuItem.Enabled = CanGenerateMergedDTB;
        }

        private void ReloadMacroEntriesClickHandler(object sender, EventArgs e)
        {
            UpdateMacroView();
        }

        private void MainFormLoadHandler(object sender, EventArgs e)
        {
            UpdateEntryManipulationControls();
            HideProgressControls();
            Text = $"{Application.ProductName} v{Application.ProductVersion}";
        }

        private void DeleteEntryClickHandler(object sender, EventArgs e)
        {
            DeleteEntry();
        }

        private void DeleteEntry()
        {
            DoAction(new DeleteElementsAction(SelectedMacroElement));
        }

        private IEnumerable<XElement> LoadMacroEntriesFromNcc(string title)
        {
            var ofd = new OpenFileDialog
            {
                Title = title,
                CheckFileExists = true,
                Multiselect = false,
                Filter = "ncc|ncc.htm*",
                FilterIndex = 0
            };
            if (ofd.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    return MacroEntry.GetMacroElementsFromNcc(new Uri(ofd.FileName));
                }
                catch (Exception e)
                {
                    MessageBox.Show(
                        this,
                        $"Could not load macro elements from ncc {ofd.FileName} due to an unexpected {e.GetType()}: {e.Message}",
                        title,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }

            return null;
        }

        private void InsertDTB()
        {
            if (SelectedMacroEntry != null)
            {
                var elements = LoadMacroEntriesFromNcc("Select ncc of DTB from which to insert macro elements");
                if (elements == null)
                {
                    return;
                }
                DoAction(new AddElementsAction(SelectedMacroElement, elements, AddModes.InsertBefore, "Insert DTB"));
            }

        }

        private void InsertDTBClickHandler(object sender, EventArgs e)
        {
            InsertDTB();
        }

        private void AddDTBClickHandler(object sender, EventArgs e)
        {
            AddDTB();
        }

        private void AddDTB()
        {
            var elements = LoadMacroEntriesFromNcc("Select ncc of DTB from which to add macro elements");
            if (elements == null)
            {
                return;
            }
            if (Macro?.Root == null)
            {
                Macro = new XDocument(new XElement("Macro", elements));
                return;
            }

            DoAction(new AddElementsAction(
                SelectedMacroElement ?? Macro.Root, 
                elements, 
                AddModes.AddAsChildren,
                "Add DTB"));
        }

        private void SaveMacroClickHandler(object sender, EventArgs e)
        {
            SaveMacro();
        }

        private void MainFormFormClosingHandler(object sender, FormClosingEventArgs e)
        {
            if (HasMacroChanged)
            {
                if (MessageBox.Show(
                        this,
                        "The current macro has unsaved changed. If you continue the changes will be lost",
                        "Exit Macro Editor",
                        MessageBoxButtons.OKCancel,
                        MessageBoxIcon.Information) == DialogResult.Cancel)
                {
                    e.Cancel = true;
                }
            }
        }

        private void UndoClickHandler(object sender, EventArgs e)
        {
            Undo();
        }

        private void RedoClickHandler(object sender, EventArgs e)
        {
            Redo();
        }

        private class TreeNodeOpenAnnotation
        {
        }

        private void MacroTreeViewAfterExpandHandler(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Tag is MacroEntry macroEntry)
            {
                macroEntry.SourceElement?.AddAnnotation(new TreeNodeOpenAnnotation());
            }
        }

        private void MacroTreeViewAfterCollapseHandler(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Tag is MacroEntry macroEntry)
            {
                macroEntry.SourceElement?.RemoveAnnotations<TreeNodeOpenAnnotation>();
            }
        }

        private void GenerateMergedDtbClickHandler(object sender, EventArgs e)
        {
            GenerateMergedDtb();
        }

        private void EnableProgressControls()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(EnableProgressControls));
            }
            else
            {
                buildProgressBar.Visible = true;
                cancelButton.Visible = true;
                cancelButton.Enabled = true;
                cancelButton.Tag = false;
                cancelButton.Click += CancelClickHandler;
            }
        }

        

        private void DisableProgressControls()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(DisableProgressControls));
            }
            else
            {
                buildProgressBar.Visible = false;
                cancelButton.Visible = false;
                cancelButton.Enabled = false;
                cancelButton.Click -= CancelClickHandler;
            }
        }

        private void CancelClickHandler(object sender, EventArgs e)
        {
            cancelButton.Tag = true;
        }

        private void SetProgress(int progressPct, string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<int, string>(SetProgress), progressPct, message);
            }
            else
            {
                buildProgressBar.Value = progressPct;
            }
        }

        private void ShowMessage(string message, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string, string, MessageBoxButtons, MessageBoxIcon>(ShowMessage), message, caption, buttons, icon);
            }
            else
            {
                MessageBox.Show(this, message, caption, buttons, icon);
            }
        }

        private bool SaveDtbProgressHandler(int progressPercentage, string progressMessage)
        {
            SetProgress(progressPercentage, progressMessage);
            return (bool)cancelButton.Tag;

        }

        private void SaveDtb(DtbBuilder builder, string path)
        {
            EnableProgressControls();
            try
            {
                if (builder.SaveDtb(path, SaveDtbProgressHandler))
                {
                    ShowMessage(
                        $"Successfully generated merged DTB from macro and saved to\n{path}",
                        "Generate Merged DTB",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception e)
            {
                ShowMessage(
                    $"An {e.GetType()} occured while saving DTB: {e.Message}",
                    "Generate Merged DTB",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

            }
            finally
            {
                DisableProgressControls();
            }
        }
        private void GenerateMergedDtb()
        {
            if (CanGenerateMergedDTB)
            {
                var process = "";
                try
                {
                    process = "loading merge entries from Macro";
                    var builder = new DtbBuilder(MergeEntry.LoadMergeEntriesFromMacro(Macro));
                    var fbd = new FolderBrowserDialog
                    {
                        Description = "Select output folder for DTB",
                        ShowNewFolderButton = true
                    };
                    if (!String.IsNullOrWhiteSpace(MacroFileName) &&
                        Directory.Exists(Path.GetDirectoryName(MacroFileName)))
                    {
                        fbd.SelectedPath = Path.GetDirectoryName(MacroFileName);
                    }

                    if (fbd.ShowDialog(this) == DialogResult.OK)
                    {
                        if (Directory.Exists(fbd.SelectedPath))
                        {
                            if (Directory.GetFileSystemEntries(fbd.SelectedPath).Any())
                            {
                                if (MessageBox.Show(
                                        this,
                                        $"Output folder\n{fbd.SelectedPath}\nis not empty and if you continue, it's content will be purged.\nDo you wish to continue?",
                                        "Generate Merged DTB",
                                        MessageBoxButtons.YesNo,
                                        MessageBoxIcon.Question) == DialogResult.No)
                                {
                                    return;
                                }
                            }
                        }
                        process = "Generating DTB";
                        ShowProgressControls();
                        saveDtbBackgroundWorker.RunWorkerAsync(new Tuple<DtbBuilder, string>(builder, fbd.SelectedPath));
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(
                        this,
                        $"An {e.GetType()} occured while {process}: {e.Message}",
                        "Generate Merged DTB",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }
            }
        }

        private void HideProgressControls()
        {
            buildProgressBar.Visible = false;
            buildMessageLabel.Visible = false;
            cancelButton.Visible = false;
            cancelButton.Enabled = false;
            mainTableLayoutPanel.RowStyles[4].Height = 0;
        }

        private void ShowProgressControls()
        {
            buildProgressBar.Visible = true;
            buildMessageLabel.Visible = true;
            cancelButton.Visible = true;
            cancelButton.Enabled = true;
            mainTableLayoutPanel.RowStyles[4].Height = mainTableLayoutPanel.RowStyles[5].Height;

        }

        private void GenerateDtbBackgroundWorkerDoWorkHandler(object sender, DoWorkEventArgs e)
        {
            var bw = (BackgroundWorker) sender;
            var builder = ((Tuple<DtbBuilder, string>) e.Argument).Item1;
            var path = ((Tuple<DtbBuilder, string>)e.Argument).Item2;

            bool ProgressDelegate(int pct, string msg)
            {
                Debug.Print($"{pct} - {msg}");
                bw.ReportProgress(pct, msg);
                return bw.CancellationPending;
            }

            if (builder.BuildDtb(ProgressDelegate))
            {
                if (!builder.SaveDtb(path, ProgressDelegate))
                {
                    e.Cancel = true;
                }
            }
            else
            {
                e.Cancel = true;
            }
        }

        private void GenerateDtbBackgroundWorkerProgressChangedHandler(object sender, ProgressChangedEventArgs e)
        {
            buildProgressBar.Value = e.ProgressPercentage;
            buildMessageLabel.Text = (string) e.UserState;
            buildProgressBar.Update();
        }

        private void CancelButtonClickHandler(object sender, EventArgs e)
        {
            var bw = (BackgroundWorker) sender;
            if (bw.IsBusy)
            {
                bw.CancelAsync();
                cancelButton.Enabled = false;
            }
        }

        private void SaveDtbBackgroundWorkerRunWorkerCompletedHandler(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!e.Cancelled)
            {
                if (e.Error != null)
                {
                    ShowMessage(
                        $"An {e.Error.GetType()} occured while saving DTB: {e.Error.Message}",
                        "Generate Merged DTB",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                }
                else
                {
                    ShowMessage(
                        $"Succesfully generated and saved merged DTB from macro",
                        "Generate Merged DTB",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                }
            }
            HideProgressControls();
        }
    }
}
