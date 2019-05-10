namespace Microsoft.RestApi.Splitters
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;
    using System.Linq;

    using Microsoft.DocAsCode.YamlSerialization;
    using Microsoft.OpenApi.Models;
    using Microsoft.RestApi.Common;
    using Microsoft.RestApi.Models;

    public static class SplitHelper
    {
        public static readonly string YamlExtension = ".yml";
        private static readonly Regex tocRegex = new Regex(@"^(?<headerLevel>#+)(( |\t)*)\[(?<tocTitle>.+)\]\((?<tocLink>(?!http[s]?://).*?)\)( |\t)*#*( |\t)*(\n|$)", RegexOptions.Compiled);
        public static readonly YamlSerializer YamlSerializer = new YamlSerializer();
       
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

        public static OpenApiDocument AggregateOpenApiTagsFromPaths(OpenApiDocument openApiDoc)
        {
            var tags = new List<OpenApiTag>(openApiDoc.Tags);
            foreach (var path in openApiDoc.Paths)
            {
                if (path.Value.Operations != null)
                {
                    foreach (var operation in path.Value.Operations)
                    {
                        if (operation.Value.Tags != null)
                        {
                            // only extract the first tag
                            var firstTag = operation.Value.Tags?.FirstOrDefault();
                            if (firstTag != null && !tags.Any(t => t.Name == firstTag.Name))
                            {
                                tags.Add(firstTag);
                            }
                        }
                    }
                }
            }
            openApiDoc.Tags = tags;
            return openApiDoc;
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
                            filteredRestPathOperation.Operations.Add(new OpenApiPathOperation
                            {
                                OpenApiPath = path,
                                Operation = operation
                            });
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
