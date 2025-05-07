using Octokit; // Still needed for Octokit exceptions
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms; // Keep this for Form and UI controls

namespace Repo2Text // Add namespace
{
    public partial class MainForm : Form
    {
        private GitHubRepoInfo _currentRepoInfo;
        private string _currentLocalPath;
        private string _currentZipPath;
        private DataSourceType _currentSourceType;
        private Dictionary<string, ZipArchiveEntry> _zipEntryMap;
        private bool _isUpdatingCheckState = false;

        public MainForm()
        {
            InitializeComponent();
            // Load settings *after* InitializeComponent
            LoadSettings();
            // Ensure event handlers are attached
            AttachEventHandlers();
        }

        // --- Load Event ---
        private void MainForm_Load(object sender, EventArgs e)
        {
            SetStatus("Ready. Select a source.", false);
        }

        // --- Helper to attach event handlers cleanly ---
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

        // --- Settings ---
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

        // --- Event Handlers ---

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
            // Ensure _currentRepoInfo is initialized before passing to service
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

            try
            {
                // Pass the _currentRepoInfo instance to be potentially updated by the service
                var files = await GitHubService.GetRepositoryTreeAsync(_currentRepoInfo, token);

                if (!_currentRepoInfo.IsValid) // Check if service marked it invalid
                {
                    SetStatus("Failed to resolve repository details. Check URL and token.", true);
                    return; // Don't proceed if resolution failed
                }

                PopulateTreeView(files);
                string displayPath = string.IsNullOrEmpty(_currentRepoInfo.Path) ? "/" : $"/{_currentRepoInfo.Path}";
                // Use ResolvedRef which is set inside GetRepositoryTreeAsync
                SetStatus($"Fetched {files.Count} items from '{_currentRepoInfo.ResolvedRef}' in path '{displayPath}'. Select files and generate.", false);
                btnGenerateText.Enabled = files.Any();
                btnDownloadZip.Enabled = true;
            }
            catch (Exception ex)
            {
                HandleFetchError(ex); // Use a helper for consistent error handling
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
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    ClearUIState();
                    _currentLocalPath = dialog.SelectedPath;
                    SetStatus($"Loading files from {_currentLocalPath}...");
                    _currentSourceType = DataSourceType.LocalDirectory;

                    try
                    {
                        this.Cursor = Cursors.WaitCursor; // Indicate loading
                        var files = await LocalFileService.GetDirectoryFilesAsync(_currentLocalPath);
                        PopulateTreeView(files);
                        SetStatus($"Loaded {files.Count} files. Select files and generate.", false);
                        btnGenerateText.Enabled = files.Any();
                        btnDownloadZip.Enabled = false;
                    }
                    catch (Exception ex)
                    {
                        SetStatus($"Error reading directory: {ex.Message}", true);
                        txtOutput.Text = $"Error reading directory: {ex.Message}\n\nPlease ensure the application has permission to read the selected directory and its subdirectories.";
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
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    ClearUIState();
                    _currentZipPath = dialog.FileName;
                    SetStatus($"Loading files from {_currentZipPath}...");
                    _currentSourceType = DataSourceType.LocalZip;

                    try
                    {
                        this.Cursor = Cursors.WaitCursor;
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
                        txtOutput.Text = $"Error reading ZIP file: {ioEx.Message}\n\nEnsure the file is a valid ZIP archive and not corrupted.";
                    }
                    catch (InvalidDataException invEx)
                    {
                        SetStatus($"Invalid ZIP data: {invEx.Message}", true);
                        txtOutput.Text = $"Invalid ZIP file format: {invEx.Message}\n\nThe file might be corrupted or not a standard ZIP file.";
                    }
                    catch (Exception ex)
                    {
                        SetStatus($"Error reading ZIP file: {ex.Message}", true);
                        txtOutput.Text = $"An unexpected error occurred reading the ZIP file: {ex.Message}";
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
            btnCopy.Enabled = false;
            btnDownloadText.Enabled = false;
            // *** FIXED: Use fully qualified name for Application ***
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

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    string savePath = sfd.FileName;
                    string token = txtAccessToken.Text.Trim();
                    SetStatus($"Downloading {selectedFiles.Count} files to ZIP...");
                    this.Cursor = Cursors.WaitCursor;

                    try
                    {
                        // *** FIXED: Use System.IO.FileMode ***
                        using (var fileStream = new FileStream(savePath, System.IO.FileMode.Create))
                        using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Create))
                        {
                            foreach (var fileItem in selectedFiles)
                            {
                                string fileRef = _currentRepoInfo.ResolvedRef;
                                string content = await GitHubService.GetFileContentAsync(
                                    _currentRepoInfo.Owner,
                                    _currentRepoInfo.RepoName,
                                    fileItem.Path,
                                    fileRef,
                                    token);

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
                    Clipboard.SetText(txtOutput.Text);
                    SetStatus("Text copied to clipboard.", false);
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
                if (sfd.ShowDialog() == DialogResult.OK)
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

        // --- TreeView Checkbox Handling ---
        private void treeViewFiles_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (_isUpdatingCheckState) return;
            _isUpdatingCheckState = true;
            try
            {
                CheckChildrenRecursive(e.Node, e.Node.Checked);
                UpdateParentCheckState(e.Node.Parent);
            }
            finally
            {
                _isUpdatingCheckState = false;
            }
        }

        private void CheckChildrenRecursive(TreeNode node, bool isChecked)
        {
            foreach (TreeNode childNode in node.Nodes)
            {
                if (childNode.Checked != isChecked)
                {
                    childNode.Checked = isChecked;
                    // Check grandchildren ONLY if we're explicitly checking recursively here.
                    // If relying on the event, this recursive call is not needed.
                    // CheckChildrenRecursive(childNode, isChecked);
                }
            }
        }

        // *** ENSURE THIS METHOD EXISTS WITHIN THE MainForm CLASS ***
        private void UpdateParentCheckState(TreeNode parentNode)
        {
            if (parentNode == null) return; // No parent

            int checkedCount = 0;
            int uncheckedCount = 0;

            foreach (TreeNode childNode in parentNode.Nodes)
            {
                if (childNode.Checked)
                {
                    checkedCount++;
                }
                else
                {
                    uncheckedCount++;
                }
            }

            bool newState;
            if (checkedCount == parentNode.Nodes.Count)
            {
                newState = true; // All children checked
            }
            else if (uncheckedCount == parentNode.Nodes.Count)
            {
                newState = false; // All children unchecked
            }
            else
            {
                newState = false; // Mixed state - Treat as unchecked visually
            }

            if (parentNode.Checked != newState)
            {
                parentNode.Checked = newState; // This will trigger AfterCheck for the parent
            }

            // We don't need to explicitly recurse here anymore because setting
            // parentNode.Checked = newState will trigger its AfterCheck event,
            // which will then call UpdateParentCheckState for *its* parent.
            // UpdateParentCheckState(parentNode.Parent); // REMOVE THIS RECURSIVE CALL HERE
        }


        // --- UI and Logic Helper Methods ---

        private void ClearUIState()
        {
            treeViewFiles.Nodes.Clear();
            txtOutput.Text = "";
            SetStatus("Ready.", false); // Set initial status
            btnGenerateText.Enabled = false;
            btnCopy.Enabled = false;
            btnDownloadText.Enabled = false;
            btnDownloadZip.Enabled = false;
            _currentRepoInfo = null;
            _currentLocalPath = null;
            _currentZipPath = null;
            _zipEntryMap?.Clear();
            _zipEntryMap = null;
        }

        private void SetStatus(string message, bool isError = false)
        {
            // *** FIXED: Check if lblStatus exists and is accessible ***
            // Ensure lblStatus is the correct name of your Label control added in the designer.
            if (lblStatus == null)
            {
                Console.WriteLine($"Status Control Error: lblStatus is null. Message: {message}");
                return;
            }

            if (lblStatus.InvokeRequired)
            {
                lblStatus.Invoke(new Action(() => SetStatus(message, isError)));
            }
            else
            {
                lblStatus.Text = message;
                lblStatus.ForeColor = isError ? System.Drawing.Color.Red : System.Drawing.SystemColors.ControlText;
                Console.WriteLine($"Status: {message}" + (isError ? " (Error)" : ""));
            }
        }

        private void PopulateTreeView(List<FileItem> items)
        {
            treeViewFiles.Nodes.Clear();
            if (!items.Any())
            {
                SetStatus("No files found in the source.", false);
                return;
            }

            treeViewFiles.BeginUpdate();

            var rootNode = new TreeNode("Source Root") { Tag = "ROOT_NODE_TAG" };
            treeViewFiles.Nodes.Add(rootNode);

            var nodeLookup = new Dictionary<string, TreeNode>();
            nodeLookup[""] = rootNode;

            // Sort primarily by path depth, then by type (dir first), then name
            items.Sort((a, b) => {
                int depthA = a.DisplayPath.Split('/', '\\').Length;
                int depthB = b.DisplayPath.Split('/', '\\').Length;
                if (depthA != depthB) return depthA.CompareTo(depthB);
                if (a.IsDirectory != b.IsDirectory) return a.IsDirectory ? -1 : 1; // Directories first (though we add files first below)
                return string.Compare(a.DisplayPath, b.DisplayPath, StringComparison.OrdinalIgnoreCase);
            });


            foreach (var item in items)
            {
                string parentPathKey = Path.GetDirectoryName(item.DisplayPath)?.Replace('\\', '/') ?? "";
                string name = Path.GetFileName(item.DisplayPath);
                string currentNodeKey = item.DisplayPath.Replace('\\', '/');

                if (string.IsNullOrEmpty(name)) name = item.DisplayPath; // Handle root-level items

                TreeNode parentNode;
                if (!nodeLookup.TryGetValue(parentPathKey, out parentNode))
                {
                    // If parent doesn't exist, create it recursively. This builds the directory structure implicitly.
                    parentNode = FindOrCreateParentNode(rootNode, nodeLookup, parentPathKey, item.SourceType);
                    if (parentNode == null)
                    {
                        Console.WriteLine($"Warning: Could not find/create parent '{parentPathKey}' for '{name}'. Adding to root.");
                        parentNode = rootNode;
                    }
                }

                // Avoid adding duplicates if path normalization leads to collisions
                if (!nodeLookup.ContainsKey(currentNodeKey))
                {
                    var newNode = new TreeNode(name)
                    {
                        Tag = item,
                        // Example: Check files by default
                        Checked = !item.IsDirectory
                    };
                    parentNode.Nodes.Add(newNode);
                    nodeLookup[currentNodeKey] = newNode;
                }
                else
                {
                    // Node already exists (likely a directory created implicitly), update its tag if this is the actual file item
                    if (!item.IsDirectory && nodeLookup[currentNodeKey].Tag is FileItem existingTag && existingTag.IsDirectory)
                    {
                        nodeLookup[currentNodeKey].Tag = item;
                        nodeLookup[currentNodeKey].Checked = true; // Check the file node
                    }
                }
            }

            // Set initial parent check states after all nodes are added
            _isUpdatingCheckState = true;
            UpdateCheckStatesFromBottom(rootNode); // Update starting from the root
            _isUpdatingCheckState = false;

            rootNode.Expand();
            // Optionally expand only the first level of real folders/files
            if (rootNode.Nodes.Count > 0)
            {
                foreach (TreeNode node in rootNode.Nodes) node.Expand();
            }


            treeViewFiles.EndUpdate();
        }

        // Helper to recursively find or create parent nodes
        private TreeNode FindOrCreateParentNode(TreeNode root, Dictionary<string, TreeNode> lookup, string pathKey, DataSourceType sourceType)
        {
            if (string.IsNullOrEmpty(pathKey)) return root;
            if (lookup.TryGetValue(pathKey, out var existingNode)) return existingNode;

            string parentPathKey = Path.GetDirectoryName(pathKey)?.Replace('\\', '/') ?? "";
            string name = Path.GetFileName(pathKey);
            if (string.IsNullOrEmpty(name)) return root; // Cannot create node for empty name

            TreeNode parent = FindOrCreateParentNode(root, lookup, parentPathKey, sourceType);
            if (parent == null) return null;

            var newNode = new TreeNode(name)
            {
                Tag = new FileItem { Name = name, Path = pathKey, DisplayPath = pathKey, IsDirectory = true, SourceType = sourceType },
                Checked = false // Directories start unchecked
            };
            parent.Nodes.Add(newNode);
            lookup[pathKey] = newNode;
            return newNode;
        }

        // Helper to update parent check states after initial population
        private void UpdateCheckStatesFromBottom(TreeNode node)
        {
            foreach (TreeNode child in node.Nodes)
            {
                UpdateCheckStatesFromBottom(child);
            }
            // Only call the state update logic if it's not the virtual root node
            if (node.Parent != null)
            {
                UpdateParentCheckState(node);
            }
        }

        private List<FileItem> GetSelectedFiles(TreeNodeCollection nodes)
        {
            var selected = new List<FileItem>();
            foreach (TreeNode node in nodes)
            {
                // Only add if it's checked AND represents a file
                if (node.Checked && node.Tag is FileItem item && !item.IsDirectory)
                {
                    selected.Add(item);
                }
                // Recurse into children even if parent isn't fully checked
                if (node.Nodes.Count > 0)
                {
                    selected.AddRange(GetSelectedFiles(node.Nodes));
                }
            }
            return selected.Distinct().ToList(); // Use Distinct for safety
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

                    if (!currentLevel.ContainsKey(part))
                    {
                        currentLevel[part] = isLast ? null : new Dictionary<string, object>();
                    }

                    if (!isLast && currentLevel[part] is Dictionary<string, object> subDir)
                    {
                        currentLevel = subDir;
                    }
                    else if (!isLast && currentLevel[part] == null) // Path conflict: file exists where dir needed
                    {
                        // Overwrite the file marker with a directory dictionary
                        currentLevel[part] = new Dictionary<string, object>();
                        currentLevel = (Dictionary<string, object>)currentLevel[part];
                    }
                    else if (!isLast && currentLevel[part] is Dictionary<string, object>)
                    {
                        // Already exists as dictionary, just descend
                        currentLevel = (Dictionary<string, object>)currentLevel[part];
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
                                content = $"Error: GitHub repository info is missing or invalid for {file.Path}.";
                                break;
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
                                content = $"Error: ZIP path is missing for {file.Path}.";
                                break;
                            }
                            try
                            {
                                using (var archive = ZipFile.OpenRead(_currentZipPath))
                                {
                                    // SourceUrl stored the entry FullName
                                    var entry = archive.GetEntry(file.SourceUrl);
                                    if (entry != null)
                                    {
                                        content = await LocalFileService.ReadZipEntryContentAsync(entry);
                                    }
                                    else
                                    {
                                        content = $"Error: ZIP entry not found ({file.Path}) in archive.";
                                    }
                                }
                            }
                            catch (Exception zipEx)
                            {
                                content = $"Error reading ZIP entry for {file.Path}: {zipEx.Message}";
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    content = $"Error processing {file.DisplayPath}: {ex.Message}";
                    Console.WriteLine(content);
                }

                sb.AppendLine();
                sb.AppendLine("---");
                sb.AppendLine($"File: /{file.DisplayPath.TrimStart('/')}");
                sb.AppendLine("---");
                sb.AppendLine();
                sb.AppendLine(content ?? $"Error: Content for {file.DisplayPath} was null or could not be read.");
            }

            return sb.ToString();
        }

        // Recursive helper for building index string
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

    } // End of MainForm class
} // End of namespace