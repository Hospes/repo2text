// MainForm.cs
using Octokit;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SharpToken;

namespace Repo2Text
{
    public partial class MainForm : Form
    {
        private GitHubRepoInfo _currentRepoInfo;
        private string _currentLocalPath;
        private string _currentZipPath;
        private DataSourceType _currentSourceType;
        private Dictionary<string, ZipArchiveEntry> _zipEntryMap;
        private static GptEncoding _tokenizer;
        private Dictionary<string, List<TreeNode>> _extensionTreeNodes = new Dictionary<string, List<TreeNode>>(StringComparer.OrdinalIgnoreCase);

        private bool _isUpdatingTreeViewProgrammatically = false;
        private bool _isUpdatingExtensionsProgrammatically = false;
        private bool _isProgrammaticallyAdjustingSplitter = false;

        public MainForm()
        {
            InitializeComponent();
            InitializeTokenizer();
            LoadSettings();
            AttachEventHandlers();
            this.flowLayoutPanelExtensions.SizeChanged += FlowLayoutPanelExtensions_SizeChanged;

            this.splitContainer3.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitContainer3.IsSplitterFixed = true;
            this.splitContainer3.Panel2MinSize = 75;
        }

        private static void InitializeTokenizer()
        {
            try
            {
                _tokenizer = GptEncoding.GetEncoding("cl100k_base");
            }
            catch (Exception ex)
            {
                _tokenizer = null;
                Console.WriteLine($"Error initializing tokenizer: {ex.Message}");
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            SetStatus("Ready. Select a source.", false);
            lblTokenCount.Visible = false;
            lblTokenCount.Text = "";
        }

        private void AttachEventHandlers()
        {
            this.treeViewFiles.AfterCheck -= treeViewFiles_AfterCheck;
            this.btnFetchGitHub.Click -= btnFetchGitHub_Click;
            this.btnSelectDirectory.Click -= btnSelectDirectory_Click;
            this.btnSelectZip.Click -= btnSelectZip_Click;
            this.btnGenerateText.Click -= btnGenerateText_Click;
            this.btnCopy.Click -= btnCopy_Click;
            this.btnDownloadText.Click -= btnDownloadText_Click;
            this.btnDownloadZip.Click -= btnDownloadZip_Click;

            this.treeViewFiles.AfterCheck += treeViewFiles_AfterCheck;
            this.btnFetchGitHub.Click += btnFetchGitHub_Click;
            this.btnSelectDirectory.Click += btnSelectDirectory_Click;
            this.btnSelectZip.Click += btnSelectZip_Click;
            this.btnGenerateText.Click += btnGenerateText_Click;
            this.btnCopy.Click += btnCopy_Click;
            this.btnDownloadText.Click += btnDownloadText_Click;
            this.btnDownloadZip.Click += btnDownloadZip_Click;
        }

        private void LoadSettings()
        {
            try
            {
                txtAccessToken.Text = Properties.Settings.Default.GitHubToken ?? "";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading settings: {ex.Message}");
                SetStatus("Error loading settings.", true);
            }
        }

        private void SaveSettings()
        {
            try
            {
                Properties.Settings.Default.GitHubToken = txtAccessToken.Text;
                Properties.Settings.Default.Save();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
                SetStatus("Error saving settings.", true);
            }
        }

        private async void btnFetchGitHub_Click(object sender, EventArgs e)
        {
            ClearUIState();
            string repoUrl = txtRepoUrl.Text.Trim();
            string token = txtAccessToken.Text.Trim();

            if (string.IsNullOrEmpty(repoUrl))
            {
                SetStatus("Please enter a GitHub URL.", true);
                return;
            }

            SetStatus("Parsing URL...");
            _currentRepoInfo = GitHubService.ParseRepoUrl(repoUrl);

            if (!_currentRepoInfo.IsValid)
            {
                SetStatus("Invalid GitHub URL format.", true);
                _currentRepoInfo = null;
                return;
            }

            SaveSettings();
            SetStatus($"Fetching tree for {_currentRepoInfo.Owner}/{_currentRepoInfo.RepoName}...");
            _currentSourceType = DataSourceType.GitHub;
            this.Cursor = Cursors.WaitCursor;

            try
            {
                var files = await GitHubService.GetRepositoryTreeAsync(_currentRepoInfo, token);

                if (!_currentRepoInfo.IsValid)
                {
                    SetStatus("Failed to resolve repository details. Check URL and token.", true);
                    return;
                }

                PopulateTreeView(files);
                string displayPath = string.IsNullOrEmpty(_currentRepoInfo.Path) ? "/" : $"/{_currentRepoInfo.Path}";
                SetStatus($"Fetched {files.Count} items from '{_currentRepoInfo.ResolvedRef}' in path '{displayPath}'. Select files and generate.", false);
                btnGenerateText.Enabled = files.Any();
                btnDownloadZip.Enabled = true;
            }
            catch (Exception ex)
            {
                HandleFetchError(ex);
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private void HandleFetchError(Exception ex)
        {
            if (ex is Octokit.RateLimitExceededException rateLimitEx)
            {
                long resetUnix = rateLimitEx.Reset.ToUnixTimeSeconds();
                DateTime resetTime = DateTimeOffset.FromUnixTimeSeconds(resetUnix).LocalDateTime;
                SetStatus($"GitHub API rate limit exceeded. Resets at {resetTime:T}. Try again later or use a token.", true);
                txtOutput.Text = $"GitHub API rate limit exceeded.\nLimit resets at: {resetTime}\nProvide a Personal Access Token for higher limits.";
            }
            else if (ex is Octokit.NotFoundException)
            {
                SetStatus("Repository, branch, or path not found.", true);
                txtOutput.Text = "Repository, branch, or path not found. Please check the URL and token permissions.";
            }
            else if (ex is Octokit.ApiException apiEx)
            {
                SetStatus($"GitHub API Error ({apiEx.StatusCode}). Check URL/token.", true);
                txtOutput.Text = $"GitHub API Error ({apiEx.StatusCode}): {apiEx.Message}\n\nCheck URL, token permissions.";
            }
            else if (ex is HttpRequestException httpEx)
            {
                SetStatus($"Network error: {httpEx.Message}", true);
                txtOutput.Text = $"A network error occurred: {httpEx.Message}\nPlease check your internet connection.";
            }
            else if (ex is InvalidOperationException invOpEx && invOpEx.Message.Contains("default branch"))
            {
                SetStatus($"Error determining default branch: {invOpEx.InnerException?.Message}", true);
                txtOutput.Text = $"Could not determine the default branch for the repository. Please check the repository URL and permissions.\nError: {invOpEx.InnerException?.Message}";
            }
            else
            {
                SetStatus($"Error fetching repository: {ex.Message}", true);
                txtOutput.Text = $"An unexpected error occurred: {ex.Message}\n\nDetails: {ex.ToString()}";
            }
        }

        private async void btnSelectDirectory_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select the root directory of the project";
                dialog.UseDescriptionForTitle = true;
                dialog.ShowNewFolderButton = false;
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    ClearUIState();
                    _currentLocalPath = dialog.SelectedPath;
                    SetStatus($"Loading files from {_currentLocalPath}...");
                    _currentSourceType = DataSourceType.LocalDirectory;
                    this.Cursor = Cursors.WaitCursor;

                    try
                    {
                        var files = await LocalFileService.GetDirectoryFilesAsync(_currentLocalPath);
                        PopulateTreeView(files);
                        SetStatus($"Loaded {files.Count} files. Select files and generate.", false);
                        btnGenerateText.Enabled = files.Any();
                        btnDownloadZip.Enabled = false;
                    }
                    catch (Exception ex)
                    {
                        SetStatus($"Error reading directory: {ex.Message}", true);
                        txtOutput.Text = $"Error reading directory: {ex.Message}\nPlease ensure permissions.";
                    }
                    finally
                    {
                        this.Cursor = Cursors.Default;
                    }
                }
            }
        }

        private async void btnSelectZip_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "ZIP Archives (*.zip)|*.zip|All Files (*.*)|*.*";
                dialog.Title = "Select a ZIP Archive";
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    ClearUIState();
                    _currentZipPath = dialog.FileName;
                    SetStatus($"Loading files from {_currentZipPath}...");
                    _currentSourceType = DataSourceType.LocalZip;
                    this.Cursor = Cursors.WaitCursor;

                    try
                    {
                        _zipEntryMap?.Clear();
                        var (files, entryMap) = await LocalFileService.GetZipFilesAsync(_currentZipPath);
                        _zipEntryMap = entryMap;

                        PopulateTreeView(files);
                        SetStatus($"Loaded {files.Count} files from ZIP. Select files and generate.", false);
                        btnGenerateText.Enabled = files.Any();
                        btnDownloadZip.Enabled = false;
                    }
                    catch (IOException ioEx)
                    {
                        SetStatus($"Error reading ZIP: {ioEx.Message}", true);
                        txtOutput.Text = $"Error reading ZIP file: {ioEx.Message}\nEnsure valid ZIP and not corrupted.";
                    }
                    catch (InvalidDataException invEx)
                    {
                        SetStatus($"Invalid ZIP data: {invEx.Message}", true);
                        txtOutput.Text = $"Invalid ZIP file format: {invEx.Message}\nFile might be corrupted.";
                    }
                    catch (Exception ex)
                    {
                        SetStatus($"Error reading ZIP file: {ex.Message}", true);
                        txtOutput.Text = $"An unexpected error occurred reading ZIP: {ex.Message}";
                    }
                    finally
                    {
                        this.Cursor = Cursors.Default;
                    }
                }
            }
        }

        private async void btnGenerateText_Click(object sender, EventArgs e)
        {
            SetStatus("Generating text...");
            txtOutput.Text = "";
            lblTokenCount.Text = "";
            lblTokenCount.Visible = false;
            btnCopy.Enabled = false;
            btnDownloadText.Enabled = false;
            System.Windows.Forms.Application.DoEvents();

            var selectedFiles = GetSelectedFiles(treeViewFiles.Nodes);
            if (!selectedFiles.Any())
            {
                SetStatus("No files selected.", true);
                return;
            }

            btnGenerateText.Enabled = false;
            this.Cursor = Cursors.WaitCursor;

            try
            {
                string generatedText = await GenerateFormattedText(selectedFiles);
                txtOutput.Text = generatedText;

                if (_tokenizer != null && !string.IsNullOrEmpty(generatedText))
                {
                    try
                    {
                        var tokens = _tokenizer.Encode(generatedText);
                        UpdateTokenCountLabel(tokens.Count);
                    }
                    catch (Exception tokenEx)
                    {
                        Console.WriteLine($"Error counting tokens: {tokenEx.Message}");
                        UpdateTokenCountLabel(-1);
                    }
                }
                else if (_tokenizer == null)
                {
                    UpdateTokenCountLabel(-2);
                }

                SetStatus($"Generated text from {selectedFiles.Count} files.", false);
                btnCopy.Enabled = true;
                btnDownloadText.Enabled = true;
            }
            catch (Exception ex)
            {
                SetStatus($"Error generating text: {ex.Message}", true);
                txtOutput.Text = $"Error during text generation: {ex.Message}\n\nDetails: {ex.ToString()}";
            }
            finally
            {
                btnGenerateText.Enabled = true;
                this.Cursor = Cursors.Default;
            }
        }

        private async void btnDownloadZip_Click(object sender, EventArgs e)
        {
            if (_currentSourceType != DataSourceType.GitHub || _currentRepoInfo == null || !_currentRepoInfo.IsValid || string.IsNullOrEmpty(_currentRepoInfo.ResolvedRef))
            {
                SetStatus("ZIP download only available after fetching from GitHub.", true);
                return;
            }

            SetStatus("Preparing ZIP download...");
            var selectedFiles = GetSelectedFiles(treeViewFiles.Nodes)
                .Where(f => !f.IsDirectory).ToList();

            if (!selectedFiles.Any())
            {
                SetStatus("No files selected for download.", true);
                return;
            }

            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "ZIP Archive (*.zip)|*.zip";
                sfd.FileName = $"{_currentRepoInfo.RepoName}_partial.zip";
                sfd.Title = "Save Selected Files as ZIP";

                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    string savePath = sfd.FileName;
                    string token = txtAccessToken.Text.Trim();
                    SetStatus($"Downloading {selectedFiles.Count} files to ZIP...");
                    this.Cursor = Cursors.WaitCursor;

                    try
                    {
                        using (var fileStream = new FileStream(savePath, System.IO.FileMode.Create))
                        using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Create))
                        {
                            foreach (var fileItem in selectedFiles)
                            {
                                string fileRef = _currentRepoInfo.ResolvedRef;
                                string content = await GitHubService.GetFileContentAsync(
                                    _currentRepoInfo.Owner, _currentRepoInfo.RepoName,
                                    fileItem.Path,
                                    fileRef, token);

                                string entryPath = fileItem.DisplayPath.TrimStart('/');

                                if (content != null && !content.StartsWith("Error:"))
                                {
                                    var entry = archive.CreateEntry(entryPath, CompressionLevel.Optimal);
                                    using (var entryStream = entry.Open())
                                    using (var streamWriter = new StreamWriter(entryStream, Encoding.UTF8))
                                    {
                                        await streamWriter.WriteAsync(content);
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"Skipping file in ZIP due to error: {fileItem.Path}");
                                    var errorEntry = archive.CreateEntry(entryPath + ".error.txt", CompressionLevel.Optimal);
                                    using (var entryStream = errorEntry.Open())
                                    using (var streamWriter = new StreamWriter(entryStream, Encoding.UTF8))
                                    {
                                        await streamWriter.WriteAsync($"Could not fetch content for: {fileItem.Path}\nError: {content ?? "Unknown error"}");
                                    }
                                }
                            }
                        }
                        SetStatus($"Successfully created ZIP file at {savePath}", false);
                    }
                    catch (Exception ex)
                    {
                        SetStatus($"Error creating ZIP file: {ex.Message}", true);
                        MessageBox.Show($"Failed to create ZIP file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        this.Cursor = Cursors.Default;
                    }
                }
                else
                {
                    SetStatus("ZIP download cancelled.", false);
                }
            }
        }

        private void btnCopy_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(txtOutput.Text))
            {
                try
                {
                    Clipboard.SetText(txtOutput.Text, TextDataFormat.UnicodeText);
                    SetStatus("Text copied to clipboard.", false);
                }
                catch (System.Runtime.InteropServices.ExternalException ex)
                {
                    SetStatus($"Error copying text: Clipboard unavailable. {ex.Message}", true);
                    MessageBox.Show($"Could not copy text to clipboard. It might be in use by another application.\n\nError: {ex.Message}", "Copy Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                catch (Exception ex)
                {
                    SetStatus($"Error copying text: {ex.Message}", true);
                    MessageBox.Show($"Could not copy text to clipboard: {ex.Message}", "Copy Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void btnDownloadText_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtOutput.Text))
            {
                SetStatus("Nothing to download.", true);
                return;
            }

            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
                sfd.FileName = "repo_content.txt";
                sfd.Title = "Save Generated Text";
                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        File.WriteAllText(sfd.FileName, txtOutput.Text, Encoding.UTF8);
                        SetStatus($"Text saved to {sfd.FileName}", false);
                    }
                    catch (Exception ex)
                    {
                        SetStatus($"Error saving file: {ex.Message}", true);
                        MessageBox.Show($"Failed to save file: {ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    SetStatus("Save cancelled.", false);
                }
            }
        }

        private void treeViewFiles_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (_isUpdatingTreeViewProgrammatically)
            {
                return;
            }

            if (e.Action == TreeViewAction.ByMouse || e.Action == TreeViewAction.ByKeyboard)
            {
                if (e.Node == null) return;

                _isUpdatingTreeViewProgrammatically = true;
                treeViewFiles.BeginUpdate();

                try
                {
                    TreeNode clickedNode = e.Node;
                    bool newCheckedStateFromUser = clickedNode.Checked;

                    SetChildNodeState(clickedNode, newCheckedStateFromUser);

                    TreeNode parent = clickedNode.Parent;
                    while (parent != null)
                    {
                        bool oldParentState = parent.Checked;
                        UpdateParentCheckStateOnly(parent);

                        if (parent.Checked == oldParentState)
                        {
                            break;
                        }
                        parent = parent.Parent;
                    }

                    if (!_isUpdatingExtensionsProgrammatically)
                    {
                        UpdateExtensionCheckStates();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in treeViewFiles_AfterCheck: {ex.Message}");
                    SetStatus($"Error updating tree: {ex.Message}", true);
                }
                finally
                {
                    treeViewFiles.EndUpdate();
                    _isUpdatingTreeViewProgrammatically = false;
                }
            }
        }

        private void SetChildNodeState(TreeNode node, bool isChecked)
        {
            foreach (TreeNode childNode in node.Nodes)
            {
                if (childNode.Checked != isChecked)
                {
                    childNode.Checked = isChecked;
                }
                SetChildNodeState(childNode, isChecked);
            }
        }

        private void UpdateParentCheckStateOnly(TreeNode parentNode)
        {
            if (parentNode == null || parentNode.Nodes.Count == 0) return;

            int checkedCount = 0;

            foreach (TreeNode childNode in parentNode.Nodes)
            {
                if (childNode.Checked)
                {
                    checkedCount++;
                }
            }

            bool newCheckedState;
            if (checkedCount == parentNode.Nodes.Count)
            {
                newCheckedState = true;
            }
            else if (checkedCount == 0)
            {
                newCheckedState = false;
            }
            else
            {
                newCheckedState = false;
            }

            if (parentNode.Checked != newCheckedState)
            {
                parentNode.Checked = newCheckedState;
            }
        }

        private void CreateExtensionFiltersUI()
        {
            flowLayoutPanelExtensions.Controls.Clear();
            flowLayoutPanelExtensions.SuspendLayout();

            if (!_extensionTreeNodes.Any())
            {
                flowLayoutPanelExtensions.Visible = false;
                flowLayoutPanelExtensions.ResumeLayout();
                return;
            }

            var sortedExtensions = _extensionTreeNodes.Keys.OrderBy(ext => ext).ToList();

            foreach (string ext in sortedExtensions)
            {
                var chk = new CheckBox
                {
                    Text = "." + ext,
                    Tag = ext,
                    AutoSize = true,
                    ThreeState = true,
                    Margin = new Padding(3, 3, 10, 3)
                };
                chk.CheckStateChanged += ExtensionCheckBox_CheckStateChanged;
                flowLayoutPanelExtensions.Controls.Add(chk);
            }

            flowLayoutPanelExtensions.Visible = true;
            flowLayoutPanelExtensions.ResumeLayout();
            UpdateExtensionCheckStates();
        }

        private void ExtensionCheckBox_CheckStateChanged(object sender, EventArgs e)
        {
            if (_isUpdatingExtensionsProgrammatically || _isUpdatingTreeViewProgrammatically) return;

            var chk = sender as CheckBox;
            if (chk == null || chk.Tag == null) return;

            string extension = chk.Tag.ToString();
            CheckState userClickedState = chk.CheckState;
            bool targetCheckedState = (userClickedState == CheckState.Checked);

            if (_extensionTreeNodes.TryGetValue(extension, out var nodesToUpdate))
            {
                _isUpdatingTreeViewProgrammatically = true;
                _isUpdatingExtensionsProgrammatically = true;

                treeViewFiles.BeginUpdate();
                try
                {
                    foreach (var node in nodesToUpdate)
                    {
                        if (node.Checked != targetCheckedState)
                        {
                            node.Checked = targetCheckedState;
                        }
                    }
                    foreach (TreeNode rootNode in treeViewFiles.Nodes)
                    {
                        UpdateAllParentStatesBottomUp(rootNode);
                    }
                }
                finally
                {
                    treeViewFiles.EndUpdate();
                    _isUpdatingTreeViewProgrammatically = false;
                    _isUpdatingExtensionsProgrammatically = false;
                }
                UpdateExtensionCheckStates();
            }
        }

        private void UpdateAllParentStatesBottomUp(TreeNode node)
        {
            foreach (TreeNode child in node.Nodes)
            {
                UpdateAllParentStatesBottomUp(child);
            }
            UpdateParentCheckStateOnly(node);
        }

        private void UpdateExtensionCheckStates()
        {
            if (_isUpdatingExtensionsProgrammatically) return;
            _isUpdatingExtensionsProgrammatically = true;
            flowLayoutPanelExtensions.SuspendLayout();

            try
            {
                foreach (Control c in flowLayoutPanelExtensions.Controls)
                {
                    if (c is CheckBox chk && chk.Tag is string extension)
                    {
                        if (_extensionTreeNodes.TryGetValue(extension, out var associatedNodes))
                        {
                            int checkedCount = 0;
                            int uncheckedCount = 0;
                            int totalCount = associatedNodes.Count;

                            if (totalCount > 0)
                            {
                                foreach (var node in associatedNodes)
                                {
                                    if (node.Checked) checkedCount++;
                                    else uncheckedCount++;
                                }

                                CheckState newState;
                                if (checkedCount == totalCount) newState = CheckState.Checked;
                                else if (uncheckedCount == totalCount) newState = CheckState.Unchecked;
                                else newState = CheckState.Indeterminate;

                                if (chk.CheckState != newState) chk.CheckState = newState;
                                if (!chk.Enabled) chk.Enabled = true;
                            }
                            else
                            {
                                if (chk.CheckState != CheckState.Unchecked) chk.CheckState = CheckState.Unchecked;
                                if (chk.Enabled) chk.Enabled = false;
                            }
                        }
                    }
                }
            }
            finally
            {
                flowLayoutPanelExtensions.ResumeLayout();
                _isUpdatingExtensionsProgrammatically = false;
            }
        }

        private void ClearUIState()
        {
            treeViewFiles.Nodes.Clear();
            txtOutput.Text = "";
            SetStatus("Ready.", false);
            lblTokenCount.Text = "";
            lblTokenCount.Visible = false;
            btnGenerateText.Enabled = false;
            btnCopy.Enabled = false;
            btnDownloadText.Enabled = false;
            btnDownloadZip.Enabled = false;
            _currentRepoInfo = null;
            _currentLocalPath = null;
            _currentZipPath = null;
            _zipEntryMap?.Clear();
            _zipEntryMap = null;
            _extensionTreeNodes.Clear();
            flowLayoutPanelExtensions.Controls.Clear();
            flowLayoutPanelExtensions.Visible = false;
        }

        private void SetStatus(string message, bool isError = false)
        {
            if (lblStatus == null) { Console.WriteLine($"Status Label Missing! Msg: {message}"); return; }
            if (lblStatus.InvokeRequired) { lblStatus.Invoke(new Action(() => { SetStatus(message, isError); })); }
            else
            {
                lblStatus.Text = message;
                lblStatus.ForeColor = isError ? Color.Red : SystemColors.ControlText;
            }
            Console.WriteLine($"Status: {message}" + (isError ? " (Error)" : ""));
        }

        private void PopulateTreeView(List<FileItem> items)
        {
            treeViewFiles.Nodes.Clear();
            _extensionTreeNodes.Clear();

            if (!items.Any())
            {
                SetStatus("No files found or none remaining after filtering.", false);
                CreateExtensionFiltersUI();
                return;
            }

            _isUpdatingTreeViewProgrammatically = true;
            treeViewFiles.BeginUpdate();

            var nodeLookup = new Dictionary<string, TreeNode>();
            items.Sort((a, b) => string.Compare(a.DisplayPath, b.DisplayPath, StringComparison.OrdinalIgnoreCase));

            foreach (var item in items)
            {
                string currentPath = "";
                TreeNode parentNode = null;
                var pathParts = item.DisplayPath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i < pathParts.Length; i++)
                {
                    bool isLastPart = (i == pathParts.Length - 1);
                    string part = pathParts[i];
                    string nodeKey = string.IsNullOrEmpty(currentPath) ? part : $"{currentPath}/{part}";
                    TreeNodeCollection currentNodeCollection = (parentNode == null) ? treeViewFiles.Nodes : parentNode.Nodes;
                    TreeNode existingNode = FindNodeByText(currentNodeCollection, part);
                    TreeNode newNode = null;

                    if (existingNode == null)
                    {
                        newNode = new TreeNode(part);
                        if (isLastPart)
                        {
                            newNode.Tag = item; newNode.Checked = !item.IsDirectory;
                        }
                        else
                        {
                            newNode.Tag = new FileItem { Name = part, Path = nodeKey, DisplayPath = nodeKey, IsDirectory = true, SourceType = item.SourceType };
                            newNode.Checked = false;
                        }
                        currentNodeCollection.Add(newNode);
                        nodeLookup[nodeKey] = newNode;
                        parentNode = newNode;
                        if (isLastPart && !item.IsDirectory) AddNodeToExtensionMap(newNode);
                    }
                    else
                    {
                        parentNode = existingNode;
                        if (isLastPart && !item.IsDirectory && parentNode.Tag is FileItem existingFileTag && existingFileTag.IsDirectory)
                        {
                            parentNode.Tag = item; parentNode.Checked = true; AddNodeToExtensionMap(parentNode);
                        }
                        else if (!isLastPart && parentNode.Tag is FileItem tag && !tag.IsDirectory)
                        {
                            Console.WriteLine($"Path conflict: Item '{item.DisplayPath}' requires directory '{part}', but a file node already exists.");
                            parentNode = null; break;
                        }
                    }
                }
            }

            foreach (TreeNode node in treeViewFiles.Nodes) UpdateAllParentStatesBottomUp(node);

            CreateExtensionFiltersUI();

            foreach (TreeNode node in treeViewFiles.Nodes) node.Expand();

            treeViewFiles.EndUpdate();
            _isUpdatingTreeViewProgrammatically = false;
        }

        private TreeNode FindNodeByText(TreeNodeCollection nodes, string text)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.Text.Equals(text, StringComparison.OrdinalIgnoreCase)) { return node; }
            }
            return null;
        }

        private void AddNodeToExtensionMap(TreeNode fileNode)
        {
            if (fileNode.Tag is FileItem item && !item.IsDirectory)
            {
                string extension = Path.GetExtension(item.Name).TrimStart('.').ToLowerInvariant();
                if (!string.IsNullOrEmpty(extension))
                {
                    if (!_extensionTreeNodes.ContainsKey(extension)) _extensionTreeNodes[extension] = new List<TreeNode>();
                    if (!_extensionTreeNodes[extension].Contains(fileNode)) _extensionTreeNodes[extension].Add(fileNode);
                }
            }
        }

        private List<FileItem> GetSelectedFiles(TreeNodeCollection nodes)
        {
            var selected = new List<FileItem>();
            foreach (TreeNode node in nodes)
            {
                if (node.Checked && node.Tag is FileItem item && !item.IsDirectory) selected.Add(item);
                if (node.Nodes.Count > 0) selected.AddRange(GetSelectedFiles(node.Nodes));
            }
            return selected.Distinct().ToList();
        }

        private async Task<string> GenerateFormattedText(List<FileItem> selectedFiles)
        {
            var sb = new StringBuilder();
            var indexSb = new StringBuilder("Directory Structure:\n\n");
            var root = new Dictionary<string, object>();

            foreach (var item in selectedFiles.OrderBy(f => f.DisplayPath))
            {
                var parts = item.DisplayPath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
                var currentLevel = root;
                for (int i = 0; i < parts.Length; i++)
                {
                    string part = parts[i];
                    bool isLast = (i == parts.Length - 1);
                    if (!currentLevel.ContainsKey(part)) currentLevel[part] = isLast ? null : new Dictionary<string, object>();
                    if (!isLast)
                    {
                        if (currentLevel[part] == null) currentLevel[part] = new Dictionary<string, object>();
                        if (currentLevel[part] is Dictionary<string, object> subDir) currentLevel = subDir;
                        else { Console.WriteLine($"Type error for part '{part}'."); break; }
                    }
                }
            }
            BuildIndexStringRecursive(root, indexSb, "", true);
            sb.Append(indexSb.ToString());
            sb.AppendLine();

            string token = txtAccessToken.Text.Trim();
            foreach (var file in selectedFiles.OrderBy(f => f.Path))
            {
                string content = $"Error: Could not retrieve content for {file.DisplayPath}";
                try
                {
                    switch (file.SourceType)
                    {
                        case DataSourceType.GitHub:
                            if (_currentRepoInfo == null || !_currentRepoInfo.IsValid || string.IsNullOrEmpty(_currentRepoInfo.ResolvedRef))
                            {
                                content = $"Error: GitHub repo info missing/invalid for {file.Path}."; break;
                            }
                            content = await GitHubService.GetFileContentAsync(
                                _currentRepoInfo.Owner, _currentRepoInfo.RepoName,
                                file.Path, _currentRepoInfo.ResolvedRef, token);
                            break;
                        case DataSourceType.LocalDirectory:
                            content = await LocalFileService.ReadLocalFileContentAsync(file.SourceUrl);
                            break;
                        case DataSourceType.LocalZip:
                            if (string.IsNullOrEmpty(_currentZipPath))
                            {
                                content = $"Error: ZIP path missing for {file.Path}."; break;
                            }
                            try
                            {
                                using (var archive = ZipFile.OpenRead(_currentZipPath))
                                {
                                    var entry = archive.GetEntry(file.SourceUrl);
                                    if (entry != null) content = await LocalFileService.ReadZipEntryContentAsync(entry);
                                    else content = $"Error: ZIP entry not found ({file.Path}).";
                                }
                            }
                            catch (Exception zipEx) { content = $"Error reading ZIP entry for {file.Path}: {zipEx.Message}"; }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    content = $"Error processing {file.DisplayPath}: {ex.Message}";
                    Console.WriteLine(content);
                }
                sb.AppendLine().AppendLine("---").AppendLine($"File: /{file.DisplayPath.TrimStart('/')}").AppendLine("---").AppendLine().AppendLine(content ?? $"Error: Content for {file.DisplayPath} was null.");
            }
            return sb.ToString();
        }

        private void BuildIndexStringRecursive(Dictionary<string, object> directory, StringBuilder output, string prefix, bool isRoot = false)
        {
            var entries = directory.OrderBy(kvp => kvp.Key).ToList();
            for (int i = 0; i < entries.Count; i++)
            {
                var kvp = entries[i];
                bool isLast = (i == entries.Count - 1);
                string connector = isLast ? "└── " : "├── ";
                string name = kvp.Key;
                output.Append(prefix + connector + name + Environment.NewLine);
                if (kvp.Value is Dictionary<string, object> subDirectory)
                {
                    string childPrefix = prefix + (isLast ? "    " : "│   ");
                    BuildIndexStringRecursive(subDirectory, output, childPrefix);
                }
            }
        }

        private void UpdateTokenCountLabel(int count)
        {
            if (lblTokenCount == null) return;
            if (lblTokenCount.InvokeRequired) { lblTokenCount.Invoke(new Action(() => UpdateTokenCountLabel(count))); }
            else
            {
                string textToShow; Color textColor = SystemColors.ControlText; bool visible = true;
                if (count == -1) { textToShow = "Token count failed."; textColor = Color.OrangeRed; }
                else if (count == -2) { textToShow = "Tokenizer unavailable."; textColor = Color.OrangeRed; }
                else if (count >= 0) { textToShow = $"Approximate Token Count: {count:N0} (cl100k_base)"; }
                else { textToShow = ""; visible = false; }
                lblTokenCount.Text = textToShow; lblTokenCount.ForeColor = textColor; lblTokenCount.Visible = visible;
            }
        }

        private void FlowLayoutPanelExtensions_SizeChanged(object sender, EventArgs e)
        {
            if (this.DesignMode || this.IsDisposed || !this.IsHandleCreated || _isProgrammaticallyAdjustingSplitter)
            {
                return;
            }

            FlowLayoutPanel flp = sender as FlowLayoutPanel;
            if (flp != null && splitContainer2 != null)
            {
                _isProgrammaticallyAdjustingSplitter = true;
                try
                {
                    int newSplitterTargetHeight = flp.Height;
                    int panel1Min = splitContainer2.Panel1MinSize;
                    int panel1Max = splitContainer2.ClientSize.Height - splitContainer2.Panel2MinSize;

                    if (panel1Max < panel1Min)
                    {
                        panel1Max = panel1Min;
                    }

                    int newSplitterDistance = Math.Max(panel1Min, Math.Min(newSplitterTargetHeight, panel1Max));

                    if (newSplitterDistance > 0 &&
                        newSplitterDistance < splitContainer2.ClientSize.Height &&
                        splitContainer2.SplitterDistance != newSplitterDistance)
                    {
                        if (splitContainer2.ClientSize.Height >= (splitContainer2.Panel1MinSize + splitContainer2.Panel2MinSize))
                        {
                            splitContainer2.SplitterDistance = newSplitterDistance;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in FlowLayoutPanelExtensions_SizeChanged: {ex.Message}");
                }
                finally
                {
                    _isProgrammaticallyAdjustingSplitter = false;
                }
            }
        }
    }
}