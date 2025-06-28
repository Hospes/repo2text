using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Repo2Text
{
    public static class DependencyResolver
    {
        // Regex to find 'using' statements. Catches "using System.Text;" and "using static System.Console;"
        private static readonly Regex CSharpUsingRegex = new Regex(@"^\s*using\s+(static\s+)?([A-Za-z0-9\._]+);", RegexOptions.Multiline);

        // Regex to find 'namespace' declarations.
        private static readonly Regex CSharpNamespaceRegex = new Regex(@"^\s*namespace\s+([A-Za-z0-9\._]+)", RegexOptions.Multiline);

        // Common top-level namespaces to ignore (System, third-party libs, etc.)
        private static readonly HashSet<string> s_ignoredNamespaces = new HashSet<string>
        {
            "System", "Microsoft", "Windows", "Octokit", "SharpToken", "ReSharper", "JetBrains"
        };

        /// <summary>
        /// Finds all dependencies for a given root file within a list of all project files.
        /// </summary>
        /// <param name="rootFile">The starting file for the analysis.</param>
        /// <param name="allProjectFiles">A complete list of all files in the project to search through.</param>
        /// <param name="contentFetcher">A function that can asynchronously retrieve the content of any file.</param>
        /// <param name="setStatus">An action to report progress.</param>
        /// <returns>A HashSet of FileItems that includes the root file and all its dependencies.</returns>
        public static async Task<HashSet<FileItem>> FindDependenciesAsync(
            FileItem rootFile,
            IReadOnlyList<FileItem> allProjectFiles,
            Func<FileItem, Task<string>> contentFetcher,
            Action<string> setStatus)
        {
            var fileExtension = Path.GetExtension(rootFile.Name)?.ToLowerInvariant();
            if (fileExtension != ".cs")
            {
                setStatus("Dependency analysis is only supported for C# (.cs) files.");
                return new HashSet<FileItem> { rootFile };
            }

            setStatus("Analyzing C# dependencies...");
            var csFiles = allProjectFiles.Where(f => Path.GetExtension(f.Name).Equals(".cs", StringComparison.OrdinalIgnoreCase)).ToList();

            // Cache for namespace definitions: namespace -> list of files defining it
            var namespaceToFileMap = new Dictionary<string, List<FileItem>>();
            // Cache for file contents to avoid re-fetching
            var contentCache = new Dictionary<string, string>();

            // Pre-scan all C# files to build the namespace map. This is the most intensive part.
            setStatus($"Building namespace map from {csFiles.Count} C# files...");
            foreach (var file in csFiles)
            {
                var content = await GetFileContent(file, contentFetcher, contentCache);
                var namespaces = ParseNamespaces(content);
                foreach (var ns in namespaces)
                {
                    if (!namespaceToFileMap.ContainsKey(ns))
                    {
                        namespaceToFileMap[ns] = new List<FileItem>();
                    }
                    namespaceToFileMap[ns].Add(file);
                }
            }

            setStatus("Traversing dependency graph...");
            var resolvedFiles = new HashSet<FileItem>();
            var filesToProcess = new Queue<FileItem>();

            filesToProcess.Enqueue(rootFile);
            resolvedFiles.Add(rootFile);

            while (filesToProcess.Count > 0)
            {
                var currentFile = filesToProcess.Dequeue();
                var content = await GetFileContent(currentFile, contentFetcher, contentCache);
                var usingStatements = ParseUsings(content);

                foreach (var usingNamespace in usingStatements)
                {
                    // Find files that declare this namespace or a sub-namespace
                    foreach (var (definedNamespace, files) in namespaceToFileMap)
                    {
                        if (definedNamespace.Equals(usingNamespace) || definedNamespace.StartsWith(usingNamespace + "."))
                        {
                            foreach (var dependencyFile in files)
                            {
                                if (resolvedFiles.Add(dependencyFile)) // If added successfully (was not present)
                                {
                                    filesToProcess.Enqueue(dependencyFile);
                                }
                            }
                        }
                    }
                }
            }

            setStatus($"Analysis complete. Found {resolvedFiles.Count} dependent files.");
            return resolvedFiles;
        }

        private static async Task<string> GetFileContent(FileItem file, Func<FileItem, Task<string>> contentFetcher, Dictionary<string, string> cache)
        {
            if (cache.TryGetValue(file.Path, out var cachedContent))
            {
                return cachedContent;
            }

            var content = await contentFetcher(file);
            cache[file.Path] = content;
            return content;
        }

        private static IEnumerable<string> ParseUsings(string content)
        {
            var matches = CSharpUsingRegex.Matches(content);
            var usings = new HashSet<string>();
            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    string ns = match.Groups[2].Value;
                    string topLevel = ns.Split('.').FirstOrDefault();
                    if (!string.IsNullOrEmpty(topLevel) && !s_ignoredNamespaces.Contains(topLevel))
                    {
                        usings.Add(ns);
                    }
                }
            }
            return usings;
        }

        private static IEnumerable<string> ParseNamespaces(string content)
        {
            var matches = CSharpNamespaceRegex.Matches(content);
            var namespaces = new HashSet<string>();
            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    namespaces.Add(match.Groups[1].Value);
                }
            }
            return namespaces;
        }
    }
}