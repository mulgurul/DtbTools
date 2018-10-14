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
            this.synthesizerTypeColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.synthesizerNameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.synthesizeDtbButton = new System.Windows.Forms.Button();
            this.synthesizeProgressMessageLabel = new System.Windows.Forms.Label();
            this.synthesizeProgressBar = new System.Windows.Forms.ProgressBar();
            this.cancelSynthesisButton = new System.Windows.Forms.Button();
            this.synthesizeBackgroundWorker = new System.ComponentModel.BackgroundWorker();
            this.mainTableLayoutPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.synthesizersDataGridView)).BeginInit();
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
            this.synthesizerTypeColumn,
            this.synthesizerNameColumn});
            this.mainTableLayoutPanel.SetColumnSpan(this.synthesizersDataGridView, 2);
            this.synthesizersDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.synthesizersDataGridView.Location = new System.Drawing.Point(751, 3);
            this.synthesizersDataGridView.Name = "synthesizersDataGridView";
            this.synthesizersDataGridView.ReadOnly = true;
            this.synthesizersDataGridView.RowTemplate.Height = 24;
            this.synthesizersDataGridView.Size = new System.Drawing.Size(373, 367);
            this.synthesizersDataGridView.TabIndex = 3;
            // 
            // languageColumn
            // 
            this.languageColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.languageColumn.HeaderText = "Language";
            this.languageColumn.Name = "languageColumn";
            this.languageColumn.ReadOnly = true;
            this.languageColumn.Width = 101;
            // 
            // synthesizerTypeColumn
            // 
            this.synthesizerTypeColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.synthesizerTypeColumn.HeaderText = "Type";
            this.synthesizerTypeColumn.Name = "synthesizerTypeColumn";
            this.synthesizerTypeColumn.ReadOnly = true;
            this.synthesizerTypeColumn.Width = 69;
            // 
            // synthesizerNameColumn
            // 
            this.synthesizerNameColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.synthesizerNameColumn.HeaderText = "Name";
            this.synthesizerNameColumn.Name = "synthesizerNameColumn";
            this.synthesizerNameColumn.ReadOnly = true;
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
            // 
            // synthesizeProgressBar
            // 
            this.mainTableLayoutPanel.SetColumnSpan(this.synthesizeProgressBar, 6);
            this.synthesizeProgressBar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.synthesizeProgressBar.Location = new System.Drawing.Point(3, 789);
            this.synthesizeProgressBar.Name = "synthesizeProgressBar";
            this.synthesizeProgressBar.Size = new System.Drawing.Size(1121, 34);
            this.synthesizeProgressBar.TabIndex = 7;
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
            this.mainTableLayoutPanel.ResumeLayout(false);
            this.mainTableLayoutPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.synthesizersDataGridView)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel mainTableLayoutPanel;
        private System.Windows.Forms.WebBrowser sourceXhtmlWebBrowser;
        private System.Windows.Forms.Button openSourceButton;
        private System.Windows.Forms.DataGridView synthesizersDataGridView;
        private System.Windows.Forms.DataGridViewTextBoxColumn languageColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn synthesizerTypeColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn synthesizerNameColumn;
        private System.Windows.Forms.Button synthesizeDtbButton;
        private System.Windows.Forms.Button exitButton;
        private System.Windows.Forms.Label synthesizeProgressMessageLabel;
        private System.Windows.Forms.ProgressBar synthesizeProgressBar;
        private System.Windows.Forms.Button cancelSynthesisButton;
        private System.ComponentModel.BackgroundWorker synthesizeBackgroundWorker;
    }
}

