using Octokit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text; // Needed for Encoding
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Repo2Text // Add namespace
{
    // Define helper classes/enums within the namespace
    public class FileItem
    {
        public string Name { get; set; }
        public string Path { get; set; } // Full path relative to root (GitHub path, Local relative path, Zip entry FullName)
        public string DisplayPath { get; set; } // Path used for display/sorting in TreeView (usually relative)
        public bool IsDirectory { get; set; }
        public string SourceUrl { get; set; } // API URL (GitHub) or Full Local File Path or ZIP Entry FullName
        public DataSourceType SourceType { get; set; }

        public override string ToString() => Name;
    }

    public enum DataSourceType
    {
        GitHub,
        LocalDirectory,
        LocalZip
    }

    public class GitHubRepoInfo
    {
        public string Owner { get; set; }
        public string RepoName { get; set; }
        public string BranchOrSha { get; set; } // Input from URL, potentially updated after resolve
        public string Path { get; set; } // Input from URL, potentially updated after resolve
        public bool IsValid { get; set; }
        public string ResolvedRef { get; set; } // Store the actually used ref after resolution
    }


    public static class GitHubService
    {
        private static GitHubClient GetClient(string token = null)
        {
            var client = new GitHubClient(new ProductHeaderValue("Repo2TextApp-Standalone"));
            if (!string.IsNullOrEmpty(token))
            {
                client.Credentials = new Credentials(token);
            }
            return client;
        }

        public static GitHubRepoInfo ParseRepoUrl(string url)
        {
            url = url.Trim().TrimEnd('/');
            var regex = new Regex(@"^https?://github\.com/([^/]+)/([^/]+)(?:/(?:tree|blob)/([^/]+)(/(.*))?)?$", RegexOptions.IgnoreCase);
            var match = regex.Match(url);

            if (match.Success)
            {
                return new GitHubRepoInfo
                {
                    Owner = match.Groups[1].Value,
                    RepoName = match.Groups[2].Value,
                    BranchOrSha = match.Groups[3].Value,
                    Path = match.Groups[5].Value?.Trim('/'),
                    IsValid = true
                };
            }

            regex = new Regex(@"^https?://github\.com/([^/]+)/([^/]+)/?(.*)?$", RegexOptions.IgnoreCase);
            match = regex.Match(url);

            if (match.Success)
            {
                return new GitHubRepoInfo
                {
                    Owner = match.Groups[1].Value,
                    RepoName = match.Groups[2].Value,
                    BranchOrSha = match.Groups[3].Value,
                    Path = "",
                    IsValid = true
                };
            }

            return new GitHubRepoInfo { IsValid = false };
        }

        // Corrected ResolveRefAndPath
        private static async Task<(string ResolvedRef, string ResolvedPath)> ResolveRefAndPath(GitHubClient client, string owner, string repoName, string potentialRefOrPath)
        {
            string defaultBranch = null;
            try
            {
                var repository = await client.Repository.Get(owner, repoName);
                defaultBranch = repository.DefaultBranch;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching default branch for {owner}/{repoName}: {ex.Message}");
                throw new InvalidOperationException("Could not determine default branch to resolve reference.", ex);
            }

            if (string.IsNullOrEmpty(potentialRefOrPath))
            {
                return (defaultBranch, "");
            }

            // 1. Check if it's a known BRANCH name
            try
            {
                await client.Repository.Branch.Get(owner, repoName, potentialRefOrPath);
                return (potentialRefOrPath, "");
            }
            catch (NotFoundException) { /* Continue */ }
            catch (ApiException apiEx) when (apiEx.StatusCode == System.Net.HttpStatusCode.NotFound) { /* Continue */}

            // 2. Check if it's a known TAG name
            try
            {
                // *** CORRECTED LINE HERE ***
                await client.Git.Reference.Get(owner, repoName, $"tags/{potentialRefOrPath}");
                // It's a valid tag name
                return (potentialRefOrPath, "");
            }
            catch (NotFoundException) { /* Continue */ }
            catch (ApiException apiEx) when (apiEx.StatusCode == System.Net.HttpStatusCode.NotFound) { /* Continue */}

            // 3. Assume it's a path under the default branch.
            return (defaultBranch, potentialRefOrPath);
        }


        public static async Task<List<FileItem>> GetRepositoryTreeAsync(GitHubRepoInfo repoInfo, string token)
        {
            if (repoInfo == null || !repoInfo.IsValid)
            {
                throw new ArgumentException("Invalid GitHubRepoInfo provided.");
            }

            var client = GetClient(token);
            var fileItems = new List<FileItem>();

            try
            {
                string targetRef;
                string targetPath;

                // Handle explicit /tree/ or /blob/ URLs first
                if (!string.IsNullOrEmpty(repoInfo.BranchOrSha) && repoInfo.Path != null) // Path can be ""
                {
                    targetRef = repoInfo.BranchOrSha;
                    targetPath = repoInfo.Path;
                    // Optionally validate ref exists here
                }
                else // Handle URLs like /owner/repo or /owner/repo/maybeRefOrPath
                {
                    string potentialRefOrPath = repoInfo.BranchOrSha ?? repoInfo.Path ?? "";
                    (targetRef, targetPath) = await ResolveRefAndPath(client, repoInfo.Owner, repoInfo.RepoName, potentialRefOrPath);
                }

                // Update the passed-in repoInfo object with resolved values
                repoInfo.ResolvedRef = targetRef;
                repoInfo.Path = targetPath; // Update Path as it might have been resolved

                var tree = await client.Git.Tree.GetRecursive(repoInfo.Owner, repoInfo.RepoName, targetRef);

                string rootPathPrefix = string.IsNullOrEmpty(targetPath) ? "" : targetPath.TrimEnd('/') + "/";

                foreach (var item in tree.Tree)
                {
                    // Filter by path if specified
                    if (string.IsNullOrEmpty(targetPath) || item.Path.StartsWith(rootPathPrefix) || item.Path.Equals(targetPath))
                    {
                        string relativePath = item.Path;
                        if (!string.IsNullOrEmpty(targetPath))
                        {
                            if (item.Path.Equals(targetPath)) // Exact match (file or dir)
                            {
                                relativePath = Path.GetFileName(item.Path);
                                if (string.IsNullOrEmpty(relativePath)) relativePath = item.Path; // Keep if root
                            }
                            else if (item.Path.StartsWith(rootPathPrefix))
                            {
                                relativePath = item.Path.Substring(rootPathPrefix.Length);
                            }
                            else
                            {
                                continue;
                            }
                        }

                        if (string.IsNullOrEmpty(relativePath)) continue;


                        fileItems.Add(new FileItem
                        {
                            Name = Path.GetFileName(item.Path),
                            Path = item.Path,
                            DisplayPath = relativePath,
                            IsDirectory = item.Type == TreeType.Tree,
                            SourceUrl = item.Url,
                            SourceType = DataSourceType.GitHub
                        });
                    }
                }
                repoInfo.IsValid = true; // Mark as valid since fetch succeeded
                return fileItems;
            }
            catch (ApiException ex)
            {
                Console.WriteLine($"GitHub API Error: {ex.Message} (Status: {ex.StatusCode})");
                if (repoInfo != null) repoInfo.IsValid = false;
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Generic Error in GetRepositoryTreeAsync: {ex.Message}");
                if (repoInfo != null) repoInfo.IsValid = false;
                throw;
            }
        }


        public static async Task<string> GetFileContentAsync(string owner, string repoName, string filePath, string branchOrSha, string token)
        {
            var client = GetClient(token);
            try
            {
                // Try raw content first
                try
                {
                    var rawBytes = await client.Repository.Content.GetRawContentByRef(owner, repoName, filePath, branchOrSha);
                    try
                    {
                        // Try UTF-8 first (most common)
                        return System.Text.Encoding.UTF8.GetString(rawBytes);
                    }
                    catch (DecoderFallbackException)
                    {
                        // Fallback if UTF-8 fails (e.g., for binary files or different encodings)
                        Console.WriteLine($"UTF-8 decoding failed for {filePath}, trying default encoding.");
                        return System.Text.Encoding.Default.GetString(rawBytes);
                    }
                }
                catch (NotFoundException) { /* Fall through */ }
                catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound) { /* Fall through */}

                // Fallback to GetContents (handles Base64)
                var contents = await client.Repository.Content.GetAllContentsByRef(owner, repoName, filePath, branchOrSha);
                if (contents != null && contents.Count > 0)
                {
                    var fileContent = contents[0];
                    if (!string.IsNullOrEmpty(fileContent.EncodedContent))
                    {
                        string base64Content = fileContent.EncodedContent.Replace("\n", "").Replace("\r", "");
                        var bytes = Convert.FromBase64String(base64Content);
                        try
                        {
                            return System.Text.Encoding.UTF8.GetString(bytes);
                        }
                        catch (DecoderFallbackException)
                        {
                            Console.WriteLine($"UTF-8 decoding failed for Base64 content {filePath}, trying default encoding.");
                            return System.Text.Encoding.Default.GetString(bytes); // Fallback encoding
                        }
                    }
                    else if (fileContent.Content != null)
                    {
                        return fileContent.Content;
                    }
                    else
                    {
                        return $"Error: No content (neither encoded nor direct) found for {filePath}.";
                    }
                }
                return $"Error: File not found or empty ({filePath}).";
            }
            catch (NotFoundException)
            {
                Console.WriteLine($"File not found (final): {owner}/{repoName}/{filePath} @ {branchOrSha}");
                return $"Error: File not found ({filePath}).";
            }
            catch (ApiException ex)
            {
                Console.WriteLine($"GitHub API Error fetching content: {ex.StatusCode} - {ex.Message}");
                if (ex.ApiError?.Message?.Contains("larger than") == true)
                {
                    return $"Error: File is too large to be retrieved via the API ({filePath}).";
                }
                return $"Error fetching {filePath}: API Error ({ex.StatusCode})";
            }
            catch (FormatException fx)
            {
                Console.WriteLine($"Base64 Format Error for {filePath}: {fx.Message}");
                return $"Error: Could not decode file content (Invalid Base64) for {filePath}.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Generic Error fetching content for {filePath}: {ex.Message}");
                return $"Error fetching {filePath}: {ex.Message}";
            }
        }
    }
} // End of namespace