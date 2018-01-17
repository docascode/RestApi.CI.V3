namespace Microsoft.RestApi.Splitters
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Microsoft.RestApi.Common;
    using Microsoft.RestApi.Model;
    using Microsoft.OpenApi.Readers;
    using Microsoft.OpenApi.Models;
    using Microsoft.RestApi.Transformers;
    using Microsoft.RestApi.Models;

    public class RestSplitter
    {
        private readonly string _sourceRootDir;
        private readonly string _targetRootDir;
        private readonly MappingFile _mappingFile;
        private readonly RestTransformerFactory _transformerFactory;

        protected const string TocFileName = "toc.md";
        protected static readonly Regex TocRegex = new Regex(@"^(?<headerLevel>#+)(( |\t)*)\[(?<tocTitle>.+)\]\((?<tocLink>(?!http[s]?://).*?)\)( |\t)*#*( |\t)*(\n|$)", RegexOptions.Compiled);

        public RestSplitter(string sourceRootDir, string targetRootDir, string mappingFilePath, RestTransformerFactory transformerFactory)
        {
            Guard.ArgumentNotNullOrEmpty(sourceRootDir, nameof(sourceRootDir));
            if (!Directory.Exists(sourceRootDir))
            {
                throw new ArgumentException($"{nameof(sourceRootDir)} '{sourceRootDir}' should exist.");
            }
            Guard.ArgumentNotNullOrEmpty(targetRootDir, nameof(targetRootDir));
            Guard.ArgumentNotNullOrEmpty(mappingFilePath, nameof(mappingFilePath));
            if (!File.Exists(mappingFilePath))
            {
                throw new ArgumentException($"mappingFilePath '{mappingFilePath}' should exist.");
            }

            _sourceRootDir = sourceRootDir;
            _targetRootDir = targetRootDir;
            _mappingFile = JsonUtility.ReadFromFile<MappingFile>(mappingFilePath).SortMappingFile();
            _transformerFactory = transformerFactory;
        }

        public void Process()
        {
            // Generate auto summary page
            GenerateAutoPage();

            // Write toc structure from MappingFile
            WriteToc();
        }


        private void GenerateAutoPage()
        {
            if (_mappingFile.ApisPageOptions == null || !_mappingFile.ApisPageOptions.EnableAutoGenerate)
            {
                return;
            }

            var targetIndexPath = Path.Combine(_targetRootDir, _mappingFile.ApisPageOptions.TargetFile);
            if (File.Exists(targetIndexPath))
            {
                Console.WriteLine($"Cleaning up previous existing {targetIndexPath}.");
                File.Delete(targetIndexPath);
            }

            using (var writer = new StreamWriter(targetIndexPath))
            {
                var summaryFile = Path.Combine(_targetRootDir, _mappingFile.ApisPageOptions.SummaryFile);
                if (File.Exists(summaryFile))
                {
                    foreach (var line in File.ReadAllLines(summaryFile))
                    {
                        writer.WriteLine(line);
                    }
                    writer.WriteLine();
                }

                writer.WriteLine("## All Product APIs");

                foreach (var orgInfo in _mappingFile.OrganizationInfos)
                {
                    // Org name as title
                    if (!string.IsNullOrWhiteSpace(orgInfo.OrganizationName))
                    {
                        writer.WriteLine($"### {orgInfo.OrganizationName}");
                        writer.WriteLine();
                    }

                    // Service table
                    if (orgInfo.Services.Count > 0)
                    {
                        writer.WriteLine("| Service | Description |");
                        writer.WriteLine("|---------|-------------|");
                        foreach (var service in orgInfo.Services)
                        {
                            if (string.IsNullOrWhiteSpace(service.IndexFile) && !File.Exists(service.IndexFile))
                            {
                                throw new InvalidOperationException($"Index file {service.IndexFile} of service {service.TocTitle} should exists.");
                            }
                            var summary = Utility.GetYamlHeaderByMeta(Path.Combine(_targetRootDir, service.IndexFile), _mappingFile.ApisPageOptions.ServiceDescriptionMetadata);
                            writer.WriteLine($"| [{service.TocTitle}](~/{service.IndexFile}) | {summary ?? string.Empty} |");
                        }
                        writer.WriteLine();
                    }
                }
            }
        }

        private void WriteToc()
        {
            var targetApiDir = SplitHelper.GetApiDirectory(_targetRootDir, _mappingFile.TargetApiRootDir);
            var targetTocPath = Path.Combine(targetApiDir, TocFileName);
           
            using (var writer = new StreamWriter(targetTocPath))
            {
                // Write auto generated apis page
                if (_mappingFile.ApisPageOptions?.EnableAutoGenerate == true)
                {
                    if (string.IsNullOrEmpty(_mappingFile.ApisPageOptions.TargetFile))
                    {
                        throw new InvalidOperationException("Target file of apis page options should not be null or empty.");
                    }
                    var targetIndexPath = Path.Combine(_targetRootDir, _mappingFile.ApisPageOptions.TargetFile);
                    writer.WriteLine($"# [{_mappingFile.ApisPageOptions.TocTitle}]({FileUtility.GetRelativePath(targetIndexPath, targetApiDir)})");
                }

                // Write organization info
                foreach (var orgInfo in _mappingFile.OrganizationInfos)
                {
                    // Deal with org name and index
                    var subTocPrefix = string.Empty;
                    if (!string.IsNullOrEmpty(orgInfo.OrganizationName))
                    {
                        // Write index
                        writer.WriteLine(!string.IsNullOrEmpty(orgInfo.OrganizationIndex)
                            ? $"# [{orgInfo.OrganizationName}]({SplitHelper.GenerateIndexHRef(_targetRootDir, orgInfo.OrganizationIndex, targetApiDir)})"
                            : $"# {orgInfo.OrganizationName}");
                        subTocPrefix = "#";
                    }
                    else if (_mappingFile.ApisPageOptions?.EnableAutoGenerate != true && !string.IsNullOrEmpty(orgInfo.DefaultTocTitle) && !string.IsNullOrEmpty(orgInfo.OrganizationIndex))
                    {
                        writer.WriteLine($"# [{orgInfo.DefaultTocTitle}]({SplitHelper.GenerateIndexHRef(_targetRootDir, orgInfo.OrganizationIndex, targetApiDir)})");
                    }

                    // Sort by service name
                    orgInfo.Services.Sort((a, b) => a.TocTitle.CompareTo(b.TocTitle));

                    // Write service info
                    foreach (var service in orgInfo.Services)
                    {
                        // 1. Top toc
                        Console.WriteLine($"Created conceptual toc item '{service.TocTitle}'");
                        writer.WriteLine(!string.IsNullOrEmpty(service.IndexFile)
                            ? $"{subTocPrefix}# [{service.TocTitle}]({SplitHelper.GenerateIndexHRef(_targetRootDir, service.IndexFile, targetApiDir)})"
                            : $"{subTocPrefix}# {service.TocTitle}");

                        // 2. Parse and split REST swaggers
                        var subTocDict = new SortedDictionary<string, List<SwaggerToc>>();
                        if (service.SwaggerInfo != null)
                        {
                            subTocDict = SplitSwaggers(targetApiDir, service);
                        }

                        // 3. Conceptual toc
                        List<string> tocLines = null;
                        if (!string.IsNullOrEmpty(service.TocFile))
                        {
                            tocLines = SplitHelper.GenerateDocTocItems(_targetRootDir, service.TocFile, targetApiDir).Where(i => !string.IsNullOrEmpty(i)).ToList();
                            if (tocLines.Any())
                            {
                                foreach (var tocLine in tocLines)
                                {
                                    // Insert one heading before to make it sub toc
                                    writer.WriteLine($"{subTocPrefix}#{tocLine}");
                                }
                                Console.WriteLine($"-- Created sub referenced toc items under conceptual toc item '{service.TocTitle}'");
                            }
                        }

                        // 4. Write REST toc
                        if (service.SwaggerInfo != null)
                        {
                            var subRefTocPrefix = string.Empty;
                            if (tocLines != null && tocLines.Count > 0)
                            {
                                subRefTocPrefix = SplitHelper.IncreaseSharpCharacter(subRefTocPrefix);
                                writer.WriteLine($"{subTocPrefix}#{subRefTocPrefix} Reference");
                            }

                            foreach (var pair in subTocDict)
                            {
                                var subGroupTocPrefix = subRefTocPrefix;
                                if (!string.IsNullOrEmpty(pair.Key))
                                {
                                    subGroupTocPrefix = SplitHelper.IncreaseSharpCharacter(subRefTocPrefix);
                                    writer.WriteLine($"{subTocPrefix}#{subGroupTocPrefix} {pair.Key}");
                                }
                                var subTocList = pair.Value;
                                subTocList.Sort((x, y) => string.CompareOrdinal(x.Title, y.Title));
                                foreach (var subToc in subTocList)
                                {
                                    writer.WriteLine($"{subTocPrefix}##{subGroupTocPrefix} [{subToc.Title}]({subToc.FilePath})");
                                    if (subToc.ChildrenToc.Count > 0)
                                    {
                                        foreach (var child in subToc.ChildrenToc)
                                        {
                                            writer.WriteLine($"{subTocPrefix}###{subGroupTocPrefix} [{child.Title}]({child.FilePath})");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private SortedDictionary<string, List<SwaggerToc>> SplitSwaggers(string targetApiDir, ServiceInfo service)
        {
            var subTocDict = new SortedDictionary<string, List<SwaggerToc>>();

            foreach (var swagger in service.SwaggerInfo)
            {
                var targetDir = FileUtility.CreateDirectoryIfNotExist(Path.Combine(targetApiDir, service.UrlGroup));
                var sourceFile = Path.Combine(_sourceRootDir, swagger.Source.TrimEnd());

                if (!File.Exists(sourceFile))
                {
                    throw new ArgumentException($"{nameof(sourceFile)} '{sourceFile}' should exist.");
                }

                var restFileInfo = SplitSwaggersByTag(targetDir, sourceFile, service.TocTitle, swagger.OperationGroupMapping, _mappingFile);
               
                var tocTitle = Utility.ExtractPascalNameByRegex(restFileInfo.TocTitle);
                var subGroupName = swagger.SubGroupTocTitle ?? string.Empty;
                List<SwaggerToc> subTocList;
                if (!subTocDict.TryGetValue(subGroupName, out subTocList))
                {
                    subTocList = new List<SwaggerToc>();
                    subTocDict.Add(subGroupName, subTocList);
                }

                foreach (var fileNameInfo in restFileInfo.FileNameInfos)
                {
                    var subTocTitle = fileNameInfo.TocName;
                    var filePath = FileUtility.NormalizePath(Path.Combine(service.UrlGroup, fileNameInfo.FileName));

                    if (subTocList.Any(toc => toc.Title == subTocTitle))
                    {
                        throw new InvalidOperationException($"Sub toc '{subTocTitle}' under '{tocTitle}' has been added into toc.md, please add operation group name mapping for file '{swagger.Source}' to avoid conflicting");
                    }

                    var childrenToc = new List<SwaggerToc>();
                    if (fileNameInfo.ChildrenFileNameInfo != null && fileNameInfo.ChildrenFileNameInfo.Count > 0)
                    {
                        foreach (var nameInfo in fileNameInfo.ChildrenFileNameInfo)
                        {
                            childrenToc.Add(new SwaggerToc(nameInfo.TocName, FileUtility.NormalizePath(Path.Combine(service.UrlGroup, nameInfo.FileName))));
                        }
                    }

                    subTocList.Add(new SwaggerToc(subTocTitle, filePath, childrenToc));
                }
                Console.WriteLine($"Done splitting swagger file from '{swagger.Source}' to '{service.UrlGroup}'");
            }

            return subTocDict;
        }

        private RestFileInfo SplitSwaggersByTag(string targetDir, string filePath, string serviceName, OperationGroupMapping operationGroupMapping, MappingFile mappingFile)
        {
            var restFileInfo = new RestFileInfo();
            if (!Directory.Exists(targetDir))
            {
                throw new ArgumentException($"{nameof(targetDir)} '{targetDir}' should exist.");
            }
            if (!File.Exists(filePath))
            {
                throw new ArgumentException($"{nameof(filePath)} '{filePath}' should exist.");
            }

            using (var streamReader = File.OpenText(filePath))
            {
                var openApiDoc = new OpenApiStreamReader().Read(streamReader.BaseStream, out var context);
                if (context.Errors?.Count() > 0)
                {
                    foreach (var error in context?.Errors)
                    {
                        Console.WriteLine(JsonUtility.ToJsonString(error));
                    }
                    throw new Exception(JsonUtility.ToIndentedJsonString(context.Errors));
                }
                var fileNameInfos = SplitOperationGroups(targetDir, filePath, openApiDoc, serviceName, operationGroupMapping, mappingFile);
                if (fileNameInfos.Any())
                {
                    restFileInfo.FileNameInfos = fileNameInfos.ToList();
                }
                restFileInfo.TocTitle = openApiDoc.Info?.Title;
            }
            return restFileInfo;
        }

        private OpenApiDocument ExtractOpenApiTagsFromPaths(OpenApiDocument openApiDoc)
        {
            var tags = new List<OpenApiTag>(openApiDoc.Tags);
            foreach(var path in openApiDoc.Paths)
            {
                if (path.Value.Operations != null)
                {
                    foreach (var operation in path.Value.Operations)
                    {
                        if(operation.Value.Tags != null)
                        {
                            foreach (var tag in operation.Value.Tags)
                            {
                                if(!tags.Any(t => t.Name == tag.Name))
                                {
                                    tags.Add(tag);
                                }
                            }
                        }
                    }
                }
            }
            openApiDoc.Tags = tags;
            return openApiDoc;
        }

        private IEnumerable<FileNameInfo> SplitOperationGroups(string targetDir, string filePath, OpenApiDocument openApiDoc, string serviceName, OperationGroupMapping operationGroupMapping, MappingFile mappingFile)
        {
            openApiDoc = ExtractOpenApiTagsFromPaths(openApiDoc);
            if (openApiDoc.Tags == null || openApiDoc.Tags.Count == 0)
            {
                Console.WriteLine($"tags is null or empty for file {filePath}.");
            }

            foreach (var tag in openApiDoc.Tags)
            {
                var filteredOperations = SplitHelper.FindOperationsByTag(openApiDoc.Paths, tag);
                if (filteredOperations.Count > 0)
                {
                    var fileNameInfo = new FileNameInfo
                    {
                        TocName = Utility.ExtractPascalNameByRegex(tag.Name)
                    };

                    // Get file name from operation group mapping
                    string newTagName;
                    if (operationGroupMapping != null && operationGroupMapping.TryGetValue(tag.Name, out newTagName))
                    {
                        fileNameInfo.TocName = newTagName;
                    }
                    else
                    {
                        newTagName = tag.Name;
                    }

                    // Split operation group to operation
                    fileNameInfo.ChildrenFileNameInfo = new List<FileNameInfo>(SplitOperations(filteredOperations, openApiDoc, serviceName, fileNameInfo.TocName, targetDir, newTagName));
                    // Sort
                    fileNameInfo.ChildrenFileNameInfo.Sort((a, b) => string.CompareOrdinal(a.TocName, b.TocName));

                    fileNameInfo.FileName = $"{newTagName}.yml";

                    var model = new TransformModel
                    {
                        OpenApiDoc = openApiDoc,
                        OpenApiTag = tag,
                        ServiceName = serviceName,
                        GroupName = fileNameInfo.TocName
                    };
                    _transformerFactory?.TransformerOperationGroup(model, targetDir, fileNameInfo.FileName);

                    yield return fileNameInfo;
                }
            }
        }

        protected IEnumerable<FileNameInfo> SplitOperations(List<KeyValuePair<OperationType, OpenApiOperation>> operations, OpenApiDocument openApiDoc, string serviceName, string groupName, string targetDir, string tag)
        {
            foreach (var operation in operations)
            {
                var operationName = operation.Value.OperationId;
                var fileNameInfo = new FileNameInfo
                {
                    TocName = Utility.ExtractPascalNameByRegex(operationName),
                    FileName = Path.Combine(tag, $"{operationName}.yml")
                };
                if (!Directory.Exists(Path.Combine(targetDir, tag)))
                {
                    Directory.CreateDirectory(Path.Combine(targetDir, tag));
                }

                var model = new TransformModel
                {
                    OpenApiDoc = openApiDoc,
                    Operation = operation,
                    ServiceName = serviceName,
                    GroupName = groupName,
                    OperationName = fileNameInfo.TocName,
                };
                _transformerFactory?.TransformerOperation(model, targetDir, fileNameInfo.FileName);
                yield return fileNameInfo;
            }
        }
    }
}
