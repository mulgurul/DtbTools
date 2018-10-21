namespace DtbSynthesizerGui
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.mainTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.exitButton = new System.Windows.Forms.Button();
            this.sourceXhtmlWebBrowser = new System.Windows.Forms.WebBrowser();
            this.openSourceButton = new System.Windows.Forms.Button();
            this.synthesizersDataGridView = new System.Windows.Forms.DataGridView();
            this.languageColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.voiceColumn = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.synthesizeDtbButton = new System.Windows.Forms.Button();
            this.synthesizeProgressMessageLabel = new System.Windows.Forms.Label();
            this.synthesizeProgressBar = new System.Windows.Forms.ProgressBar();
            this.cancelSynthesisButton = new System.Windows.Forms.Button();
            this.settingsTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.dcPublisherTextBox = new System.Windows.Forms.TextBox();
            this.dcCreatorTextBox = new System.Windows.Forms.TextBox();
            this.dcPublisherLabel = new System.Windows.Forms.Label();
            this.dcCreatorLabel = new System.Windows.Forms.Label();
            this.dcIdentifierLabel = new System.Windows.Forms.Label();
            this.audioFormatLabel = new System.Windows.Forms.Label();
            this.audioFormatComboBox = new System.Windows.Forms.ComboBox();
            this.dcIdentifierTextBox = new System.Windows.Forms.TextBox();
            this.resetSynthesizersButton = new System.Windows.Forms.Button();
            this.synthesizeBackgroundWorker = new System.ComponentModel.BackgroundWorker();
            this.mainTableLayoutPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.synthesizersDataGridView)).BeginInit();
            this.settingsTableLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainTableLayoutPanel
            // 
            this.mainTableLayoutPanel.ColumnCount = 6;
            this.mainTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.mainTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.mainTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.mainTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.mainTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.mainTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.mainTableLayoutPanel.Controls.Add(this.exitButton, 5, 4);
            this.mainTableLayoutPanel.Controls.Add(this.sourceXhtmlWebBrowser, 0, 0);
            this.mainTableLayoutPanel.Controls.Add(this.openSourceButton, 0, 4);
            this.mainTableLayoutPanel.Controls.Add(this.synthesizersDataGridView, 4, 0);
            this.mainTableLayoutPanel.Controls.Add(this.synthesizeDtbButton, 1, 4);
            this.mainTableLayoutPanel.Controls.Add(this.synthesizeProgressMessageLabel, 0, 2);
            this.mainTableLayoutPanel.Controls.Add(this.synthesizeProgressBar, 0, 3);
            this.mainTableLayoutPanel.Controls.Add(this.cancelSynthesisButton, 2, 4);
            this.mainTableLayoutPanel.Controls.Add(this.settingsTableLayoutPanel, 4, 1);
            this.mainTableLayoutPanel.Controls.Add(this.resetSynthesizersButton, 4, 4);
            this.mainTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainTableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.mainTableLayoutPanel.Name = "mainTableLayoutPanel";
            this.mainTableLayoutPanel.RowCount = 5;
            this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.mainTableLayoutPanel.Size = new System.Drawing.Size(1127, 867);
            this.mainTableLayoutPanel.TabIndex = 0;
            // 
            // exitButton
            // 
            this.exitButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.exitButton.Location = new System.Drawing.Point(940, 831);
            this.exitButton.Margin = new System.Windows.Forms.Padding(5);
            this.exitButton.Name = "exitButton";
            this.exitButton.Size = new System.Drawing.Size(182, 31);
            this.exitButton.TabIndex = 5;
            this.exitButton.Text = "Exit";
            this.exitButton.UseVisualStyleBackColor = true;
            this.exitButton.Click += new System.EventHandler(this.ExitButtonClickHandler);
            // 
            // sourceXhtmlWebBrowser
            // 
            this.mainTableLayoutPanel.SetColumnSpan(this.sourceXhtmlWebBrowser, 4);
            this.sourceXhtmlWebBrowser.Dock = System.Windows.Forms.DockStyle.Fill;
            this.sourceXhtmlWebBrowser.Location = new System.Drawing.Point(3, 3);
            this.sourceXhtmlWebBrowser.MinimumSize = new System.Drawing.Size(20, 20);
            this.sourceXhtmlWebBrowser.Name = "sourceXhtmlWebBrowser";
            this.mainTableLayoutPanel.SetRowSpan(this.sourceXhtmlWebBrowser, 2);
            this.sourceXhtmlWebBrowser.Size = new System.Drawing.Size(742, 740);
            this.sourceXhtmlWebBrowser.TabIndex = 0;
            // 
            // openSourceButton
            // 
            this.openSourceButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.openSourceButton.Location = new System.Drawing.Point(5, 831);
            this.openSourceButton.Margin = new System.Windows.Forms.Padding(5);
            this.openSourceButton.Name = "openSourceButton";
            this.openSourceButton.Size = new System.Drawing.Size(177, 31);
            this.openSourceButton.TabIndex = 2;
            this.openSourceButton.Text = "Open Source";
            this.openSourceButton.UseVisualStyleBackColor = true;
            this.openSourceButton.Click += new System.EventHandler(this.OpenSourceButtonClickHandler);
            // 
            // synthesizersDataGridView
            // 
            this.synthesizersDataGridView.AllowUserToAddRows = false;
            this.synthesizersDataGridView.AllowUserToDeleteRows = false;
            this.synthesizersDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.synthesizersDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.languageColumn,
            this.voiceColumn});
            this.mainTableLayoutPanel.SetColumnSpan(this.synthesizersDataGridView, 2);
            this.synthesizersDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.synthesizersDataGridView.Location = new System.Drawing.Point(751, 3);
            this.synthesizersDataGridView.Name = "synthesizersDataGridView";
            this.synthesizersDataGridView.RowTemplate.Height = 24;
            this.synthesizersDataGridView.Size = new System.Drawing.Size(373, 367);
            this.synthesizersDataGridView.TabIndex = 3;
            this.synthesizersDataGridView.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.SynthesizersDataGridViewCellValueChangedHandler);
            // 
            // languageColumn
            // 
            this.languageColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.languageColumn.HeaderText = "Language";
            this.languageColumn.Name = "languageColumn";
            this.languageColumn.ReadOnly = true;
            this.languageColumn.Width = 101;
            // 
            // voiceColumn
            // 
            this.voiceColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.voiceColumn.HeaderText = "Voice";
            this.voiceColumn.Name = "voiceColumn";
            this.voiceColumn.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.voiceColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            // 
            // synthesizeDtbButton
            // 
            this.synthesizeDtbButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.synthesizeDtbButton.Location = new System.Drawing.Point(192, 831);
            this.synthesizeDtbButton.Margin = new System.Windows.Forms.Padding(5);
            this.synthesizeDtbButton.Name = "synthesizeDtbButton";
            this.synthesizeDtbButton.Size = new System.Drawing.Size(177, 31);
            this.synthesizeDtbButton.TabIndex = 4;
            this.synthesizeDtbButton.Text = "Synthesize DTB";
            this.synthesizeDtbButton.UseVisualStyleBackColor = true;
            this.synthesizeDtbButton.Click += new System.EventHandler(this.SynthesizeDtbButtonClickHandler);
            // 
            // synthesizeProgressMessageLabel
            // 
            this.synthesizeProgressMessageLabel.AutoSize = true;
            this.mainTableLayoutPanel.SetColumnSpan(this.synthesizeProgressMessageLabel, 6);
            this.synthesizeProgressMessageLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.synthesizeProgressMessageLabel.Location = new System.Drawing.Point(3, 746);
            this.synthesizeProgressMessageLabel.Name = "synthesizeProgressMessageLabel";
            this.synthesizeProgressMessageLabel.Size = new System.Drawing.Size(1121, 40);
            this.synthesizeProgressMessageLabel.TabIndex = 6;
            this.synthesizeProgressMessageLabel.Text = "-";
            this.synthesizeProgressMessageLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.synthesizeProgressMessageLabel.Visible = false;
            // 
            // synthesizeProgressBar
            // 
            this.mainTableLayoutPanel.SetColumnSpan(this.synthesizeProgressBar, 6);
            this.synthesizeProgressBar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.synthesizeProgressBar.Location = new System.Drawing.Point(5, 791);
            this.synthesizeProgressBar.Margin = new System.Windows.Forms.Padding(5);
            this.synthesizeProgressBar.Name = "synthesizeProgressBar";
            this.synthesizeProgressBar.Size = new System.Drawing.Size(1117, 30);
            this.synthesizeProgressBar.TabIndex = 7;
            this.synthesizeProgressBar.Visible = false;
            // 
            // cancelSynthesisButton
            // 
            this.cancelSynthesisButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cancelSynthesisButton.Location = new System.Drawing.Point(379, 831);
            this.cancelSynthesisButton.Margin = new System.Windows.Forms.Padding(5);
            this.cancelSynthesisButton.Name = "cancelSynthesisButton";
            this.cancelSynthesisButton.Size = new System.Drawing.Size(177, 31);
            this.cancelSynthesisButton.TabIndex = 8;
            this.cancelSynthesisButton.Text = "Cancel Synthesis";
            this.cancelSynthesisButton.UseVisualStyleBackColor = true;
            this.cancelSynthesisButton.Visible = false;
            this.cancelSynthesisButton.Click += new System.EventHandler(this.CancelSynthesisButtonClickHandler);
            // 
            // settingsTableLayoutPanel
            // 
            this.settingsTableLayoutPanel.ColumnCount = 2;
            this.mainTableLayoutPanel.SetColumnSpan(this.settingsTableLayoutPanel, 2);
            this.settingsTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.settingsTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.settingsTableLayoutPanel.Controls.Add(this.dcPublisherTextBox, 5, 3);
            this.settingsTableLayoutPanel.Controls.Add(this.dcCreatorTextBox, 5, 2);
            this.settingsTableLayoutPanel.Controls.Add(this.dcPublisherLabel, 0, 3);
            this.settingsTableLayoutPanel.Controls.Add(this.dcCreatorLabel, 0, 2);
            this.settingsTableLayoutPanel.Controls.Add(this.dcIdentifierLabel, 0, 1);
            this.settingsTableLayoutPanel.Controls.Add(this.audioFormatLabel, 0, 0);
            this.settingsTableLayoutPanel.Controls.Add(this.audioFormatComboBox, 1, 0);
            this.settingsTableLayoutPanel.Controls.Add(this.dcIdentifierTextBox, 1, 1);
            this.settingsTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.settingsTableLayoutPanel.Location = new System.Drawing.Point(751, 376);
            this.settingsTableLayoutPanel.Name = "settingsTableLayoutPanel";
            this.settingsTableLayoutPanel.RowCount = 5;
            this.settingsTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.settingsTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.settingsTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.settingsTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.settingsTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.settingsTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.settingsTableLayoutPanel.Size = new System.Drawing.Size(373, 367);
            this.settingsTableLayoutPanel.TabIndex = 9;
            // 
            // dcPublisherTextBox
            // 
            this.dcPublisherTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dcPublisherTextBox.Location = new System.Drawing.Point(103, 128);
            this.dcPublisherTextBox.Margin = new System.Windows.Forms.Padding(5, 8, 5, 5);
            this.dcPublisherTextBox.Name = "dcPublisherTextBox";
            this.dcPublisherTextBox.Size = new System.Drawing.Size(265, 22);
            this.dcPublisherTextBox.TabIndex = 10;
            this.dcPublisherTextBox.Tag = "meta:dc:publisher";
            this.dcPublisherTextBox.TextChanged += new System.EventHandler(this.MetaDataTextBoxTextChangedHandler);
            // 
            // dcCreatorTextBox
            // 
            this.dcCreatorTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dcCreatorTextBox.Location = new System.Drawing.Point(103, 88);
            this.dcCreatorTextBox.Margin = new System.Windows.Forms.Padding(5, 8, 5, 5);
            this.dcCreatorTextBox.Name = "dcCreatorTextBox";
            this.dcCreatorTextBox.Size = new System.Drawing.Size(265, 22);
            this.dcCreatorTextBox.TabIndex = 9;
            this.dcCreatorTextBox.Tag = "meta:dc:creator";
            this.dcCreatorTextBox.TextChanged += new System.EventHandler(this.MetaDataTextBoxTextChangedHandler);
            // 
            // dcPublisherLabel
            // 
            this.dcPublisherLabel.AutoSize = true;
            this.dcPublisherLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dcPublisherLabel.Location = new System.Drawing.Point(3, 120);
            this.dcPublisherLabel.Name = "dcPublisherLabel";
            this.dcPublisherLabel.Size = new System.Drawing.Size(92, 40);
            this.dcPublisherLabel.TabIndex = 8;
            this.dcPublisherLabel.Text = "dc:publisher";
            this.dcPublisherLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // dcCreatorLabel
            // 
            this.dcCreatorLabel.AutoSize = true;
            this.dcCreatorLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dcCreatorLabel.Location = new System.Drawing.Point(3, 80);
            this.dcCreatorLabel.Name = "dcCreatorLabel";
            this.dcCreatorLabel.Size = new System.Drawing.Size(92, 40);
            this.dcCreatorLabel.TabIndex = 4;
            this.dcCreatorLabel.Text = "dc:creator";
            this.dcCreatorLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // dcIdentifierLabel
            // 
            this.dcIdentifierLabel.AutoSize = true;
            this.dcIdentifierLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dcIdentifierLabel.Location = new System.Drawing.Point(3, 40);
            this.dcIdentifierLabel.Name = "dcIdentifierLabel";
            this.dcIdentifierLabel.Size = new System.Drawing.Size(92, 40);
            this.dcIdentifierLabel.TabIndex = 2;
            this.dcIdentifierLabel.Text = "dc:identifier";
            this.dcIdentifierLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // audioFormatLabel
            // 
            this.audioFormatLabel.AutoSize = true;
            this.audioFormatLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.audioFormatLabel.Location = new System.Drawing.Point(3, 0);
            this.audioFormatLabel.Name = "audioFormatLabel";
            this.audioFormatLabel.Size = new System.Drawing.Size(92, 40);
            this.audioFormatLabel.TabIndex = 0;
            this.audioFormatLabel.Text = "Audio Format";
            this.audioFormatLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // audioFormatComboBox
            // 
            this.audioFormatComboBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.audioFormatComboBox.FormattingEnabled = true;
            this.audioFormatComboBox.Location = new System.Drawing.Point(103, 8);
            this.audioFormatComboBox.Margin = new System.Windows.Forms.Padding(5, 8, 5, 5);
            this.audioFormatComboBox.Name = "audioFormatComboBox";
            this.audioFormatComboBox.Size = new System.Drawing.Size(265, 24);
            this.audioFormatComboBox.TabIndex = 1;
            this.audioFormatComboBox.SelectedValueChanged += new System.EventHandler(this.AudioFormatComboBoxSelectedValueChangedHandler);
            // 
            // dcIdentifierTextBox
            // 
            this.dcIdentifierTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dcIdentifierTextBox.Location = new System.Drawing.Point(103, 48);
            this.dcIdentifierTextBox.Margin = new System.Windows.Forms.Padding(5, 8, 5, 5);
            this.dcIdentifierTextBox.Name = "dcIdentifierTextBox";
            this.dcIdentifierTextBox.Size = new System.Drawing.Size(265, 22);
            this.dcIdentifierTextBox.TabIndex = 3;
            this.dcIdentifierTextBox.Tag = "meta:dc:identifier";
            this.dcIdentifierTextBox.TextChanged += new System.EventHandler(this.MetaDataTextBoxTextChangedHandler);
            // 
            // resetSynthesizersButton
            // 
            this.resetSynthesizersButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.resetSynthesizersButton.Location = new System.Drawing.Point(753, 831);
            this.resetSynthesizersButton.Margin = new System.Windows.Forms.Padding(5);
            this.resetSynthesizersButton.Name = "resetSynthesizersButton";
            this.resetSynthesizersButton.Size = new System.Drawing.Size(177, 31);
            this.resetSynthesizersButton.TabIndex = 10;
            this.resetSynthesizersButton.Text = "Reset Voices";
            this.resetSynthesizersButton.UseVisualStyleBackColor = true;
            this.resetSynthesizersButton.Click += new System.EventHandler(this.ResetSynthesizersButtonClickHandler);
            // 
            // synthesizeBackgroundWorker
            // 
            this.synthesizeBackgroundWorker.WorkerReportsProgress = true;
            this.synthesizeBackgroundWorker.WorkerSupportsCancellation = true;
            this.synthesizeBackgroundWorker.DoWork += new System.ComponentModel.DoWorkEventHandler(this.SynthesizeBackgroundWorkerDoWorkHandler);
            this.synthesizeBackgroundWorker.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.SynthesizeBackgroundWorkerProgressChangedHandler);
            this.synthesizeBackgroundWorker.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.SynthesizeBackgroundWorkerRunWorkerCompletedHandler);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1127, 867);
            this.Controls.Add(this.mainTableLayoutPanel);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "MainForm";
            this.Text = "Dtb Synthesizer";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainFormFormClosingHandler);
            this.mainTableLayoutPanel.ResumeLayout(false);
            this.mainTableLayoutPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.synthesizersDataGridView)).EndInit();
            this.settingsTableLayoutPanel.ResumeLayout(false);
            this.settingsTableLayoutPanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel mainTableLayoutPanel;
        private System.Windows.Forms.WebBrowser sourceXhtmlWebBrowser;
        private System.Windows.Forms.Button openSourceButton;
        private System.Windows.Forms.DataGridView synthesizersDataGridView;
        private System.Windows.Forms.Button synthesizeDtbButton;
        private System.Windows.Forms.Button exitButton;
        private System.Windows.Forms.Label synthesizeProgressMessageLabel;
        private System.Windows.Forms.ProgressBar synthesizeProgressBar;
        private System.Windows.Forms.Button cancelSynthesisButton;
        private System.ComponentModel.BackgroundWorker synthesizeBackgroundWorker;
        private System.Windows.Forms.DataGridViewTextBoxColumn languageColumn;
        private System.Windows.Forms.DataGridViewComboBoxColumn voiceColumn;
        private System.Windows.Forms.TableLayoutPanel settingsTableLayoutPanel;
        private System.Windows.Forms.Label audioFormatLabel;
        private System.Windows.Forms.ComboBox audioFormatComboBox;
        private System.Windows.Forms.Button resetSynthesizersButton;
        private System.Windows.Forms.TextBox dcPublisherTextBox;
        private System.Windows.Forms.TextBox dcCreatorTextBox;
        private System.Windows.Forms.Label dcPublisherLabel;
        private System.Windows.Forms.Label dcCreatorLabel;
        private System.Windows.Forms.Label dcIdentifierLabel;
        private System.Windows.Forms.TextBox dcIdentifierTextBox;
    }
}

