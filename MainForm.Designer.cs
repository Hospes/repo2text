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
            treeViewFiles.Location = new Point(25, 156);
            treeViewFiles.Name = "treeViewFiles";
            treeViewFiles.Size = new Size(741, 459);
            treeViewFiles.TabIndex = 7;
            // 
            // btnGenerateText
            // 
            btnGenerateText.Enabled = false;
            btnGenerateText.Location = new Point(1090, 20);
            btnGenerateText.Name = "btnGenerateText";
            btnGenerateText.Size = new Size(170, 23);
            btnGenerateText.TabIndex = 8;
            btnGenerateText.Text = "Generate Text";
            btnGenerateText.UseVisualStyleBackColor = true;
            // 
            // btnCopy
            // 
            btnCopy.Enabled = false;
            btnCopy.Location = new Point(1034, 592);
            btnCopy.Name = "btnCopy";
            btnCopy.Size = new Size(170, 23);
            btnCopy.TabIndex = 9;
            btnCopy.Text = "Copy to Clipboard";
            btnCopy.UseVisualStyleBackColor = true;
            // 
            // btnDownloadText
            // 
            btnDownloadText.Enabled = false;
            btnDownloadText.Location = new Point(1210, 592);
            btnDownloadText.Name = "btnDownloadText";
            btnDownloadText.Size = new Size(170, 23);
            btnDownloadText.TabIndex = 10;
            btnDownloadText.Text = "Download Text";
            btnDownloadText.UseVisualStyleBackColor = true;
            // 
            // btnDownloadZip
            // 
            btnDownloadZip.Enabled = false;
            btnDownloadZip.Location = new Point(1386, 592);
            btnDownloadZip.Name = "btnDownloadZip";
            btnDownloadZip.Size = new Size(170, 23);
            btnDownloadZip.TabIndex = 11;
            btnDownloadZip.Text = "Download Selected as ZIP";
            btnDownloadZip.UseVisualStyleBackColor = true;
            // 
            // txtOutput
            // 
            txtOutput.Font = new Font("Fira Code", 11.9999981F, FontStyle.Regular, GraphicsUnit.Point, 0);
            txtOutput.Location = new Point(772, 49);
            txtOutput.Multiline = true;
            txtOutput.Name = "txtOutput";
            txtOutput.ReadOnly = true;
            txtOutput.ScrollBars = ScrollBars.Both;
            txtOutput.Size = new Size(784, 519);
            txtOutput.TabIndex = 12;
            txtOutput.WordWrap = false;
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Location = new Point(772, 571);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(38, 15);
            lblStatus.TabIndex = 13;
            lblStatus.Text = "label1";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1568, 627);
            Controls.Add(lblStatus);
            Controls.Add(txtOutput);
            Controls.Add(btnDownloadZip);
            Controls.Add(btnDownloadText);
            Controls.Add(btnCopy);
            Controls.Add(btnGenerateText);
            Controls.Add(treeViewFiles);
            Controls.Add(btnSelectZip);
            Controls.Add(btnSelectDirectory);
            Controls.Add(btnFetchGitHub);
            Controls.Add(txtAccessToken);
            Controls.Add(lblAccessToken);
            Controls.Add(txtRepoUrl);
            Controls.Add(lblRepoUrl);
            Name = "MainForm";
            Text = "Form1";
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
    }
}
