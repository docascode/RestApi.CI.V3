namespace Microsoft.RestApi.Splitters
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Microsoft.OpenApi.Models;
    using Microsoft.OpenApi.Readers;

    using Microsoft.RestApi.Common;
    using Microsoft.RestApi.Models;
    using Microsoft.RestApi.Transformers;

    public class RestSplitter
    {
        protected static readonly string ComponentGroupName = "resources";
        protected const string TocFileName = "toc.md";
        protected static readonly Regex TocRegex = new Regex(@"^(?<headerLevel>#+)(( |\t)*)\[(?<tocTitle>.+)\]\((?<tocLink>(?!http[s]?://).*?)\)( |\t)*#*( |\t)*(\n|$)", RegexOptions.Compiled);

        public string SourceRootDir { get; }
        public string TargetRootDir { get; }
        public string OutputDir { get; }
        public MappingFile MappingFile { get; }

        public IList<string> Errors { get; set; }

        public RestSplitter(string sourceRootDir, string targetRootDir, string mappingFilePath, string outputDir)
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
        }

        public virtual void Process()
        {
            var targetApiDir = SplitHelper.GetOutputDirectory(OutputDir);
            if (MappingFile.VersionList?.Count > 0)
            {
                Console.WriteLine($"Found version list : {string.Join(",", MappingFile.VersionList)}");
                // Generate with version infos
                foreach (var version in MappingFile.VersionList)
                {
                    Console.WriteLine($"Starting to handle version : {version}");
                    var targetApiVersionDir = Path.Combine(targetApiDir, version);
                    Directory.CreateDirectory(targetApiVersionDir);
                    if (!targetApiVersionDir.EndsWith(Path.DirectorySeparatorChar.ToString()))
                    {
                        targetApiVersionDir = targetApiVersionDir + Path.DirectorySeparatorChar;
                    }

                    var rootGroup = VersionSplit(targetApiVersionDir, version);
                    Console.WriteLine($"Finished to handle version : {version}");

                    WriteToc(targetApiVersionDir, rootGroup);
                }
            }
            else
            {
                Console.WriteLine("No found version list");
                // Generate with no version info
                var rootGroup = VersionSplit(targetApiDir, null);

                WriteToc(targetApiDir, rootGroup);
            }
            
            PrintAndClearError();
            Console.WriteLine("Done!");
        }

        public virtual void GenerateTocAndYamls(string targetApiVersionDir, SplitSwaggerResult splitSwaggerResult, RestTocGroup restTocGroup)
        {
        }

        public virtual void WriteToc(string targetApiDir, RestTocGroup restTocGroup)
        {
            Console.WriteLine("Generating toc...");
            var targetTocPath = Path.Combine(targetApiDir, TocFileName);

            using (var writer = new StreamWriter(targetTocPath))
            {
                WriteTocCore(writer, restTocGroup, "#", 1);
            }

            TocConverter.Convert(targetTocPath);
            if (File.Exists(targetTocPath))
            {
                File.Delete(targetTocPath);
            }

            Console.WriteLine("Toc has been generated.");
        }

        public virtual void WriteTocCore(StreamWriter writer, RestTocGroup tocGroup, string signs, int depth)
        {
            if (tocGroup != null)
            {
                var sortedGroups = new SortedDictionary<string, RestTocGroup>(tocGroup.Groups);
                foreach (var group in sortedGroups)
                {
                    if (!string.IsNullOrEmpty(group.Key))
                    {
                        if (depth < 3)
                        {
                            writer.WriteLine($"{signs} {Utility.ExtractPascalNameByRegex(group.Key)}");
                        }
                        else
                        {
                            writer.WriteLine(!string.IsNullOrEmpty(group.Value.Id)
                                  ? $"{signs} [{Utility.ExtractPascalNameByRegex(group.Key)}](xref:{group.Value.Id})"
                                  : $"{signs} {Utility.ExtractPascalNameByRegex(group.Key)}");
                        }
                        WriteTocCore(writer, group.Value, signs + "#", depth + 1);
                    }
                    else
                    {
                        WriteTocCore(writer, group.Value, signs, depth + 1);
                    }
                }

                if (tocGroup.RestTocLeaves?.Count > 0)
                {
                    var aggregateOperations = tocGroup.RestTocLeaves.OrderBy(p => p.MainOperation.Name);
                    foreach (var aggregateOperation in aggregateOperations)
                    {
                        writer.WriteLine($"{signs} [{Utility.ExtractPascalNameByRegex(aggregateOperation.MainOperation.Name)}](xref:{aggregateOperation.MainOperation.Id})");
                    }
                }
            }
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

        private RestTocGroup VersionSplit(string targetApiDir, string version)
        {
            var rootGroup = new RestTocGroup();

            Console.WriteLine($"Starting to split services");
            foreach (var orgInfo in MappingFile.OrganizationInfos)
            {
                if (version == null || string.Equals(orgInfo.Version, version))
                {
                    foreach (var service in orgInfo.Services)
                    {
                        if (service.SwaggerInfo?.Count > 0)
                        {
                            Console.WriteLine($"Starting to split service: {service.UrlGroup}");
                            var serviceGroup = SplitSwaggers(targetApiDir, service);
                            rootGroup[service.UrlGroup] = serviceGroup;
                            Console.WriteLine($"finished to split service: {service.UrlGroup}");
                        }
                    }
                }
            }
            Console.WriteLine("finished to split all services");
            return rootGroup;
        }

        private void ResolveRelationships(SplitSwaggerResult splitResult)
        {
            Console.WriteLine("starting to resolve relationships");
            foreach (var operationGroup in splitResult.OperationGroups)
            {
                foreach(var operation in operationGroup.Operations)
                {
                    if (operation.Responses?.Count > 0)
                    {
                        foreach(var response in operation.Responses)
                        {
                            if (response.ResponseLinks?.Count > 0)
                            {
                                foreach (var link in response.ResponseLinks)
                                {
                                    var foundOperation = FindOperation(splitResult, link.OperationId);
                                    if (foundOperation != null)
                                    {
                                        link.OperationId = foundOperation.Id;
                                    }
                                    else
                                    {
                                        Console.WriteLine($"can not resolve relationship of operation id: {link.OperationId}");
                                    }
                                }
                            }
                        }
                    }
                }
            }
            Console.WriteLine("finished to resolve relationships");
        }

        private OperationEntity FindOperation(SplitSwaggerResult splitResult, string internalOpeartionId)
        {
            foreach (var operationGroup in splitResult.OperationGroups)
            {
                foreach (var operation in operationGroup.Operations)
                {
                    if (string.Equals(operation.InternalOpeartionId, internalOpeartionId, StringComparison.OrdinalIgnoreCase))
                    {
                        return operation;
                    }
                }
            }
            return null;
        }

        private RestTocGroup SplitSwaggers(string targetApiDir, ServiceInfo service)
        {
            var rootGroup = new RestTocGroup();
            foreach (var swagger in service.SwaggerInfo)
            {
                var sourceFile = Path.Combine(SourceRootDir, swagger.Source.TrimEnd());
                Console.WriteLine($"Starting to split swagger: {sourceFile}");
                if (!File.Exists(sourceFile))
                {
                    throw new ArgumentException($"{nameof(sourceFile)} '{sourceFile}' should exist.");
                }

                string subGroupName = swagger.SubGroupTocTitle ?? string.Empty;
                var restTocGroup = rootGroup[subGroupName];
                if (restTocGroup == null)
                {
                    rootGroup.Name = subGroupName;
                    rootGroup[subGroupName] = new RestTocGroup();
                }
                restTocGroup = rootGroup[subGroupName];
                var splitResult = SplitSwagger(sourceFile, service.UrlGroup, swagger.OperationGroupMapping, MappingFile);
                Console.WriteLine($"finished to split swagger: {sourceFile}");

                ResolveRelationships(splitResult);
                GenerateTocAndYamls(targetApiDir, splitResult, restTocGroup);
            }
            return rootGroup;
        }

        private SplitSwaggerResult SplitSwagger(string sourceFilePath, string serviceName, OperationGroupMapping operationGroupMapping, MappingFile mappingFile)
        {
            if (!File.Exists(sourceFilePath))
            {
                throw new ArgumentException($"{nameof(sourceFilePath)} '{sourceFilePath}' should exist.");
            }

            var splitSwaggerResult = new SplitSwaggerResult();
            using (var streamReader = File.OpenText(sourceFilePath))
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
                splitSwaggerResult.OperationGroups = SplitSwaggerByTag(openApiDoc, serviceName, operationGroupMapping, mappingFile);
                splitSwaggerResult.ComponentGroup = GetTransformerdComponentGroup(openApiDoc, serviceName);
            }
            return splitSwaggerResult;
        }

        public virtual string GetOperationName(string operationName)
        {
            return operationName.FirstLetterToLower();
        }

        public virtual string GetOperationGroupName(string operationGroupName, string operationId)
        {
            return operationGroupName;
        }

        private IList<OperationGroupEntity> SplitSwaggerByTag(OpenApiDocument openApiDoc, string serviceName, OperationGroupMapping operationGroupMapping, MappingFile mappingFile)
        {
            openApiDoc = SplitHelper.AggregateOpenApiTagsFromPaths(openApiDoc);

            var operationGroups = new List<OperationGroupEntity>();
            foreach (var tag in openApiDoc.Tags)
            {
                var filteredOpenApiPath = SplitHelper.FindOperationsByTag(openApiDoc.Paths, tag);
                if (filteredOpenApiPath.Operations.Count > 0)
                {
                    string newTagName = tag.Name;
                    if (operationGroupMapping != null && operationGroupMapping.TryGetValue(tag.Name, out var foundTagName))
                    {
                        newTagName = foundTagName;
                    }
                    var groupName = string.IsNullOrEmpty(mappingFile.TagSeparator) ? newTagName : newTagName.Replace(mappingFile.TagSeparator, ".");

                    var model = new TransformModel
                    {
                        OpenApiDoc = openApiDoc,
                        OpenApiTag = tag,
                        ServiceName = serviceName,
                        OperationGroupName = groupName,
                        OperationGroupId = Utility.GetId(serviceName, groupName, null),
                        ComponentGroupName = ComponentGroupName,
                        ComponentGroupId = Utility.GetId(serviceName, ComponentGroupName, null),
                    };
                    var componentGroup = RestOperationGroupTransformer.Transform(model);
                    componentGroup.Operations = GetTransformerdOperationGroups(filteredOpenApiPath, model);
                    operationGroups.Add(componentGroup);
                }
            }
            return operationGroups;
        }

        private IList<OperationEntity> GetTransformerdOperationGroups(FilteredOpenApiPath filteredOpenApiPath, TransformModel model)
        {
            var operations = new List<OperationEntity>();
            foreach (var operation in filteredOpenApiPath.Operations)
            {
                var operationName = GetOperationName(operation.Operation.Value.OperationId);
                model.OperationGroupName = GetOperationGroupName(model.OperationGroupName, operation.Operation.Value.OperationId);
                model.OperationGroupId = Utility.GetId(model.ServiceName, model.OperationGroupName, null);
                model.OperationId = Utility.GetId(model.ServiceName, model.OperationGroupName, operationName);
                model.OperationName = Utility.ExtractPascalNameByRegex(operationName);
                model.Operation = operation.Operation;
                model.OpenApiPath = operation.OpenApiPath;
                operations.Add(RestOperationTransformer.Transform(model));
            }
            return operations;
        }

        private ComponentGroupEntity GetTransformerdComponentGroup(OpenApiDocument openApiDoc, string serviceName)
        {
            var model = new TransformModel
            {
                OpenApiDoc = openApiDoc,
                ServiceName = serviceName,
                ComponentGroupName = ComponentGroupName,
                ComponentGroupId = Utility.GetId(serviceName, ComponentGroupName, null),
            };
            var componentGroup = RestComponentGroupTransformer.Transform(model);
            var components = new List<ComponentEntity>();
            if (openApiDoc.Components?.Schemas != null)
            {
                foreach (var schema in openApiDoc.Components?.Schemas)
                {
                    model.ComponentId = Utility.GetId(serviceName, ComponentGroupName, schema.Key);
                    model.ComponentName = schema.Key;
                    model.OpenApiSchema = schema.Value;
                    components.Add(RestComponentTransformer.Transform(model));
                }
            }
            componentGroup.Components = components;
            return componentGroup;
        }
    }
}
