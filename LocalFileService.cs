namespace Repo2Text
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Text.RegularExpressions; // For GitIgnore

    public static class LocalFileService
    {
        // --- GitIgnore Handling ---
        private static List<Regex> ParseGitIgnore(string gitignoreContent)
        {
            var regexList = new List<Regex>();
            if (string.IsNullOrEmpty(gitignoreContent)) return regexList;

            var lines = gitignoreContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                         .Select(line => line.Trim())
                                         .Where(line => !string.IsNullOrEmpty(line) && !line.StartsWith("#"));

            foreach (var line in lines)
            {
                try
                {
                    // Basic conversion, not fully compliant with all gitignore features
                    string pattern = Regex.Escape(line).Replace(@"\*", ".*").Replace(@"\?", ".");

                    // Handle directory matching (ending with /)
                    if (pattern.EndsWith(@"\/"))
                    {
                        pattern = pattern.TrimEnd('/') + "(/.*)?$"; // Match directory or anything inside it
                    }
                    // Handle root matching (starting with /)
                    if (pattern.StartsWith(@"\/"))
                    {
                        pattern = "^" + pattern.Substring(2); // Match from start, remove escaped slash
                    }
                    else
                    {
                        pattern = "(^|/)" + pattern; // Match start or after slash if not rooted
                    }


                    regexList.Add(new Regex(pattern, RegexOptions.IgnoreCase));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing gitignore line '{line}': {ex.Message}");
                }
            }
            return regexList;
        }

        private static bool IsIgnored(string relativePath, List<Regex> ignorePatterns)
        {
            // Normalize path separators
            string normalizedPath = relativePath.Replace(Path.DirectorySeparatorChar, '/').TrimStart('/');
            return ignorePatterns.Any(regex => regex.IsMatch(normalizedPath));
        }

        // --- Directory Reading ---
        public static async Task<List<FileItem>> GetDirectoryFilesAsync(string rootPath)
        {
            var fileItems = new List<FileItem>();
            var gitignorePath = Path.Combine(rootPath, ".gitignore");
            string gitignoreContent = null;
            List<Regex> ignorePatterns = new List<Regex> { new Regex(@"\.git(/.*)?$", RegexOptions.IgnoreCase) }; // Always ignore .git

            if (File.Exists(gitignorePath))
            {
                try
                {
                    gitignoreContent = await File.ReadAllTextAsync(gitignorePath);
                    ignorePatterns.AddRange(ParseGitIgnore(gitignoreContent));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading .gitignore: {ex.Message}");
                }
            }

            var allFiles = Directory.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories);

            foreach (var file in allFiles)
            {
                string relativePath = Path.GetRelativePath(rootPath, file);
                if (!IsIgnored(relativePath, ignorePatterns))
                {
                    fileItems.Add(new FileItem
                    {
                        Name = Path.GetFileName(file),
                        Path = relativePath, // Use relative path for consistency
                        DisplayPath = relativePath,
                        IsDirectory = false,
                        SourceUrl = file, // Store the full local path
                        SourceType = DataSourceType.LocalDirectory
                    });
                }
            }
            return fileItems;
        }

        public static async Task<string> ReadLocalFileContentAsync(string fullPath)
        {
            try
            {
                // Detect encoding or default to UTF-8
                using (var reader = new StreamReader(fullPath, Encoding.UTF8, true)) // `true` detects BOM
                {
                    return await reader.ReadToEndAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading file {fullPath}: {ex.Message}");
                return $"Error reading {Path.GetFileName(fullPath)}: {ex.Message}";
            }
        }


        // --- ZIP Reading ---
        public static async Task<(List<FileItem> files, Dictionary<string, ZipArchiveEntry> entryMap)> GetZipFilesAsync(string zipPath)
        {
            var fileItems = new List<FileItem>();
            var entryMap = new Dictionary<string, ZipArchiveEntry>();
            string gitignoreContent = null;
            List<Regex> ignorePatterns = new List<Regex> { new Regex(@"\.git(/.*)?$", RegexOptions.IgnoreCase) }; // Always ignore .git

            try
            {
                using (var archive = ZipFile.OpenRead(zipPath))
                {
                    // First pass to find .gitignore
                    var gitignoreEntry = archive.Entries.FirstOrDefault(e => e.Name.Equals(".gitignore", StringComparison.OrdinalIgnoreCase) || e.FullName.Equals(".gitignore", StringComparison.OrdinalIgnoreCase));
                    if (gitignoreEntry != null)
                    {
                        try
                        {
                            using (var stream = gitignoreEntry.Open())
                            using (var reader = new StreamReader(stream, Encoding.UTF8, true))
                            {
                                gitignoreContent = await reader.ReadToEndAsync();
                                ignorePatterns.AddRange(ParseGitIgnore(gitignoreContent));
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error reading .gitignore from ZIP: {ex.Message}");
                        }
                    }

                    // Second pass to process files
                    foreach (var entry in archive.Entries)
                    {
                        // Skip directories and ignored files
                        if (!entry.FullName.EndsWith("/") && !entry.FullName.EndsWith("\\") && !IsIgnored(entry.FullName, ignorePatterns))
                        {
                            string relativePath = entry.FullName.Replace('\\', '/'); // Normalize separators
                            fileItems.Add(new FileItem
                            {
                                Name = Path.GetFileName(relativePath),
                                Path = relativePath, // Use relative path
                                DisplayPath = relativePath,
                                IsDirectory = false,
                                SourceUrl = entry.FullName, // Store entry name for lookup
                                SourceType = DataSourceType.LocalZip
                            });
                            entryMap[relativePath] = entry; // Map path to entry for later content reading
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading ZIP file {zipPath}: {ex.Message}");
                throw new IOException($"Failed to read ZIP file: {ex.Message}", ex);
            }
            return (fileItems, entryMap);
        }

        public static async Task<string> ReadZipEntryContentAsync(ZipArchiveEntry entry)
        {
            try
            {
                using (var stream = entry.Open())
                using (var reader = new StreamReader(stream, Encoding.UTF8, true)) // Detect encoding
                {
                    return await reader.ReadToEndAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading ZIP entry {entry.FullName}: {ex.Message}");
                return $"Error reading {entry.Name}: {ex.Message}";
            }
        }
    }
}