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

        public static string GetOutputDirectory(string outputRootDir)
        {
            Guard.ArgumentNotNullOrEmpty(outputRootDir, nameof(outputRootDir));

            if (Directory.Exists(outputRootDir))
            {
                // Clear last built output folder
                Directory.Delete(outputRootDir, true);
                Console.WriteLine($"Done cleaning previous existing {outputRootDir}");
            }
            Directory.CreateDirectory(outputRootDir);
            if (!outputRootDir.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                outputRootDir = outputRootDir + Path.DirectorySeparatorChar;
            }
            return outputRootDir;
        }

        public static string GenerateIndexHRef(string targetRootDir, string indexRelativePath, string targetApiVersionDir)
        {
            Guard.ArgumentNotNullOrEmpty(targetRootDir, nameof(targetRootDir));
            Guard.ArgumentNotNullOrEmpty(indexRelativePath, nameof(indexRelativePath));
            Guard.ArgumentNotNullOrEmpty(targetApiVersionDir, nameof(targetApiVersionDir));

            var indexPath = Path.Combine(targetRootDir, indexRelativePath);
            if (!File.Exists(indexPath))
            {
                throw new FileNotFoundException($"Index file '{indexPath}' not exists.");
            }
            return FileUtility.GetRelativePath(indexPath, targetApiVersionDir);
        }

        public static string GenerateHref(string targetRootDir, string relativePath, string targetApiVersionDir)
        {
            Guard.ArgumentNotNullOrEmpty(targetRootDir, nameof(targetRootDir));
            Guard.ArgumentNotNullOrEmpty(relativePath, nameof(relativePath));
            Guard.ArgumentNotNullOrEmpty(targetApiVersionDir, nameof(targetApiVersionDir));

            var indexPath = Path.Combine(targetRootDir, relativePath);
            if (!File.Exists(indexPath))
            {
                return null;
            }
            return FileUtility.GetRelativePath(indexPath, targetApiVersionDir);
        }

        public static IEnumerable<string> GenerateDocTocItems(string targetRootDir, string tocRelativePath, string targetApiVersionDir)
        {
            Guard.ArgumentNotNullOrEmpty(targetRootDir, nameof(targetRootDir));
            Guard.ArgumentNotNullOrEmpty(tocRelativePath, nameof(tocRelativePath));
            Guard.ArgumentNotNullOrEmpty(targetApiVersionDir, nameof(targetApiVersionDir));

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
            var tocRelativeDirectoryToApi = FileUtility.GetRelativePath(Path.GetDirectoryName(tocPath), targetApiVersionDir);

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
                        var linkPath = Path.Combine(targetApiVersionDir, tocLinkRelativePath);
                        if (!File.Exists(linkPath))
                        {
                            throw new FileNotFoundException($"Link '{tocLinkRelativePath}' not exist in '{tocRelativePath}', when merging into '{tocFileName}' of '{targetApiVersionDir}'");
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

        public static FilteredOpenApiPath FindOperationsByTag(OpenApiPaths openApiPaths, OpenApiTag tag)
        {
            var filteredRestPathOperation = new FilteredOpenApiPath();
            foreach (var path in openApiPaths)
            {
                foreach(var operation in path.Value.Operations)
                {
                    var firstTag = operation.Value.Tags?.FirstOrDefault();
                    if (firstTag != null)
                    {
                        if (firstTag.Name == tag.Name)
                        {
                            filteredRestPathOperation.OpenApiPath = path;
                            filteredRestPathOperation.Operations.Add(operation);
                            foreach (var etag in operation.Value.Tags)
                            {
                                if (etag.Name != firstTag.Name && !filteredRestPathOperation.ExtendTagNames.Any(t => t == etag.Name))
                                {
                                    filteredRestPathOperation.ExtendTagNames.Add(etag.Name);
                                }
                            }
                        }
                    }
                }
            }
            return filteredRestPathOperation;
        }
    }
}
