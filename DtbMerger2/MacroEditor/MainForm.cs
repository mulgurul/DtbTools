using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using DtbMerger2Library.Daisy202;
using Uri = System.Uri;

namespace MacroEditor
{
    public partial class MainForm : Form
    {
        private Boolean hasMacroChanged = false;

        public bool HasMacroChanged
        {
            get => hasMacroChanged;
            set
            {
                hasMacroChanged = value;
                Text = Text.TrimEnd('*') + (hasMacroChanged ? "*" : "");
            }
        }

        public MainForm()
        {
            InitializeComponent();
        }

        public XDocument Macro { get; set; }

        public string MacroFileName =>
            Macro?.BaseUri == null
                ? null
                : new Uri(Macro.BaseUri).LocalPath;

        public void UpdateMacroView()
        {
            var selectedMacroEntry = SelectedMacroEntry;
            macroTreeView.Nodes.Clear();
            if (Macro?.Root != null)
            {
                macroTreeView.Nodes.Add(new TreeNode(
                    $"Macro [{Macro.Root.Attribute("name")?.Value??"Unnamed"}]",
                    Macro.Root.Elements().Select(elem => new MacroEntry(elem)).Select(GetMacroEntryTreeNode).ToArray()));
                var selectedTreeNode = FindMacroElement(selectedMacroEntry?.SourceElement, macroTreeView.Nodes[0]);
                if (selectedTreeNode != null)
                {
                    macroTreeView.SelectedNode = selectedTreeNode;
                }
            }
            Text = $"{Application.ProductName} [{MacroFileName}]{(HasMacroChanged?"*":"")}";
            UpdateEntryManipulationControls();
        }

        private TreeNode FindMacroElement(XElement elem, TreeNode currentNode)
        {
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
                var candidateNode = FindMacroElement(elem, node);
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
                Macro = XDocument.Load(ofd.FileName, LoadOptions.SetBaseUri | LoadOptions.SetLineInfo);
                HasMacroChanged = false;
                Macro.Changed += (sender, args) => { HasMacroChanged = true; };
                UpdateMacroView();
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
                    Macro.Changed += (sender, args) => { HasMacroChanged = true; };
                    HasMacroChanged = false;
                    UpdateMacroView();
                }
                catch (Exception e)
                {
                    MessageBox.Show(
                        $"Could not save macro as {sfd.FileName} due to an unexpected {e.GetType()}: {e.Message}",
                        "Save Macro",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        public MacroEntry SelectedMacroEntry => macroTreeView.SelectedNode?.Tag as MacroEntry;

        private void MacroTreeViewAfterSelectHandler(object sender, TreeViewEventArgs e)
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
            UpdateEntryManipulationControls();
        }

        private void PropertiesDataGridViewCellEndEditHandler(object sender, DataGridViewCellEventArgs e)
        {
            Debug.Print($"End edit ({e.ColumnIndex},{e.RowIndex}), new value is {propertiesDataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value}");
            if (e.ColumnIndex == 2)
            {
                var row = propertiesDataGridView.Rows[e.RowIndex];
                if (row.Cells[0].Value is XAttribute attr)
                {
                    attr.Value = row.Cells[2].Value.ToString();
                }

                if (row.Cells[0].Value is XElement elem)
                {
                    var val = row.Cells[2].Value?.ToString().Trim()??"";
                    switch (row.Cells[1].Value.ToString())
                    {
                        case "LocalName":
                            elem.Name = elem.Name.Namespace+ val;
                            break;
                        case "NameSpace":
                            XNamespace ns = val;
                            elem.Name = ns + elem.Name.LocalName;
                            break;
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

        private void DefaultifyNodeAndElem(ref TreeNode treeNode, ref XElement elem)
        {
            if (treeNode == null)
            {
                treeNode = macroTreeView.SelectedNode;
            }
            if (elem == null)
            {
                elem = (treeNode?.Tag as MacroEntry)?.SourceElement;
            }
        }

        public bool CanMoveUp(TreeNode treeNode = null, XElement elem = null)
        {
            DefaultifyNodeAndElem(ref treeNode, ref elem);
            if (treeNode != null && elem != null)
            {
                return treeNode.PrevNode != null && elem.ElementsBeforeSelf().Any();
            }
            return false;
        }

        private bool CanMoveDown(TreeNode treeNode = null, XElement elem = null)
        {
            DefaultifyNodeAndElem(ref treeNode, ref elem);
            if (treeNode != null && elem != null)
            {
                return treeNode.NextNode != null && elem.ElementsAfterSelf().Any();
            }
            return false;
        }
        private bool CanMoveIn(TreeNode treeNode = null, XElement elem = null)
        {
            return CanMoveUp(treeNode, elem);
        }
        private bool CanMoveOut(TreeNode treeNode = null, XElement elem = null)
        {
            DefaultifyNodeAndElem(ref treeNode, ref elem);
            if (treeNode != null && elem != null)
            {
                return (treeNode.Parent.Tag as MacroEntry) != null;
            }
            return false;
        }

        private bool CanDeleteEntry(TreeNode treeNode = null, XElement elem = null)
        {
            DefaultifyNodeAndElem(ref treeNode, ref elem);
            return treeNode != null && elem != null;
        }

        private bool CanInsertDTBAfterEntry(TreeNode treeNode = null, XElement elem = null)
        {
            return CanDeleteEntry();
        }

        private void MoveEntryUp()
        {
            var treeNode = macroTreeView.SelectedNode;
            var elem = SelectedMacroEntry?.SourceElement;
            if (CanMoveUp(treeNode, elem) && elem!=null)
            {
                var prevElem = elem.ElementsBeforeSelf().Last();
                var prevTreeNode = treeNode.PrevNode;
                elem.Remove();
                prevElem.AddBeforeSelf(elem);
                treeNode.Remove();
                prevTreeNode.Parent.Nodes.Insert(prevTreeNode.Index, treeNode);
                macroTreeView.SelectedNode = treeNode;
            }
        }

        private void MoveEntryDown()
        {
            var treeNode = macroTreeView.SelectedNode;
            var elem = SelectedMacroEntry?.SourceElement;
            if (CanMoveDown(treeNode, elem) && elem != null)
            {
                var nextElem = elem.ElementsAfterSelf().First();
                var nextTreeNode = treeNode.NextNode;
                elem.Remove();
                nextElem.AddAfterSelf(elem);
                treeNode.Remove();
                nextTreeNode.Parent.Nodes.Insert(nextTreeNode.Index + 1, treeNode);
                macroTreeView.SelectedNode = treeNode;
            }
        }

        private void MoveEntryIn()
        {
            var treeNode = macroTreeView.SelectedNode;
            var elem = SelectedMacroEntry?.SourceElement;
            if (CanMoveIn(treeNode, elem) && elem != null)
            {
                var prevElem = elem.ElementsBeforeSelf().Last();
                var prevTreeNode = treeNode.PrevNode;
                elem.Remove();
                prevElem.Add(elem);
                treeNode.Remove();
                prevTreeNode.Nodes.Add(treeNode);
                macroTreeView.SelectedNode = treeNode;
            }
        }

        private void MoveEntryOut()
        {
            var treeNode = macroTreeView.SelectedNode;
            var elem = SelectedMacroEntry?.SourceElement;
            if (CanMoveOut(treeNode, elem) && elem != null)
            {
                var parent = elem.Parent;
                var parentTreeNode = treeNode.Parent;
                elem.Remove();
                parent?.AddAfterSelf(elem);
                treeNode.Remove();
                parentTreeNode.Parent.Nodes.Insert(parentTreeNode.Index + 1, treeNode);
                macroTreeView.SelectedNode = treeNode;
            }
        }

        public void UpdateEntryManipulationControls()
        {
            moveEntryDownButton.Enabled = CanMoveDown();
            moveEntryDownToolStripMenuItem.Enabled = moveEntryDownButton.Enabled;
            moveEntryUpButton.Enabled = CanMoveUp();
            moveEntryUpToolStripMenuItem.Enabled = moveEntryUpButton.Enabled;
            moveEntryInButton.Enabled = CanMoveIn();
            moveEntryInToolStripMenuItem.Enabled = moveEntryInButton.Enabled;
            moveEntryOutButton.Enabled = CanMoveOut();
            moveEntryOutToolStripMenuItem.Enabled = moveEntryOutButton.Enabled;
            deleteEntryButton.Enabled = CanDeleteEntry();
            insertDTBButton.Enabled = CanInsertDTBAfterEntry();
            insertDTBToolStripMenuItem.Enabled = insertDTBButton.Enabled;
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
            var treeNode = macroTreeView.SelectedNode;
            var elem = SelectedMacroEntry?.SourceElement;
            if (CanDeleteEntry(treeNode, elem) && elem != null)
            {
                if (elem.Elements().Any())
                {
                    switch (MessageBox.Show(
                        "Do you with to delete the child entries as well? Choosing no will replace the entry with it's child entries",
                        "Delete Entry",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button1))
                    {
                        case DialogResult.Yes:
                            break;
                        case DialogResult.No:
                            while (elem.HasElements)
                            {
                                var child = elem.Elements().Last();
                                child.Remove();
                                elem.AddAfterSelf(child);
                            }

                            while (treeNode.Nodes.Count > 0)
                            {
                                var child = treeNode.Nodes[treeNode.Nodes.Count - 1];
                                child.Remove();
                                treeNode.Parent.Nodes.Insert(treeNode.Index + 1, child);
                            }
                            break;
                        default:
                            return;
                    }
                    macroTreeView.SelectedNode = treeNode.PrevNode ?? treeNode.Parent;
                    elem.Remove();
                    treeNode.Remove();
                }
            }
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
                SelectedMacroEntry.SourceElement.AddBeforeSelf(elements);
                UpdateMacroView();
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

            if (Macro == null)
            {
                Macro = new XDocument(new XElement("Macro", elements));
                Macro.Changed += (sender, args) =>
                {
                    HasMacroChanged = true;
                };
                HasMacroChanged = true;
            }
            else if (SelectedMacroEntry == null)
            {
                if (Macro.Root == null)
                {
                    Macro.Add(new XElement("Macro", elements));
                }
                else
                {
                    Macro.Root.Add(elements);
                }
            }
            else
            {
                SelectedMacroEntry.SourceElement.Add(elements);
            }
            UpdateMacroView();
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
                        "The current macro has unsaved changed. If you continue the changes will be lost",
                        "Exit Macro Editor",
                        MessageBoxButtons.OKCancel,
                        MessageBoxIcon.Information) == DialogResult.Cancel)
                {
                    e.Cancel = true;
                }
            }
        }
    }
}
