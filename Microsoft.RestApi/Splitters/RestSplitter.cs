namespace Microsoft.RestApi.Splitters
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
    using Microsoft.OpenApi.Any;

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

            var targetApiDir = SplitHelper.GetOutputDirectory(OutputDir);
            // Write toc structure from MappingFile
            if (MappingFile.VersionList != null)
            {
                // Generate with version infos
                foreach (var version in MappingFile.VersionList)
                {
                    var targetApiVersionDir = Path.Combine(targetApiDir, version);
                    Directory.CreateDirectory(targetApiVersionDir);
                    if (!targetApiVersionDir.EndsWith(Path.DirectorySeparatorChar.ToString()))
                    {
                        targetApiVersionDir = targetApiVersionDir + Path.DirectorySeparatorChar;
                    }
                    WriteToc(targetApiVersionDir, version);
                }
            }
            else
            {
                // Generate with no version info
                WriteToc(targetApiDir, null);
            }
        }

        public Dictionary<string, RestTocGroup> SortGroups(Dictionary<string, RestTocGroup> tocGroups, int depth, string parentGroupName)
        {
            var sortedGroups = new SortedDictionary<string, RestTocGroup>(tocGroups);
            var finalGroups = new Dictionary<string, RestTocGroup>();

            var orderNameList = new List<string>();
            if (depth == 1 && MappingFile.FirstLevelTocOrders?.Count() > 0)
            {
                orderNameList = MappingFile.FirstLevelTocOrders.ToList();
            }
            else if (depth == 2 && MappingFile.SecondLevelTocOrders?.Count() > 0 && MappingFile.SecondLevelTocOrders.TryGetValue(parentGroupName, out var secondLevelSortOrders))
            {
                orderNameList = secondLevelSortOrders;
            }

            foreach (var orderName in orderNameList)
            {
                if (sortedGroups.TryGetValue(orderName, out var findTocs))
                {
                    finalGroups.Add(orderName, findTocs);
                }
            }

            foreach (var sortedGroup in sortedGroups)
            {
                if (!finalGroups.TryGetValue(sortedGroup.Key, out var findTocs))
                {
                    finalGroups.Add(sortedGroup.Key, sortedGroup.Value);
                }
            }

            return finalGroups;
        }

        private string GetConceptual(string conceptualFile, string targetApiVersionDir)
        {
            var conceptualHref = SplitHelper.GenerateHref(Path.Combine(TargetRootDir, MappingFile.ConceptualFolder), conceptualFile, targetApiVersionDir);
            if (string.IsNullOrEmpty(conceptualHref))
            {
                Errors.Add($"Can not find the conceptual file: {conceptualFile}");
            }
            return conceptualHref;
        }

        private string GetComponentId(List<RestTocLeaf> components, string componentFile)
        {
            foreach (var component in components)
            {
                var fileName = component.FileName.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
                var componentName = fileName.ToLower().Split(Path.DirectorySeparatorChar)?.Last();

                if (!string.IsNullOrEmpty(componentName) && componentName.Equals(componentFile))
                {
                    return component.Id;
                }
            }
            return null;
        }

        private string GetComponentFullPath(List<RestTocLeaf> components, string componentFile, string targetApiVersionDir)
        {
            foreach (var component in components)
            {
                var fileName = component.FileName.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
                var componentName = fileName.ToLower().Split(Path.DirectorySeparatorChar)?.Last();

                if (!string.IsNullOrEmpty(componentName) && componentName.Equals(componentFile))
                {
                    return Path.Combine(targetApiVersionDir, fileName);
                }
            }
            return null;
        }

        public virtual void ResolveComponent(string componentYamlFile, RestTocGroup restTocGroup)
        {
        }

        public void WriteRestTocTree(StreamWriter writer, List<RestTocLeaf> components, string targetApiVersionDir, string subTocPrefix, string subGroupTocPrefix, RestTocGroup tocGroup, string numberSign, int depth, string parentGroupName = null)
        {
            if (tocGroup != null)
            {
                var sortedGroups = SortGroups(tocGroup.Groups, depth, parentGroupName);
                foreach (var group in sortedGroups)
                {
                    if (!group.Value.IsComponentGroup)
                    {
                        if (depth == 1)
                        {
                            var href = GetConceptual(group.Key + ".md", targetApiVersionDir);
                            writer.WriteLine(!string.IsNullOrEmpty(href)
                               ? $"{subTocPrefix}{numberSign}{subGroupTocPrefix} [{Utility.ExtractPascalNameByRegex(group.Key)}]({href})"
                               : $"{subTocPrefix}{numberSign}{subGroupTocPrefix} {Utility.ExtractPascalNameByRegex(group.Key)}");
                        }
                        else
                        {
                            var componentFilePath = GetComponentFullPath(components, MappingFile.ComponentPrefix + group.Key.ToLower() + ".yml", targetApiVersionDir);
                            if (!string.IsNullOrEmpty(componentFilePath) && File.Exists(componentFilePath))
                            {
                                ResolveComponent(componentFilePath, group.Value);
                            }
                            var componentId = GetComponentId(components, MappingFile.ComponentPrefix + group.Key.ToLower() + ".yml");
                            writer.WriteLine(!string.IsNullOrEmpty(componentId)
                            ? $"{subTocPrefix}{numberSign}{subGroupTocPrefix} [{Utility.ExtractPascalNameByRegex(group.Key)}](xref:{componentId})"
                            : $"{subTocPrefix}{numberSign}{subGroupTocPrefix} {Utility.ExtractPascalNameByRegex(group.Key)}");
                            writer.WriteLine();
                        }

                        WriteRestTocTree(writer, components, targetApiVersionDir, subTocPrefix, subGroupTocPrefix, group.Value, numberSign + "#", depth + 1, group.Key);
                    }
                }
                   
                if(tocGroup.OperationOrComponents?.Count > 0)
                {
                    var operations = tocGroup.OperationOrComponents.OrderBy(p => p.Name);
                    foreach (var operation in operations)
                    {
                        writer.WriteLine($"{subTocPrefix}{numberSign}{subGroupTocPrefix} [{Utility.ExtractPascalNameByRegex(operation.Name)}](xref:{operation.Id})");
                    }
                }
            }
        }

        private List<RestTocLeaf> GetAllComponents(RestTocGroup restTocGroup)
        {
            var components = new List<RestTocLeaf>();
            foreach(var group in restTocGroup.Groups)
            {
                if (group.Value.IsComponentGroup)
                {
                    if (group.Value.OperationOrComponents != null)
                    {
                        foreach (var component in group.Value.OperationOrComponents)
                        {
                            components.Add(component);
                        }
                    }
                }
            }
            return components;
        }
        public void WriteRestToc(StreamWriter writer, string subTocPrefix, List<string> tocLines, RestTocGroup rootGroup, string targetApiVersionDir)
        {
            var subRefTocPrefix = string.Empty;
            if (tocLines != null && tocLines.Count > 0)
            {
                subRefTocPrefix = SplitHelper.IncreaseSharpCharacter(subRefTocPrefix);
                writer.WriteLine($"{subTocPrefix}#{subRefTocPrefix} Reference");
            }

            foreach (var pair in rootGroup.Groups)
            {
                var subGroupTocPrefix = subRefTocPrefix;
                if (!string.IsNullOrEmpty(pair.Key))
                {
                    subGroupTocPrefix = SplitHelper.IncreaseSharpCharacter(subRefTocPrefix);
                    writer.WriteLine($"{subTocPrefix}#{subGroupTocPrefix} {pair.Key}");
                }
                var tocGroup = pair.Value;
                var components = GetAllComponents(tocGroup);

                WriteRestTocTree(writer, components, targetApiVersionDir, subTocPrefix, subGroupTocPrefix, tocGroup, "##", 1);
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

        private void WriteToc(string targetApiVersionDir, string version)
        {
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
                    if (version == null || string.Equals(orgInfo.Version, version))
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
                            var rootToc = new RestTocGroup();
                            if (service.SwaggerInfo != null)
                            {
                                rootToc = SplitSwaggers(targetApiVersionDir, service);
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
                                WriteRestToc(writer, subTocPrefix, tocLines, rootToc, targetApiVersionDir);
                            }
                        }
                    }
                }
            }

            TocConverter.Convert(targetTocPath);
            if (File.Exists(targetTocPath))
            {
                File.Delete(targetTocPath);
            }

            PrintAndClearError();
        }

        private void PrintAndClearError()
        {
            if (Errors.Count > 0)
            {
                Errors = Errors.Distinct().ToList();
                foreach (var error in Errors)
                {
                    Console.WriteLine(error);
                }
            }
            Errors = new List<string>();
        }

        public RestTocGroup GenerateGroupTree(FileNameInfo fileNameInfo, List<string> groupNames, int index, RestTocGroup parentGroup)
        {
            if (index < groupNames.Count)
            {
                var childGroup = parentGroup[groupNames[index]];
                if(childGroup == null)
                {
                    childGroup = new RestTocGroup
                    {
                        Name = groupNames[index],
                        IsComponentGroup = fileNameInfo.IsComponentGroup
                    };

                    parentGroup[groupNames[index]] = childGroup;
                }
                return GenerateGroupTree(fileNameInfo, groupNames, index + 1, childGroup);
            }
            else
            {
                return parentGroup;
            }
        }

        private RestTocGroup SplitSwaggers(string targetApiVersionDir, ServiceInfo service)
        {
            var rootGroup = new RestTocGroup();
            foreach (var swagger in service.SwaggerInfo)
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

                var restTocGroup = rootGroup[subGroupName];
                if (restTocGroup == null)
                {
                    rootGroup.Name = subGroupName;
                    rootGroup[subGroupName] = new RestTocGroup();
                }
                restTocGroup = rootGroup[subGroupName];

                foreach (var fileNameInfo in restFileInfo.FileNameInfos)
                {
                    var groupNames = new List<string>();
                    if (!fileNameInfo.IsComponentGroup)
                    {
                        var groupName = fileNameInfo.TocName;
                        
                        if (groupName.Contains('.'))
                        {
                            groupNames = groupName.Split('.').ToList();
                        }
                        else
                        {
                            groupNames.Add(groupName);
                        }
                    }
                    else
                    {
                        groupNames.Add(fileNameInfo.TocName);
                    }

                    var operationOrComponents = new List<RestTocLeaf>();
                    if (fileNameInfo.ChildrenFileNameInfo != null && fileNameInfo.ChildrenFileNameInfo.Count > 0)
                    {
                        foreach (var nameInfo in fileNameInfo.ChildrenFileNameInfo)
                        {
                            operationOrComponents.Add(new RestTocLeaf
                            {
                                Id = nameInfo.FileId,
                                Name = nameInfo.TocName,
                                FileName = nameInfo.FileName,
                                IsComponent = fileNameInfo.IsComponentGroup,
                                OperationInfo = nameInfo.OperationInfo
                            });
                        }
                    }

                    var leafGroup = GenerateGroupTree(fileNameInfo, groupNames, 0, restTocGroup);
                    if (leafGroup.OperationOrComponents == null)
                    {
                        leafGroup.OperationOrComponents = new List<RestTocLeaf>();
                    }
                    leafGroup.OperationOrComponents.AddRange(operationOrComponents);
                }
                Console.WriteLine($"Done splitting swagger file from '{swagger.Source}' to '{service.UrlGroup}'");
            }

            return rootGroup;
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
                    restFileInfo.FileNameInfos = AddTocType(openApiDoc.Tags, fileNameInfos);
                }
                restFileInfo.TocTitle = openApiDoc.Info?.Title;

                var componentsFileNameInfo = GenerateComponents(targetDir, openApiDoc, serviceName);
                componentsFileNameInfo.IsComponentGroup = true;
                restFileInfo.FileNameInfos.Add(componentsFileNameInfo);
            }
            return restFileInfo;
        }

        private List<FileNameInfo> AddTocType(IList<OpenApiTag>tags, IEnumerable<FileNameInfo> fileNameInfos)
        {
            var results = new List<FileNameInfo>();
            foreach (var fileNameInfo in fileNameInfos)
            {
                var foundTag =tags.FirstOrDefault(t => t.Name == fileNameInfo.TocName);
                if (foundTag != null && foundTag.Extensions.TryGetValue("x-ms-docs-toc-type", out var tagType))
                {
                    if (tagType is OpenApiString stringValue)
                    {
                        if (Enum.TryParse<TocType>(stringValue.Value, true, out var tocType))
                        {
                            fileNameInfo.TocType = tocType;
                        }
                    }
                }
                results.Add(fileNameInfo);
            }
            return results;
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
                var filteredOpenApiPath = SplitHelper.FindOperationsByTag(openApiDoc.Paths, tag);
                if (filteredOpenApiPath.Operations.Count > 0)
                {

                    var fileNameInfo = new FileNameInfo();

                    // Get file name from operation group mapping
                    string newTagName = tag.Name;
                    if (operationGroupMapping != null && operationGroupMapping.TryGetValue(tag.Name, out var foundTagName))
                    {
                        newTagName = foundTagName;
                    }

                    var groupName = string.IsNullOrEmpty(mappingFile.TagSeparator) ? newTagName : newTagName.Replace(mappingFile.TagSeparator, ".");

                    // Split operation group to operation
                    fileNameInfo.ChildrenFileNameInfo = new List<FileNameInfo>(SplitOperations(filteredOpenApiPath, openApiDoc, serviceName, groupName, targetDir));

                    var model = new TransformModel
                    {
                        OpenApiDoc = openApiDoc,
                        OpenApiTag = tag,
                        ServiceName = serviceName,
                        OperationGroupName = groupName,
                        OperationGroupPath = groupName.Replace(".", "/")
                    };

                    var nameInfo = TransformerFactory?.TransformerOperationGroup(model, targetDir);
                    fileNameInfo.TocName = groupName;
                    fileNameInfo.FileId = nameInfo.FileId;
                    fileNameInfo.FileName = nameInfo.FileName;
                    yield return fileNameInfo;

                    foreach (var extendTag in filteredOpenApiPath.ExtendTagNames)
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

        private IEnumerable<FileNameInfo> SplitOperations(FilteredOpenApiPath filteredOpenApiPath, OpenApiDocument openApiDoc, string serviceName, string groupName, string targetDir)
        {
            foreach (var operation in filteredOpenApiPath.Operations)
            {
                var operationName = operation.Operation.Value.OperationId;
                // todo: remove this after the Graph team fix the operation Id.
                if (operationName.Contains('-'))
                {
                    operationName = operationName.Split('-').Last();
                }
                if (operationName.Contains('.'))
                {
                    operationName = operationName.Split('.').Last();
                }
                operationName = operationName.FirstLetterToLower();

                var fileNameInfo = new FileNameInfo
                {
                    TocName = operationName,
                    FileName = Path.Combine(groupName.Replace(".", "/"), $"{operationName}.yml")
                };

                var model = new TransformModel
                {
                    OpenApiDoc = openApiDoc,
                    Operation = operation.Operation,
                    OpenApiPath = operation.OpenApiPath,
                    ServiceName = serviceName,
                    OperationGroupName = groupName,
                    OperationGroupPath = groupName.Replace(".", "/"),
                    ComponentGroupName = ComponentGroupName,
                    OperationName = Utility.ExtractPascalNameByRegex(fileNameInfo.TocName),
                };
                var nameInfo = TransformerFactory?.TransformerOperation(model, targetDir);
                fileNameInfo.FileId = nameInfo.FileId;
                fileNameInfo.FileName = nameInfo.FileName;
                fileNameInfo.OperationInfo = nameInfo.OperationInfo;

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
