namespace Repo2Text
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            lblRepoUrl = new Label();
            txtRepoUrl = new TextBox();
            lblAccessToken = new Label();
            txtAccessToken = new TextBox();
            btnFetchGitHub = new Button();
            btnSelectDirectory = new Button();
            btnSelectZip = new Button();
            treeViewFiles = new TreeView();
            btnGenerateText = new Button();
            btnCopy = new Button();
            btnDownloadText = new Button();
            btnDownloadZip = new Button();
            txtOutput = new TextBox();
            lblStatus = new Label();
            lblTokenCount = new Label();
            flowLayoutPanelExtensions = new FlowLayoutPanel();
            splitContainer1 = new SplitContainer();
            splitContainer2 = new SplitContainer();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer2).BeginInit();
            splitContainer2.Panel1.SuspendLayout();
            splitContainer2.Panel2.SuspendLayout();
            splitContainer2.SuspendLayout();
            SuspendLayout();
            // 
            // lblRepoUrl
            // 
            lblRepoUrl.AutoSize = true;
            lblRepoUrl.Location = new Point(25, 24);
            lblRepoUrl.Name = "lblRepoUrl";
            lblRepoUrl.Size = new Size(72, 15);
            lblRepoUrl.TabIndex = 0;
            lblRepoUrl.Text = "GitHub URL:";
            // 
            // txtRepoUrl
            // 
            txtRepoUrl.Location = new Point(25, 42);
            txtRepoUrl.Name = "txtRepoUrl";
            txtRepoUrl.Size = new Size(522, 23);
            txtRepoUrl.TabIndex = 1;
            // 
            // lblAccessToken
            // 
            lblAccessToken.AutoSize = true;
            lblAccessToken.Location = new Point(25, 80);
            lblAccessToken.Name = "lblAccessToken";
            lblAccessToken.Size = new Size(138, 15);
            lblAccessToken.TabIndex = 2;
            lblAccessToken.Text = "Access Token (Optional):";
            // 
            // txtAccessToken
            // 
            txtAccessToken.Location = new Point(25, 98);
            txtAccessToken.Name = "txtAccessToken";
            txtAccessToken.PasswordChar = '*';
            txtAccessToken.Size = new Size(522, 23);
            txtAccessToken.TabIndex = 3;
            // 
            // btnFetchGitHub
            // 
            btnFetchGitHub.Location = new Point(25, 127);
            btnFetchGitHub.Name = "btnFetchGitHub";
            btnFetchGitHub.Size = new Size(138, 23);
            btnFetchGitHub.TabIndex = 4;
            btnFetchGitHub.Text = "Fetch from GitHub";
            btnFetchGitHub.UseVisualStyleBackColor = true;
            // 
            // btnSelectDirectory
            // 
            btnSelectDirectory.Location = new Point(596, 41);
            btnSelectDirectory.Name = "btnSelectDirectory";
            btnSelectDirectory.Size = new Size(170, 23);
            btnSelectDirectory.TabIndex = 5;
            btnSelectDirectory.Text = "Select Local Directory";
            btnSelectDirectory.UseVisualStyleBackColor = true;
            // 
            // btnSelectZip
            // 
            btnSelectZip.Location = new Point(596, 97);
            btnSelectZip.Name = "btnSelectZip";
            btnSelectZip.Size = new Size(170, 23);
            btnSelectZip.TabIndex = 6;
            btnSelectZip.Text = "Select Local ZIP";
            btnSelectZip.UseVisualStyleBackColor = true;
            // 
            // treeViewFiles
            // 
            treeViewFiles.CheckBoxes = true;
            treeViewFiles.Dock = DockStyle.Fill;
            treeViewFiles.Location = new Point(0, 0);
            treeViewFiles.Name = "treeViewFiles";
            treeViewFiles.Size = new Size(754, 425);
            treeViewFiles.TabIndex = 7;
            // 
            // btnGenerateText
            // 
            btnGenerateText.Enabled = false;
            btnGenerateText.Location = new Point(300, 12);
            btnGenerateText.Name = "btnGenerateText";
            btnGenerateText.Size = new Size(170, 23);
            btnGenerateText.TabIndex = 8;
            btnGenerateText.Text = "Generate Text";
            btnGenerateText.UseVisualStyleBackColor = true;
            // 
            // btnCopy
            // 
            btnCopy.Enabled = false;
            btnCopy.Location = new Point(433, 599);
            btnCopy.Name = "btnCopy";
            btnCopy.Size = new Size(170, 23);
            btnCopy.TabIndex = 9;
            btnCopy.Text = "Copy to Clipboard";
            btnCopy.UseVisualStyleBackColor = true;
            // 
            // btnDownloadText
            // 
            btnDownloadText.Enabled = false;
            btnDownloadText.Location = new Point(609, 599);
            btnDownloadText.Name = "btnDownloadText";
            btnDownloadText.Size = new Size(170, 23);
            btnDownloadText.TabIndex = 10;
            btnDownloadText.Text = "Download Text";
            btnDownloadText.UseVisualStyleBackColor = true;
            // 
            // btnDownloadZip
            // 
            btnDownloadZip.Enabled = false;
            btnDownloadZip.Location = new Point(609, 570);
            btnDownloadZip.Name = "btnDownloadZip";
            btnDownloadZip.Size = new Size(170, 23);
            btnDownloadZip.TabIndex = 11;
            btnDownloadZip.Text = "Download Selected as ZIP";
            btnDownloadZip.UseVisualStyleBackColor = true;
            // 
            // txtOutput
            // 
            txtOutput.Font = new Font("Fira Code", 11.9999981F, FontStyle.Regular, GraphicsUnit.Point, 0);
            txtOutput.Location = new Point(3, 42);
            txtOutput.Multiline = true;
            txtOutput.Name = "txtOutput";
            txtOutput.ReadOnly = true;
            txtOutput.ScrollBars = ScrollBars.Both;
            txtOutput.Size = new Size(776, 522);
            txtOutput.TabIndex = 12;
            txtOutput.WordWrap = false;
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Location = new Point(772, 571);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(0, 15);
            lblStatus.TabIndex = 13;
            // 
            // lblTokenCount
            // 
            lblTokenCount.AutoSize = true;
            lblTokenCount.Location = new Point(3, 603);
            lblTokenCount.Name = "lblTokenCount";
            lblTokenCount.Size = new Size(86, 15);
            lblTokenCount.TabIndex = 14;
            lblTokenCount.Text = "Token Count: -";
            lblTokenCount.TextAlign = ContentAlignment.TopRight;
            lblTokenCount.Visible = false;
            // 
            // flowLayoutPanelExtensions
            // 
            flowLayoutPanelExtensions.AutoSize = true;
            flowLayoutPanelExtensions.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            flowLayoutPanelExtensions.BorderStyle = BorderStyle.FixedSingle;
            flowLayoutPanelExtensions.Dock = DockStyle.Fill;
            flowLayoutPanelExtensions.Location = new Point(0, 0);
            flowLayoutPanelExtensions.Name = "flowLayoutPanelExtensions";
            flowLayoutPanelExtensions.Padding = new Padding(3);
            flowLayoutPanelExtensions.Size = new Size(754, 30);
            flowLayoutPanelExtensions.TabIndex = 15;
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Location = new Point(0, 0);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(splitContainer2);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(btnGenerateText);
            splitContainer1.Panel2.Controls.Add(lblTokenCount);
            splitContainer1.Panel2.Controls.Add(btnCopy);
            splitContainer1.Panel2.Controls.Add(btnDownloadText);
            splitContainer1.Panel2.Controls.Add(btnDownloadZip);
            splitContainer1.Panel2.Controls.Add(txtOutput);
            splitContainer1.Size = new Size(1568, 627);
            splitContainer1.SplitterDistance = 773;
            splitContainer1.TabIndex = 16;
            // 
            // splitContainer2
            // 
            splitContainer2.Location = new Point(12, 156);
            splitContainer2.Name = "splitContainer2";
            splitContainer2.Orientation = Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            splitContainer2.Panel1.Controls.Add(flowLayoutPanelExtensions);
            // 
            // splitContainer2.Panel2
            // 
            splitContainer2.Panel2.Controls.Add(treeViewFiles);
            splitContainer2.Size = new Size(754, 459);
            splitContainer2.SplitterDistance = 30;
            splitContainer2.TabIndex = 8;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1568, 627);
            Controls.Add(lblStatus);
            Controls.Add(btnSelectZip);
            Controls.Add(btnSelectDirectory);
            Controls.Add(btnFetchGitHub);
            Controls.Add(txtAccessToken);
            Controls.Add(lblAccessToken);
            Controls.Add(txtRepoUrl);
            Controls.Add(lblRepoUrl);
            Controls.Add(splitContainer1);
            Name = "MainForm";
            Text = "Form1";
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            splitContainer2.Panel1.ResumeLayout(false);
            splitContainer2.Panel1.PerformLayout();
            splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer2).EndInit();
            splitContainer2.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblRepoUrl;
        private TextBox txtRepoUrl;
        private Label lblAccessToken;
        private TextBox txtAccessToken;
        private Button btnFetchGitHub;
        private Button btnSelectDirectory;
        private Button btnSelectZip;
        private TreeView treeViewFiles;
        private Button btnGenerateText;
        private Button btnCopy;
        private Button btnDownloadText;
        private Button btnDownloadZip;
        private TextBox txtOutput;
        private Label lblStatus;
        private Label lblTokenCount;
        private FlowLayoutPanel flowLayoutPanelExtensions;
        private SplitContainer splitContainer1;
        private SplitContainer splitContainer2;
    }
}
