﻿namespace Microsoft.RestApi.Splitters
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Microsoft.RestApi.Common;
    using Microsoft.RestApi.Models;
    using Microsoft.OpenApi.Readers;
    using Microsoft.OpenApi.Models;
    using Microsoft.RestApi.Transformers;

    public class RestSplitter
    {
        protected static readonly string ComponentGroupName = "components";
        protected const string TocFileName = "toc.md";
        protected static readonly Regex TocRegex = new Regex(@"^(?<headerLevel>#+)(( |\t)*)\[(?<tocTitle>.+)\]\((?<tocLink>(?!http[s]?://).*?)\)( |\t)*#*( |\t)*(\n|$)", RegexOptions.Compiled);

        public string SourceRootDir { get; }
        public string TargetRootDir { get; }
        public string OutputDir { get; }
        public MappingFile MappingFile { get; }
        public RestTransformerFactory TransformerFactory { get; }

        public IList<string> Errors { get; set; }

        public RestSplitter(string sourceRootDir, string targetRootDir, string mappingFilePath, string outputDir, RestTransformerFactory transformerFactory)
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
            Errors = new List<string>();
            SourceRootDir = sourceRootDir;
            TargetRootDir = targetRootDir;
            OutputDir = outputDir;
            MappingFile = JsonUtility.ReadFromFile<MappingFile>(mappingFilePath).SortMappingFile();
            TransformerFactory = transformerFactory;
        }

        public void Process()
        {
            // Generate auto summary page
            GenerateAutoPage();

            // Write toc structure from MappingFile
            WriteToc();
        }

        public virtual void WriteRestToc(StreamWriter writer, string subTocPrefix, List<string> tocLines, SortedDictionary<string, List<SwaggerToc>> subTocDict, string targetApiVersionDir)
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
                subTocList.OrderBy(x => !Equals(x.Title, "components")).ToList().Sort((x, y) => string.CompareOrdinal(x.Title, y.Title));
                foreach (var subToc in subTocList)
                {
                    writer.WriteLine($"{subTocPrefix}##{subGroupTocPrefix} [{subToc.Title}](xref:{subToc.Uid})");
                    if (subToc.ChildrenToc.Count > 0)
                    {
                        foreach (var child in subToc.ChildrenToc)
                        {
                            writer.WriteLine($"{subTocPrefix}###{subGroupTocPrefix} [{child.Title}](xref:{child.Uid})");
                        }
                    }
                }
            }
        }

        private void GenerateAutoPage()
        {
            if (MappingFile.ApisPageOptions == null || !MappingFile.ApisPageOptions.EnableAutoGenerate)
            {
                return;
            }

            var targetIndexPath = Path.Combine(TargetRootDir, MappingFile.ApisPageOptions.TargetFile);
            if (File.Exists(targetIndexPath))
            {
                Console.WriteLine($"Cleaning up previous existing {targetIndexPath}.");
                File.Delete(targetIndexPath);
            }

            using (var writer = new StreamWriter(targetIndexPath))
            {
                var summaryFile = Path.Combine(TargetRootDir, MappingFile.ApisPageOptions.SummaryFile);
                if (File.Exists(summaryFile))
                {
                    foreach (var line in File.ReadAllLines(summaryFile))
                    {
                        writer.WriteLine(line);
                    }
                    writer.WriteLine();
                }

                writer.WriteLine("## All Product APIs");

                foreach (var orgInfo in MappingFile.OrganizationInfos)
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
                            var summary = Utility.GetYamlHeaderByMeta(Path.Combine(TargetRootDir, service.IndexFile), MappingFile.ApisPageOptions.ServiceDescriptionMetadata);
                            writer.WriteLine($"| [{service.TocTitle}](~/{service.IndexFile}) | {summary ?? string.Empty} |");
                        }
                        writer.WriteLine();
                    }
                }
            }
        }

        private void WriteToc()
        {
            var targetApiDir = SplitHelper.GetOutputDirectory(OutputDir);
            foreach (var version in MappingFile.VersionList)
            {
                var targetApiVersionDir = Path.Combine(targetApiDir, version);
                Directory.CreateDirectory(targetApiVersionDir);
                if (!targetApiVersionDir.EndsWith(Path.DirectorySeparatorChar.ToString()))
                {
                    targetApiVersionDir = targetApiVersionDir + Path.DirectorySeparatorChar;
                }
                var targetTocPath = Path.Combine(targetApiVersionDir, TocFileName);

                using (var writer = new StreamWriter(targetTocPath))
                {
                    // Write auto generated apis page
                    if (MappingFile.ApisPageOptions?.EnableAutoGenerate == true)
                    {
                        if (string.IsNullOrEmpty(MappingFile.ApisPageOptions.TargetFile))
                        {
                            throw new InvalidOperationException("Target file of apis page options should not be null or empty.");
                        }
                        var targetIndexPath = Path.Combine(TargetRootDir, MappingFile.ApisPageOptions.TargetFile);
                        writer.WriteLine($"# [{MappingFile.ApisPageOptions.TocTitle}]({FileUtility.GetRelativePath(targetIndexPath, targetApiVersionDir)})");
                    }

                    // Write organization info
                    foreach (var orgInfo in MappingFile.OrganizationInfos)
                    {
                        // Deal with org name and index
                        var subTocPrefix = string.Empty;
                        if (!string.IsNullOrEmpty(orgInfo.OrganizationName))
                        {
                            // Write index
                            writer.WriteLine(!string.IsNullOrEmpty(orgInfo.OrganizationIndex)
                                ? $"# [{orgInfo.OrganizationName}]({SplitHelper.GenerateIndexHRef(TargetRootDir, orgInfo.OrganizationIndex, targetApiVersionDir)})"
                                : $"# {orgInfo.OrganizationName}");
                            subTocPrefix = "#";
                        }
                        else if (MappingFile.ApisPageOptions?.EnableAutoGenerate != true && !string.IsNullOrEmpty(orgInfo.DefaultTocTitle) && !string.IsNullOrEmpty(orgInfo.OrganizationIndex))
                        {
                            writer.WriteLine($"# [{orgInfo.DefaultTocTitle}]({SplitHelper.GenerateIndexHRef(TargetRootDir, orgInfo.OrganizationIndex, targetApiVersionDir)})");
                        }

                        // Sort by service name
                        orgInfo.Services.Sort((a, b) => a.TocTitle.CompareTo(b.TocTitle));

                        // Write service info
                        foreach (var service in orgInfo.Services)
                        {
                            // 1. Top toc
                            Console.WriteLine($"Created conceptual toc item '{service.TocTitle}'");
                            writer.WriteLine(!string.IsNullOrEmpty(service.IndexFile)
                                ? $"{subTocPrefix}# [{service.TocTitle}]({SplitHelper.GenerateIndexHRef(TargetRootDir, service.IndexFile, targetApiVersionDir)})"
                                : $"{subTocPrefix}# {service.TocTitle}");

                            // 2. Parse and split REST swaggers
                            var subTocDict = new SortedDictionary<string, List<SwaggerToc>>();
                            if (service.SwaggerInfo != null)
                            {
                                subTocDict = SplitSwaggers(targetApiVersionDir, service, version);
                            }

                            // 3. Conceptual toc
                            List<string> tocLines = null;
                            if (!string.IsNullOrEmpty(service.TocFile))
                            {
                                tocLines = SplitHelper.GenerateDocTocItems(TargetRootDir, service.TocFile, targetApiVersionDir).Where(i => !string.IsNullOrEmpty(i)).ToList();
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
                                WriteRestToc(writer, subTocPrefix, tocLines, subTocDict, targetApiVersionDir);
                            }
                        }
                    }
                }

                TocConverter.Convert(targetTocPath);
                if (File.Exists(targetTocPath))
                {
                    File.Delete(targetTocPath);
                }
            }
        }

        private SortedDictionary<string, List<SwaggerToc>> SplitSwaggers(string targetApiVersionDir, ServiceInfo service, string version)
        {
            var subTocDict = new SortedDictionary<string, List<SwaggerToc>>();

            foreach (var swagger in service.SwaggerInfo)
            {
                if (string.Equals(swagger.Version, version))
                {
                    var targetDir = FileUtility.CreateDirectoryIfNotExist(Path.Combine(targetApiVersionDir, service.UrlGroup));
                    var sourceFile = Path.Combine(SourceRootDir, swagger.Source.TrimEnd());

                    if (!File.Exists(sourceFile))
                    {
                        throw new ArgumentException($"{nameof(sourceFile)} '{sourceFile}' should exist.");
                    }

                    var restFileInfo = SplitSwaggerByTag(targetDir, sourceFile, service.Name, swagger.OperationGroupMapping, MappingFile);

                    var tocTitle = restFileInfo.TocTitle;
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
                                childrenToc.Add(new SwaggerToc(nameInfo.TocName, nameInfo.FileName, nameInfo.FileId));
                            }
                        }

                        subTocList.Add(new SwaggerToc(subTocTitle, filePath, fileNameInfo.FileId, childrenToc, fileNameInfo.IsComponentGroup));
                    }
                    Console.WriteLine($"Done splitting swagger file from '{swagger.Source}' to '{service.UrlGroup}'");
                }
            }

            return subTocDict;
        }

        private RestFileInfo SplitSwaggerByTag(string targetDir, string filePath, string serviceName, OperationGroupMapping operationGroupMapping, MappingFile mappingFile)
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

                var componentsFileNameInfo = GenerateComponents(targetDir, openApiDoc, serviceName);
                componentsFileNameInfo.IsComponentGroup = true;
                restFileInfo.FileNameInfos.Add(componentsFileNameInfo);
            }
            return restFileInfo;
        }

        private OpenApiDocument ExtractOpenApiTagsFromPaths(OpenApiDocument openApiDoc)
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

        private IEnumerable<FileNameInfo> SplitOperationGroups(string targetDir, string filePath, OpenApiDocument openApiDoc, string serviceName, OperationGroupMapping operationGroupMapping, MappingFile mappingFile)
        {
            openApiDoc = ExtractOpenApiTagsFromPaths(openApiDoc);
            if (openApiDoc.Tags == null || openApiDoc.Tags.Count == 0)
            {
                Console.WriteLine($"tags is null or empty for file {filePath}.");
            }

            foreach (var tag in openApiDoc.Tags)
            {
                var filteredPathAndOperationsWithExtendTags = SplitHelper.FindOperationsByTag(openApiDoc.Paths, tag);
                var filteredPathAndOperations = filteredPathAndOperationsWithExtendTags.Item1;
                if (filteredPathAndOperations.Count > 0)
                {
                    var fileNameInfo = new FileNameInfo
                    {
                        TocName = tag.Name
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

                    var groupName = string.IsNullOrEmpty(mappingFile.TagSeparator) ? tag.Name : tag.Name.Replace(mappingFile.TagSeparator, ".");

                    // Split operation group to operation
                    fileNameInfo.ChildrenFileNameInfo = new List<FileNameInfo>(SplitOperations(filteredPathAndOperations, openApiDoc, serviceName, groupName, targetDir, newTagName));

                    var model = new TransformModel
                    {
                        OpenApiDoc = openApiDoc,
                        OpenApiTag = tag,
                        ServiceName = serviceName,
                        OperationGroupName = groupName
                    };

                    var nameInfo = TransformerFactory?.TransformerOperationGroup(model, targetDir);
                    fileNameInfo.FileId = nameInfo.FileId;
                    fileNameInfo.FileName = nameInfo.FileName;
                    yield return fileNameInfo;

                    foreach (var extendTag in filteredPathAndOperationsWithExtendTags.Item2)
                    {
                        var extendFileNameInfo = new FileNameInfo
                        {
                            TocName = extendTag,
                            FileId = nameInfo.FileId,
                            FileName = nameInfo.FileName,
                            ChildrenFileNameInfo = fileNameInfo.ChildrenFileNameInfo
                        };
                        yield return extendFileNameInfo;
                    }
                }
            }
        }

        private IEnumerable<FileNameInfo> SplitOperations(List<KeyValuePair<string, KeyValuePair<OperationType, OpenApiOperation>>> pathAndOperations, OpenApiDocument openApiDoc, string serviceName, string groupName, string targetDir, string tag)
        {
            foreach (var pathAndOperation in pathAndOperations)
            {
                var operationName = pathAndOperation.Value.Value.OperationId;

                // todo: remove this after the Graph team fix the operation Id.
                if (operationName.Length > 50)
                {
                    operationName = operationName.Split('.').Last().Substring(0, 20);
                }
                var fileNameInfo = new FileNameInfo
                {
                    TocName = operationName,
                    FileName = Path.Combine(tag, $"{operationName}.yml")
                };

                var model = new TransformModel
                {
                    OpenApiDoc = openApiDoc,
                    Operation = pathAndOperation.Value,
                    Path = pathAndOperation.Key,
                    ServiceName = serviceName,
                    OperationGroupName = groupName,
                    ComponentGroupName = ComponentGroupName,
                    OperationName = Utility.ExtractPascalNameByRegex(fileNameInfo.TocName),
                };
                var nameInfo = TransformerFactory?.TransformerOperation(model, targetDir);
                fileNameInfo.FileId = nameInfo.FileId;
                fileNameInfo.FileName = nameInfo.FileName;
                yield return fileNameInfo;
            }
        }

        private FileNameInfo GenerateComponents(string targetDir, OpenApiDocument openApiDoc, string serviceName)
        {
            var model = new TransformModel
            {
                OpenApiDoc = openApiDoc,
                ServiceName = serviceName,
                ComponentGroupName = ComponentGroupName
            };

            return TransformerFactory.TransformerComponents(model, targetDir, model.ComponentGroupName);
        }
    }
}
