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

        public static OpenApiDocument AggregateOpenApiTagsFromPaths(OpenApiDocument openApiDoc, MappingFile mapping, string sourceFilePath)
        {
            var tags = new List<OpenApiTag>();
            foreach (var path in openApiDoc.Paths)
            {
                if (path.Value.Operations != null)
                {
                    foreach (var operation in path.Value.Operations)
                    {
                        if (mapping.IsGroupdedByTag)
                        {
                            if (operation.Value.Tags == null || !operation.Value.Tags.Any())
                            {
                                throw new Exception($"tags is null or empty for file {sourceFilePath}.");
                            }
                            var firstTag = operation.Value.Tags?.FirstOrDefault();

                            if (!tags.Any(t => t.Name == firstTag.Name))
                            {
                                tags.Add(firstTag);
                            }

                            if (mapping.RemoveTagFromOperationId && operation.Value.OperationId.StartsWith(firstTag.Name))
                            {
                                operation.Value.OperationId = operation.Value.OperationId.TrimStart(firstTag.Name.ToCharArray())
                                    .Trim('_').Trim(' ');
                            }
                        }
                        else
                        {
                            var idResult = GetOperationGroupFromOperationId(operation.Value.OperationId);
                            if(!tags.Any(t => t.Name == idResult.Item1))
                            {
                                tags.Add(new OpenApiTag() { Name = idResult.Item1 });
                            }
                            operation.Value.OperationId = idResult.Item2;
                        }
                    }
                }
            }
            openApiDoc.Tags = tags;
            return openApiDoc;
        }

        private static Tuple<string, string> GetOperationGroupFromOperationId(string operationId)
        {
            var result = operationId.Split('_');
            if (result.Length < 2)
            {
                // When the operation id doesn't contain '_', treat the whole operation id as Noun and Verb at the same time
                return Tuple.Create(result[0], result[0]);
            }
            if (result.Length > 2)
            {
                throw new InvalidOperationException($"Invalid operation id: {operationId}, it should be Noun_Verb format.");
            }
            return Tuple.Create(result[0], result[1]);
        }

        public static FilteredOpenApiPath FindOperationsByTag(OpenApiPaths openApiPaths, OpenApiTag tag)
        {
            var filteredRestPathOperation = new FilteredOpenApiPath();
            foreach (var path in openApiPaths)
            {
                foreach (var operation in path.Value.Operations)
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
