namespace Microsoft.RestApi.Splitters
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;

    using Microsoft.RestApi.Common;
    using Microsoft.RestApi.Models;
    using Microsoft.OpenApi.Models;
    using System.Linq;

    public static class SplitHelper
    {
        private static string tocFileName = "toc.md";
        private static readonly Regex tocRegex = new Regex(@"^(?<headerLevel>#+)(( |\t)*)\[(?<tocTitle>.+)\]\((?<tocLink>(?!http[s]?://).*?)\)( |\t)*#*( |\t)*(\n|$)", RegexOptions.Compiled);

        // Sort by org and service name
        public static MappingFile SortMappingFile(this MappingFile mappingFile)
        {
            Guard.ArgumentNotNull(mappingFile, nameof(mappingFile));
            Guard.ArgumentNotNull(mappingFile.OrganizationInfos, nameof(mappingFile.OrganizationInfos));

            mappingFile.OrganizationInfos.Sort((x, y) => string.CompareOrdinal(x.OrganizationName, y.OrganizationName));
            foreach (var organizationInfo in mappingFile.OrganizationInfos)
            {
                organizationInfo.Services?.Sort((x, y) => string.CompareOrdinal(x.TocTitle, y.TocTitle));
            }
            return mappingFile;
        }

        public static string GetApiDirectory(string rootDirectory, string targetApiRootDir)
        {
            Guard.ArgumentNotNullOrEmpty(rootDirectory, nameof(rootDirectory));
            Guard.ArgumentNotNullOrEmpty(targetApiRootDir, nameof(targetApiRootDir));

            var targetApiDir = Path.Combine(rootDirectory, targetApiRootDir);
            if (Directory.Exists(targetApiDir))
            {
                // Clear last built target api folder
                Directory.Delete(targetApiDir, true);
                Console.WriteLine($"Done cleaning previous existing {targetApiDir}");
            }
            Directory.CreateDirectory(targetApiDir);
            if (!targetApiDir.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                targetApiDir = targetApiDir + Path.DirectorySeparatorChar;
            }
            return targetApiDir;
        }

        public static string GenerateIndexHRef(string targetRootDir, string indexRelativePath, string targetApiDir)
        {
            Guard.ArgumentNotNullOrEmpty(targetRootDir, nameof(targetRootDir));
            Guard.ArgumentNotNullOrEmpty(indexRelativePath, nameof(indexRelativePath));
            Guard.ArgumentNotNullOrEmpty(targetApiDir, nameof(targetApiDir));

            var indexPath = Path.Combine(targetRootDir, indexRelativePath);
            if (!File.Exists(indexPath))
            {
                throw new FileNotFoundException($"Index file '{indexPath}' not exists.");
            }
            return FileUtility.GetRelativePath(indexPath, targetApiDir);
        }

        public static IEnumerable<string> GenerateDocTocItems(string targetRootDir, string tocRelativePath, string targetApiDir)
        {
            Guard.ArgumentNotNullOrEmpty(targetRootDir, nameof(targetRootDir));
            Guard.ArgumentNotNullOrEmpty(tocRelativePath, nameof(tocRelativePath));
            Guard.ArgumentNotNullOrEmpty(targetApiDir, nameof(targetApiDir));

            var tocPath = Path.Combine(targetRootDir, tocRelativePath);
            if (!File.Exists(tocPath))
            {
                throw new FileNotFoundException($"Toc file '{tocRelativePath}' not exists.");
            }
            var fileName = Path.GetFileName(tocPath);
            if (!fileName.Equals(tocFileName, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Currently only '{tocFileName}' is supported as conceptual toc, please update the toc path '{tocRelativePath}'.");
            }
            var tocRelativeDirectoryToApi = FileUtility.GetRelativePath(Path.GetDirectoryName(tocPath), targetApiDir);

            foreach (var tocLine in File.ReadLines(tocPath))
            {
                var match = tocRegex.Match(tocLine);
                if (match.Success)
                {
                    var tocLink = match.Groups["tocLink"].Value;
                    if (string.IsNullOrEmpty(tocLink))
                    {
                        // Handle case like [Text]()
                        yield return tocLine;
                    }
                    else
                    {
                        var tocTitle = match.Groups["tocTitle"].Value;
                        var headerLevel = match.Groups["headerLevel"].Value.Length;
                        var tocLinkRelativePath = tocRelativeDirectoryToApi + "/" + tocLink;
                        var linkPath = Path.Combine(targetApiDir, tocLinkRelativePath);
                        if (!File.Exists(linkPath))
                        {
                            throw new FileNotFoundException($"Link '{tocLinkRelativePath}' not exist in '{tocRelativePath}', when merging into '{tocFileName}' of '{targetApiDir}'");
                        }
                        yield return $"{new string('#', headerLevel)} [{tocTitle}]({tocLinkRelativePath})";
                    }
                }
                else
                {
                    yield return tocLine;
                }
            }
        }

        public static string IncreaseSharpCharacter(string str)
        {
            Guard.ArgumentNotNull(str, nameof(str));
            return str + "#";
        }

        public static List<KeyValuePair<string, KeyValuePair<OperationType, OpenApiOperation>>> FindOperationsByTag(OpenApiPaths openApiPaths, OpenApiTag tag)
        {
            var pathAndOperations = new List<KeyValuePair<string, KeyValuePair<OperationType, OpenApiOperation>>>();
            foreach (var path in openApiPaths)
            {
                foreach(var operation in path.Value.Operations)
                {
                    if (operation.Value.Tags?.Any(t => t.Name == tag.Name) == true)
                    {
                        pathAndOperations.Add(new KeyValuePair<string, KeyValuePair<OperationType, OpenApiOperation>>(path.Key, operation));
                    }
                }
            }
            return pathAndOperations;
        }
    }
}
