namespace Microsoft.RestApi.Splitters
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Microsoft.DocAsCode.YamlSerialization;
    using Microsoft.OpenApi.Models;
    using Microsoft.OpenApi.Readers;

    using Microsoft.RestApi.Common;
    using Microsoft.RestApi.Models;
    using Microsoft.RestApi.Transformers;

    public class RestSplitter
    {
        protected const string TocFileName = "toc.md";
        public static readonly string YamlExtension = ".yml";
        protected static readonly Regex TocRegex = new Regex(@"^(?<headerLevel>#+)(( |\t)*)\[(?<tocTitle>.+)\]\((?<tocLink>(?!http[s]?://).*?)\)( |\t)*#*( |\t)*(\n|$)", RegexOptions.Compiled);
        public static readonly YamlSerializer YamlSerializer = new YamlSerializer();

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
            if(Directory.Exists(targetApiDir))
            {
                Directory.Delete(targetApiDir, true);
            }
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

        public virtual void GenerateTocAndYamls(string targetApiVersionDir, SplitSwaggerResult splitSwaggerResult, RestTocGroup serviceGroup)
        {
            // Generate Toc
            GenerateToc(splitSwaggerResult, serviceGroup);

            //Write Yamls
            GenerateYamls(targetApiVersionDir, splitSwaggerResult);
        }

        private void GenerateYamls(string targetApiVersionDir, SplitSwaggerResult splitSwaggerResult)
        {
            var operationGroups = splitSwaggerResult.OperationGroups;
            var componentGroups = splitSwaggerResult.ComponentGroups;
            var serviceName = operationGroups != null ? operationGroups.First().Service :
                componentGroups != null ? componentGroups.First().Service 
                : null;
            var servicePath = Path.Combine(targetApiVersionDir, serviceName);
            if (!Directory.Exists(servicePath))
            {
                Directory.CreateDirectory(servicePath);
            }

            GenerateComponents(servicePath, componentGroups);
            GenerateOperations(servicePath, operationGroups);

        }

        private void GenerateOperations(string servicePath, List<OperationGroupEntity> operationGroups)
        {
            if (operationGroups == null) return;
            foreach (var group in operationGroups)
            {
                var groupFileName = Utility.GetId("", group.Name);
                var groupFolder = Path.Combine(servicePath, groupFileName);
                if (!Directory.Exists(groupFolder))
                {
                    Directory.CreateDirectory(groupFolder);
                }

                using (var writer = new StreamWriter(Path.Combine(servicePath, $"{groupFileName}{YamlExtension}")))
                {
                    writer.WriteLine("### YamlMime:RESTOperationGroupV3");
                    YamlSerializer.Serialize(writer, group);
                }

                foreach (var operation in group.Operations)
                {
                    using (var writer = new StreamWriter(Path.Combine(groupFolder, $"{Utility.GetId("", operation.Name)}{YamlExtension}")))
                    {
                        writer.WriteLine("### YamlMime:RESTOperationV3");
                        YamlSerializer.Serialize(writer, operation);
                    }
                }
            }
        }

        private void GenerateComponents(string servicePath, List<ComponentGroupEntity> componentGroups)
        {
            var componentsFolder = Path.Combine(servicePath, "Components");
            if (!Directory.Exists(componentsFolder))
            {
                Directory.CreateDirectory(componentsFolder);
            }

            foreach (var group in componentGroups)
            {
                var schema = string.Empty;
                switch (group.Name)
                {
                    case "Schemas":
                        schema = "RESTTypeV3";
                        break;
                    case "Responses":
                        schema = "RESTResponseV3";
                        break;
                    case "Parameters":
                        schema = "RESTParameterV3";
                        break;
                    case "Examples":
                        schema = "RESTExampleV3";
                        break;
                    case "RequestBodies":
                        schema = "RESTRequestBodyV3";
                        break;
                    case "ReponseHeaders":
                        schema = "RESTResponseHeaderV3";
                        break;
                    case "Securities":
                        schema = "RESTSecurityV3";
                        break;
                    default:
                        schema = string.Empty;
                        break;
                }

                var groupFileName = Utility.GetId("", group.Name);
                var groupFolder = Path.Combine(componentsFolder, groupFileName);
                if (!Directory.Exists(groupFolder))
                {
                    Directory.CreateDirectory(groupFolder);
                }

                using (var writer = new StreamWriter(Path.Combine(componentsFolder, $"{groupFileName}{YamlExtension}")))
                {
                    writer.WriteLine("### YamlMime:RESTComponentGroupV3");
                    YamlSerializer.Serialize(writer, group);
                }

                foreach (var component in group.Components)
                {
                    using (var writer = new StreamWriter(Path.Combine(groupFolder, $"{Utility.GetId("", component.Name)}{YamlExtension}")))
                    {
                        writer.WriteLine($"### YamlMime:{schema}");
                        YamlSerializer.Serialize(writer, component);
                    }
                }
            }

        }

        private void GenerateToc(SplitSwaggerResult splitSwaggerResult, RestTocGroup serviceGroup)
        {
            var componnentGroups = new RestTocGroup { Name = "Components" };
            serviceGroup[componnentGroups.Name] = componnentGroups;

            foreach (var cpGroup in splitSwaggerResult.ComponentGroups)
            {
                var componnentGroup = new RestTocGroup { Name = cpGroup.Name, Id = cpGroup.Id };
                componnentGroups[componnentGroup.Name] = componnentGroup;

                foreach (var component in cpGroup.Components)
                {
                    try
                    {
                        componnentGroup[component.Name] = new RestTocGroup { Name = component.Name, Id = component.Id };
                    }
                    catch
                    {

                    }
                }
            }

            if (splitSwaggerResult.OperationGroups != null)
            {
                foreach (var opGroup in splitSwaggerResult.OperationGroups)
                {
                    var operationGroup = new RestTocGroup { Name = opGroup.Name, Id = opGroup.Id };
                    serviceGroup[operationGroup.Name] = operationGroup;
                    foreach (var operation in opGroup.Operations)
                    {
                        operationGroup[operation.Name] = new RestTocGroup { Name = operation.Name, Id = operation.Id };
                    }
                }
            }
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

        //private void ResolveRelationships(SplitSwaggerResult splitResult)
        //{
        //    Console.WriteLine("starting to resolve relationships");
        //    foreach (var operationGroup in splitResult.OperationGroups)
        //    {
        //        foreach(var operation in operationGroup.Operations)
        //        {
        //            if (operation.Responses?.Count > 0)
        //            {
        //                foreach(var response in operation.Responses)
        //                {
        //                    if (response.ResponseLinks?.Count > 0)
        //                    {
        //                        foreach (var link in response.ResponseLinks)
        //                        {
        //                            var foundOperation = FindOperation(splitResult, link.OperationId);
        //                            if (foundOperation != null)
        //                            {
        //                                link.OperationId = foundOperation.Id;
        //                            }
        //                            else
        //                            {
        //                                Console.WriteLine($"can not resolve relationship of operation id: {link.OperationId}");
        //                            }
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    Console.WriteLine("finished to resolve relationships");
        //}

        //private OperationEntity FindOperation(SplitSwaggerResult splitResult, string internalOpeartionId)
        //{
        //    foreach (var operationGroup in splitResult.OperationGroups)
        //    {
        //        foreach (var operation in operationGroup.Operations)
        //        {
        //            if (string.Equals(operation.InternalOpeartionId, internalOpeartionId, StringComparison.OrdinalIgnoreCase))
        //            {
        //                return operation;
        //            }
        //        }
        //    }
        //    return null;
        //}

        private RestTocGroup SplitSwaggers(string targetApiDir, ServiceInfo service)
        {
            var serviceGroup = new RestTocGroup { Name = service.TocFile };
            var splitResults = new List<SplitSwaggerResult>();
            foreach (var swagger in service.SwaggerInfo)
            {
                var sourceFile = Path.Combine(SourceRootDir, swagger.Source.TrimEnd());
                Console.WriteLine($"Starting to split swagger: {sourceFile}");
                if (!File.Exists(sourceFile))
                {
                    throw new ArgumentException($"{nameof(sourceFile)} '{sourceFile}' should exist.");
                }

                string subGroupName = swagger.SubGroupTocTitle ?? string.Empty;
                splitResults.Add(SplitSwagger(sourceFile, service.UrlGroup, swagger.OperationGroupMapping, MappingFile));
                Console.WriteLine($"finished to split swagger: {sourceFile}");

                //ResolveRelationships(splitResult);
            }

            GenerateTocAndYamls(targetApiDir, MergeResults(splitResults), serviceGroup);
            return serviceGroup;
        }

        private SplitSwaggerResult MergeResults(List<SplitSwaggerResult> splitResults)
        {
            var componentGroupDictionary = new Dictionary<string, ComponentGroupEntity>();
            var operationGroupDictionary = new Dictionary<string, OperationGroupEntity>();
            foreach (var singleResult in splitResults)
            {
                if (singleResult.ComponentGroups != null)
                {
                    foreach (var cg in singleResult.ComponentGroups)
                    {
                        if (!componentGroupDictionary.ContainsKey(cg.Id))
                        {
                            componentGroupDictionary[cg.Id] = cg;
                        }
                        else
                        {
                            componentGroupDictionary[cg.Id].Components.AddRange(cg.Components);
                        }
                    }
                }

                if (singleResult.OperationGroups != null)
                {
                    foreach (var og in singleResult.OperationGroups)
                    {
                        if (!operationGroupDictionary.ContainsKey(og.Id))
                        {
                            operationGroupDictionary[og.Id] = og;
                        }
                        else
                        {
                            operationGroupDictionary[og.Id].Operations.AddRange(og.Operations);
                        }
                    }
                }
            }

            return new SplitSwaggerResult
            {
                ComponentGroups = componentGroupDictionary.Any()? componentGroupDictionary.Values.ToList() : null,
                OperationGroups = operationGroupDictionary.Any()? operationGroupDictionary.Values.ToList() : null
            };
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

                var needExtractedSchemas = new Dictionary<string, OpenApiSchema>();
                splitSwaggerResult.OperationGroups = SplitSwaggerByTag(openApiDoc, serviceName, operationGroupMapping, mappingFile);
                splitSwaggerResult.ComponentGroups = GetTransformerdComponentGroups(openApiDoc, serviceName, ref needExtractedSchemas);
            }
            return splitSwaggerResult;
        }

        public virtual string GetOperationName(string operationName)
        {
            if (operationName.Contains('-'))
            {
                operationName = operationName.Split('-').Last();
            }
            if (operationName.Contains('.'))
            {
                operationName = operationName.Split('.').Last();
            }
            return operationName.FirstLetterToLower(); ;
        }

        public virtual string GetOperationGroupName(string operationGroupName, string operationId)
        {
            return operationGroupName;
        }

        private List<OperationGroupEntity> SplitSwaggerByTag(
            OpenApiDocument openApiDoc, 
            string serviceName, 
            OperationGroupMapping operationGroupMapping, 
            MappingFile mappingFile)
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
                        OperationGroupId = Utility.GetId(serviceName, groupName, null)
                    };
                    var operationGroup = RestOperationGroupTransformer.Transform(model);
                    operationGroup.Operations = GetTransformerdOperations(filteredOpenApiPath, model);
                    operationGroups.Add(operationGroup);
                }
            }
            return operationGroups;
        }

        private List<OperationV3Entity> GetTransformerdOperations(FilteredOpenApiPath filteredOpenApiPath, TransformModel operationGroup)
        {
            var operations = new List<OperationV3Entity>();
            foreach (var operation in filteredOpenApiPath.Operations)
            {
                var operationName = GetOperationName(operation.Operation.Value.OperationId);
                operationGroup.ServiceName = operationGroup.ServiceName;
                operationGroup.OperationId = Utility.GetId(operationGroup.ServiceName, operationGroup.OperationGroupName, operationName);
                operationGroup.OperationName = Utility.ExtractPascalNameByRegex(operationName);
                operationGroup.Operation = operation.Operation;
                operationGroup.OpenApiPath = operation.OpenApiPath;
                operations.Add(RestOperationTransformer.Transform(operationGroup));
            }
            return operations;
        }

        private List<ComponentGroupEntity> GetTransformerdComponentGroups(OpenApiDocument openApiDoc, string serviceName, ref Dictionary<string, OpenApiSchema> needExtractedSchemas)
        {
            if (openApiDoc.Components == null) return null;
            var componentGroups = new List<ComponentGroupEntity>();

            if (openApiDoc.Components.Schemas != null && openApiDoc.Components.Schemas.Any())
            {
                componentGroups.Add(GetTransformerdTypesGroup(openApiDoc, serviceName, ref needExtractedSchemas));
            }

            // todo: Callbacks

            //if (openApiDoc.Components.Examples != null && openApiDoc.Components.Examples.Any())
            //{
            //    componentGroups.Add(GetTransformerdExampleGroup(openApiDoc, serviceName));
            //}

            //if (openApiDoc.Components.Headers != null && openApiDoc.Components.Headers.Any())
            //{
            //    componentGroups.Add(GetTransformerdResponseHeaderGroup(openApiDoc, serviceName));
            //}

            //if (openApiDoc.Components.Parameters != null && openApiDoc.Components.Parameters.Any())
            //{
            //    componentGroups.Add(GetTransformerdParametersGroup(openApiDoc, serviceName));
            //}

            //if (openApiDoc.Components.RequestBodies != null && openApiDoc.Components.RequestBodies.Any())
            //{
            //    componentGroups.Add(GetTransformerdRequestBodiesGroup(openApiDoc, serviceName));
            //}

            //if (openApiDoc.Components.Responses != null && openApiDoc.Components.Responses.Any())
            //{
            //    componentGroups.Add(GetTransformerdReponsesGroup(openApiDoc, serviceName));
            //}


            //if (openApiDoc.Components.SecuritySchemes != null && openApiDoc.Components.SecuritySchemes.Any())
            //{
            //    componentGroups.Add(GetTransformerdSecurityGroup(openApiDoc, serviceName));
            //}

            return componentGroups;
        }

        private ComponentGroupEntity GetTransformerdSecurityGroup(OpenApiDocument openApiDoc, string serviceName)
        {
            var componentGroupName = ComponentGroup.Securities.ToString();
            var model = new TransformModel
            {
                OpenApiDoc = openApiDoc,
                ServiceName = serviceName,
                ComponentGroupName = componentGroupName,
                ComponentGroupId = Utility.GetId(serviceName, componentGroupName, null),
            };
            var componentGroup = RestComponentGroupTransformer.Transform(model);
            componentGroup.Components = new List<NamedEntity>();

            foreach (var security in TransformHelper.TransformSecurities(model, openApiDoc.Components.SecuritySchemes))
            {
                componentGroup.Components.Add(security);
            }

            return componentGroup;
        }

        private ComponentGroupEntity GetTransformerdTypesGroup(OpenApiDocument openApiDoc, string serviceName, ref Dictionary<string, OpenApiSchema> needExtractedSchemas)
        {
            var componentGroupName = ComponentGroup.Schemas.ToString();
            var model = new TransformModel
            {
                OpenApiDoc = openApiDoc,
                ServiceName = serviceName,
                ComponentGroupName = componentGroupName,
                ComponentGroupId = Utility.GetId(serviceName, componentGroupName, null),
            };
            var componentGroup = RestComponentGroupTransformer.Transform(model);
            componentGroup.Components = new List<NamedEntity>();
            foreach (var schema in TransformHelper.TransformSchemas(model, openApiDoc.Components.Schemas, ref needExtractedSchemas, true))
            {
                componentGroup.Components.Add(schema);
            }

            return componentGroup;
        }

        private ComponentGroupEntity GetTransformerdReponsesGroup(OpenApiDocument openApiDoc, string serviceName)//
        {
            var componentGroupName = ComponentGroup.Responses.ToString();
            var model = new TransformModel
            {
                OpenApiDoc = openApiDoc,
                ServiceName = serviceName,
                ComponentGroupName = componentGroupName,
                ComponentGroupId = Utility.GetId(serviceName, componentGroupName, null),
            };
            var componentGroup = RestComponentGroupTransformer.Transform(model);
            componentGroup.Components = new List<NamedEntity>();

            foreach (var response in TransformHelper.TransformResponses(model, openApiDoc.Components.Responses, true))
            {
                componentGroup.Components.Add(response);
            }

            return componentGroup;
        }

        private ComponentGroupEntity GetTransformerdRequestBodiesGroup(OpenApiDocument openApiDoc, string serviceName)
        {
            var componentGroupName = ComponentGroup.RequestBodies.ToString();
            var model = new TransformModel
            {
                OpenApiDoc = openApiDoc,
                ServiceName = serviceName,
                ComponentGroupName = componentGroupName,
                ComponentGroupId = Utility.GetId(serviceName, componentGroupName, null),
            };
            var componentGroup = RestComponentGroupTransformer.Transform(model);
            componentGroup.Components = new List<NamedEntity>();

            foreach (var requestBody in TransformHelper.TransformRequestBodies(model, openApiDoc.Components.RequestBodies, true))
            {
                componentGroup.Components.Add(requestBody);
            }

            return componentGroup;
        }

        private ComponentGroupEntity GetTransformerdParametersGroup(OpenApiDocument openApiDoc, string serviceName)
        {
            var componentGroupName = ComponentGroup.Parameters.ToString();
            var model = new TransformModel
            {
                OpenApiDoc = openApiDoc,
                ServiceName = serviceName,
                ComponentGroupName = componentGroupName,
                ComponentGroupId = Utility.GetId(serviceName, componentGroupName, null),
            };
            var componentGroup = RestComponentGroupTransformer.Transform(model);
            componentGroup.Components = new List<NamedEntity>();

            foreach (var parameter in TransformHelper.TransformParameters(model, openApiDoc.Components.Parameters, true))
            {
                componentGroup.Components.Add(parameter);
            }

            return componentGroup;
        }

        private ComponentGroupEntity GetTransformerdResponseHeaderGroup(OpenApiDocument openApiDoc, string serviceName)
        {
            var componentGroupName = ComponentGroup.ReponseHeaders.ToString();
            var model = new TransformModel
            {
                OpenApiDoc = openApiDoc,
                ServiceName = serviceName,
                ComponentGroupName = componentGroupName,
                ComponentGroupId = Utility.GetId(serviceName, componentGroupName, null),
            };
            var componentGroup = RestComponentGroupTransformer.Transform(model);
            componentGroup.Components = new List<NamedEntity>();

            foreach (var header in TransformHelper.TransformResponseHeaders(model, openApiDoc.Components.Headers, true))
            {
                componentGroup.Components.Add(header);
            }

            return componentGroup;
        }

        private ComponentGroupEntity GetTransformerdExampleGroup(OpenApiDocument openApiDoc, string serviceName)
        {
            var componentGroupName = ComponentGroup.Examples.ToString();
            var model = new TransformModel
            {
                OpenApiDoc = openApiDoc,
                ServiceName = serviceName,
                ComponentGroupName = componentGroupName,
                ComponentGroupId = Utility.GetId(serviceName, componentGroupName, null),
            };
            var componentGroup = RestComponentGroupTransformer.Transform(model);
            componentGroup.Components = new List<NamedEntity>();

            foreach (var example in TransformHelper.TransformExamples(model, openApiDoc.Components.Examples, true))
            {
                componentGroup.Components.Add(example);
            }

            return componentGroup;
        }
    }
}
