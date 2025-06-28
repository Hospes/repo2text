namespace Repo2Text
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Text.RegularExpressions;

    public static class LocalFileService
    {
        /// <summary>
        /// Converts a .gitignore glob pattern to a Regex.
        /// This is a simplified converter that handles *, ?, and [abc] character sets.
        /// It correctly avoids escaping glob wildcards, which was the flaw in the previous version.
        /// </summary>
        private static string GlobToRegex(string glob)
        {
            var regex = new StringBuilder();
            // This is a simple conversion. For full .gitignore spec, a more complex parser is needed.
            // This handles the most common cases.
            foreach (char c in glob)
            {
                switch (c)
                {
                    case '*':
                        regex.Append(".*");
                        break;
                    case '?':
                        regex.Append(".");
                        break;
                    // Characters that are special in Regex but not in globs
                    case '.':
                    case '(':
                    case ')':
                    case '+':
                    case '|':
                    case '^':
                    case '$':
                    case '#':
                        regex.Append('\\').Append(c);
                        break;
                    default:
                        regex.Append(c);
                        break;
                }
            }
            return regex.ToString();
        }

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
                    string pattern = line;
                    bool isRooted = pattern.StartsWith("/");
                    bool isNegated = pattern.StartsWith("!");

                    if (isNegated)
                    {
                        // Note: Negation is complex and not fully supported here.
                        // A proper implementation requires checking against all positive matches first.
                        // We will skip negated patterns for now to ensure correctness of positive matches.
                        continue;
                    }

                    if (isRooted)
                    {
                        pattern = pattern.Substring(1);
                    }

                    bool isDirectory = pattern.EndsWith("/");
                    if (isDirectory)
                    {
                        pattern = pattern.TrimEnd('/');
                    }

                    // *** THE CRITICAL FIX IS HERE ***
                    // We use our custom GlobToRegex converter instead of Regex.Escape
                    string regexPattern = GlobToRegex(pattern);

                    string finalRegex;
                    if (isRooted)
                    {
                        // Anchored to the start of the path
                        finalRegex = "^" + regexPattern;
                    }
                    else
                    {
                        // Can match anywhere in the path, either at the start or after a slash
                        finalRegex = "(^|/)" + regexPattern;
                    }

                    // If the pattern was for a directory OR it doesn't contain a slash (like 'obj'),
                    // it should match the directory and everything inside it.
                    if (isDirectory || !pattern.Contains('/'))
                    {
                        finalRegex += "($|/.*)";
                    }
                    else // If it's a file pattern like 'some/path/file.log'
                    {
                        finalRegex += "$"; // Must be an exact match
                    }

                    regexList.Add(new Regex(finalRegex, RegexOptions.IgnoreCase));
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
            // Normalize path separators for consistent matching.
            string normalizedPath = relativePath.Replace(Path.DirectorySeparatorChar, '/').TrimStart('/');
            return ignorePatterns.Any(regex => regex.IsMatch(normalizedPath));
        }

        // --- Directory Reading ---
        public static async Task<List<FileItem>> GetDirectoryFilesAsync(string rootPath)
        {
            var fileItems = new List<FileItem>();
            var gitignorePath = Path.Combine(rootPath, ".gitignore");
            string gitignoreContent = null;

            // Always ignore the .git directory itself.
            List<Regex> ignorePatterns = new List<Regex> { new Regex(@"(^|/)\.git($|/.*)", RegexOptions.IgnoreCase) };

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

            // Use an enumeration options to skip denied folders gracefully
            var enumerationOptions = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true,
            };

            var allFiles = Directory.EnumerateFiles(rootPath, "*", enumerationOptions);

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
                using (var reader = new StreamReader(fullPath, Encoding.UTF8, true))
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
            List<Regex> ignorePatterns = new List<Regex> { new Regex(@"(^|/)\.git($|/.*)", RegexOptions.IgnoreCase) };

            try
            {
                using (var archive = ZipFile.OpenRead(zipPath))
                {
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

                    foreach (var entry in archive.Entries)
                    {
                        if (!entry.FullName.EndsWith("/") && !entry.FullName.EndsWith("\\") && !IsIgnored(entry.FullName, ignorePatterns))
                        {
                            string relativePath = entry.FullName.Replace('\\', '/');
                            fileItems.Add(new FileItem
                            {
                                Name = Path.GetFileName(relativePath),
                                Path = relativePath,
                                DisplayPath = relativePath,
                                IsDirectory = false,
                                SourceUrl = entry.FullName,
                                SourceType = DataSourceType.LocalZip
                            });
                            entryMap[relativePath] = entry;
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
                using (var reader = new StreamReader(stream, Encoding.UTF8, true))
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