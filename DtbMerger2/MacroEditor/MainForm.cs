using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using DtbMerger2Library.Actions;
using DtbMerger2Library.Daisy202;
using Uri = System.Uri;

namespace MacroEditor
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
            switch (xObjectChangeEventArgs.ObjectChange)
            {
                case XObjectChange.Add:
                case XObjectChange.Remove:
                    UpdateMacroView();
                    break;
                case XObjectChange.Name:
                case XObjectChange.Value:
                    UpdatePropertiesView();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
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
                    Macro.Root.Elements().Select(elem => new MacroEntry(elem)).Select(GetMacroEntryTreeNode).ToArray()));
                var selectedTreeNode = FindTreeNodeForMacroElement(selectedMacroElement);
                if (selectedTreeNode != null)
                {
                    macroTreeView.SelectedNode = selectedTreeNode;
                }
                UpdateMacroTreeNodeStates(macroTreeView.Nodes[0]);
            }
            Text = $"{Application.ProductName} [{MacroFileName}]{(HasMacroChanged?"*":"")}";
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
            get => SelectedMacroEntry?.SourceElement;
            set => macroTreeView.SelectedNode = FindTreeNodeForMacroElement(value);
        }

        private void UpdatePropertiesView()
        {
            propertiesDataGridView.Rows.Clear();
            var macroEntry = SelectedMacroEntry;
            if (macroEntry != null)
            {
                propertiesDataGridView.Rows.Add(macroEntry.SourceElement, "LocalName", macroEntry.SourceElement.Name.LocalName);
                propertiesDataGridView.Rows.Add(macroEntry.SourceElement, "NameSpace", macroEntry.SourceElement.Name.NamespaceName);
                foreach (var attr in macroEntry.SourceElement.Attributes())
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

        private void PropertiesDataGridViewCellEndEditHandler(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 2)
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
                        DoAction(new RenameEntryAction(elem, newName));
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
            SelectedMacroElement != null && new MoveEntryUpAction(SelectedMacroElement).CanExecute;

        public bool CanMoveDown =>
            SelectedMacroElement != null && new MoveEntryDownAction(SelectedMacroElement).CanExecute;

        public bool CanDeleteEntry =>
            SelectedMacroElement != null && new DeleteEntryAction(SelectedMacroElement).CanExecute;

        public bool CanMoveIn =>
            SelectedMacroElement != null && new MoveEntryInAction(SelectedMacroElement).CanExecute;

        public bool CanMoveOut =>
            SelectedMacroElement != null && new MoveEntryOutAction(SelectedMacroElement).CanExecute;

        public bool CanGenerateMergedDTB => Macro?.Root?.Elements().Any() ?? false;

        public bool CanInsertEntries =>
            SelectedMacroElement != null &&
            new AddEntriesAction(SelectedMacroElement, new XElement[0], AddModes.InsertBefore).CanExecute;

        private void MoveEntryUp()
        {
            DoAction(new MoveEntryUpAction(SelectedMacroElement));
        }

        private void MoveEntryDown()
        {
            DoAction(new MoveEntryDownAction(SelectedMacroElement));
        }

        private void MoveEntryIn()
        {
            DoAction(new MoveEntryInAction(SelectedMacroElement));
        }

        private void MoveEntryOut()
        {
            DoAction(new MoveEntryOutAction(SelectedMacroElement));
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
        }

        private void DeleteEntryClickHandler(object sender, EventArgs e)
        {
            DeleteEntry();
        }

        private void DeleteEntry()
        {
            DoAction(new DeleteEntryAction(SelectedMacroElement));
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
                DoAction(new AddEntriesAction(SelectedMacroElement, elements, AddModes.InsertBefore, "Insert DTB"));
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

            DoAction(new AddEntriesAction(
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



        private void GenerateMergedDtb()
        {
            if (CanGenerateMergedDTB)
            {
                var process = "";
                UseWaitCursor = true;
                try
                {
                    process = "loading merge entries from Macro";
                    var builder = new DtbBuilder(MergeEntry.LoadMergeEntriesFromMacro(Macro));
                    process = "building DTB";
                    builder.BuildDtb();
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
                        process = "saving DTB";
                        builder.SaveDtb(fbd.SelectedPath);
                        MessageBox.Show(
                            this,
                            $"Succesfully generated merged DTB from macro and saved to\n{fbd.SelectedPath}",
                            "Generate Merged DTB",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
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
                finally
                {
                    UseWaitCursor = false;
                }
            }
        }
    }
}
