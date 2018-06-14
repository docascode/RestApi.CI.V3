namespace Microsoft.RestApi.Splitters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.DocAsCode.YamlSerialization;
    using Microsoft.RestApi.Models;

    public class GraphRestSplitter : RestSplitter
    {
        public static readonly string YamlExtension = ".yml";
        public static readonly YamlSerializer YamlSerializer = new YamlSerializer();
        public static readonly YamlDeserializer YamlDeserializer = new YamlDeserializer();

        public GraphRestSplitter(string sourceRootDir, string targetRootDir, string mappingFilePath, string outputDir)
            : base(sourceRootDir, targetRootDir, mappingFilePath, outputDir)
        {
        }
        public override void GenerateTocAndYamls(string targetApiVersionDir, SplitSwaggerResult splitSwaggerResult, RestTocGroup restTocGroup)
        {
            Console.WriteLine("Starting to aggregate split result");
            var aggregateResult = Aggregate(splitSwaggerResult);

            Console.WriteLine("Starting to generate toc for aggregated result");
            GenerateToc(restTocGroup, aggregateResult);

            Console.WriteLine($"Starting to create operation yamls");
            WriteOpeartions(targetApiVersionDir, aggregateResult, restTocGroup, 1);

            Console.WriteLine($"Starting to create component yamls");
            SplitHelper.WriteComponents(targetApiVersionDir, aggregateResult.Components);
        }

        public override void WriteToc(string targetApiVersionDir, RestTocGroup restTocGroup)
        {
            // todo: remove the toc.yml
            base.WriteToc(targetApiVersionDir, restTocGroup);
        }

        public override string GetOperationName(string operationName)
        {
            if (operationName.Contains('-'))
            {
                operationName = operationName.Split('-').Last();
            }
            if (operationName.Contains('.'))
            {
                operationName = operationName.Split('.').Last();
            }
            return operationName.FirstLetterToLower();
        }

        public override string GetOperationGroupName(string operationGroupName, string opeartionId)
        {
            var groupNames = new List<string>();
            var ids = opeartionId.Split('.');
            for (int i = 0; i < ids.Length - 1; i++)
            {
                groupNames.Add(ids[i]);
            }
            return string.Join(".", groupNames);
        }

        private void WriteOpeartions(string targetApiVersionDir, GraphAggregateResult aggregateResult, RestTocGroup restTocGroup, int depth)
        {
            if (restTocGroup != null)
            {
                foreach (var group in restTocGroup.Groups)
                {
                    if (depth > 1)
                    {
                        ResolveComponent(aggregateResult, MappingFile.ComponentPrefix + group.Key, group.Value);
                    }
                    WriteOpeartions(targetApiVersionDir, aggregateResult, group.Value, depth + 1);
                }
                if (restTocGroup.RestTocLeaves?.Count > 0)
                {
                    var aggregateOperations = restTocGroup.RestTocLeaves.OrderBy(p => p.MainOperation.Name);
                    foreach (var aggregateOperation in aggregateOperations)
                    {
                        SplitHelper.WriteOperations(targetApiVersionDir, aggregateOperation, MergeOperations, MergeOperations);
                    }
                }
            }
        }

        private OperationEntity MergeOperations(GraphAggregateEntity aggregateOperation)
        {
            var mainOperation = aggregateOperation.MainOperation;
            if (aggregateOperation.GroupedOperations?.Count > 0)
            {
                foreach (var groupedOperation in aggregateOperation.GroupedOperations)
                {
                    mainOperation.Paths.Add(groupedOperation.Paths[0]);
                }
            }
            return mainOperation;
        }

        private void ResolveComponent(GraphAggregateResult aggregateResult, string componentName, RestTocGroup restTocGroup)
        {
            foreach (var component in aggregateResult.Components)
            {
                if (string.Equals(component.Name, componentName, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"Find the component: {componentName}, then adding operations and links");
                    component.Operations = component.Operations ?? new List<string>();
                    component.Links = component.Links ?? new List<ResponseLinkEntity>();
                    if (restTocGroup.RestTocLeaves?.Count > 0)
                    {
                        foreach (var aggregateOperation in restTocGroup.RestTocLeaves)
                        {
                            if (!component.Operations.Any(o => o == aggregateOperation.MainOperation.Id))
                            {
                                component.Operations.Add(aggregateOperation.MainOperation.Id);
                            }

                            if (aggregateOperation.MainOperation.Responses?.Count > 0)
                            {
                                foreach (var response in aggregateOperation.MainOperation.Responses)
                                {
                                    if (response.ResponseLinks?.Count > 0)
                                    {
                                        foreach (var link in response.ResponseLinks)
                                        {
                                            if (!component.Links.Any(l => l.Key == link.Key))
                                            {
                                                if (aggregateResult.IdMappings.TryGetValue(link.OperationId, out var mainOperationId))
                                                {
                                                    component.Links.Add(new ResponseLinkEntity
                                                    {
                                                        Key = link.Key,
                                                        OperationId = mainOperationId
                                                    });
                                                }
                                                else
                                                {
                                                    Console.WriteLine($"Can not get link operation id for {link.OperationId}");
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    restTocGroup.Id = component.Id;
                    return;
                }
            }
            Console.WriteLine($"Cannot find the component: {componentName}");
        }

        private RestTocGroup GenerateGroupTree(List<string> groupNames, int index, RestTocGroup parentGroup)
        {
            if (index < groupNames.Count)
            {
                var childGroup = parentGroup[groupNames[index]];
                if (childGroup == null)
                {
                    childGroup = new RestTocGroup
                    {
                        Name = groupNames[index]
                    };

                    parentGroup[groupNames[index]] = childGroup;
                }
                return GenerateGroupTree(groupNames, index + 1, childGroup);
            }
            else
            {
                return parentGroup;
            }
        }

        private void GenerateToc(RestTocGroup restTocGroup, GraphAggregateResult aggregateResult)
        {
            foreach (var aggregateOperation in aggregateResult.AggregateOperations)
            {
                if (string.IsNullOrEmpty(aggregateOperation.MainOperation.GroupName))
                {
                    Console.WriteLine($"Should at least have one group for operation: {aggregateOperation.MainOperation.InternalOpeartionId}");
                }

                var groupNames = new List<string>();
                if (aggregateOperation.MainOperation.GroupName.Contains('.'))
                {
                    groupNames = aggregateOperation.MainOperation.GroupName.Split('.').ToList();
                }
                else
                {
                    groupNames.Add(aggregateOperation.MainOperation.GroupName);
                }

                var leafGroup = GenerateGroupTree(groupNames, 0, restTocGroup);
                if (leafGroup.RestTocLeaves == null)
                {
                    leafGroup.RestTocLeaves = new List<GraphAggregateEntity>();
                }
                leafGroup.RestTocLeaves.Add(aggregateOperation);
            }
        }

        private Tuple<string, IList<string>> CalculateAggregatenPath(List<string> paths)
        {
            paths.Sort();
            string mainPath = paths[0];
            paths.RemoveAt(0);
            return new Tuple<string, IList<string>>(mainPath, paths);
        }

        private GraphAggregateResult Aggregate(SplitSwaggerResult splitSwaggerResult)
        {
            var aggregatedResult = new GraphAggregateResult
            {
                Components = splitSwaggerResult.ComponentGroup.Components
            };

            foreach (var operationGroup in splitSwaggerResult.OperationGroups)
            {
                foreach (var operation in operationGroup.Operations)
                {
                    if (operation.IsMainOperation == null)
                    {
                        OperationEntity mainOperation;
                        List<OperationEntity> groupedOperations = new List<OperationEntity>();
                        if (operation.GroupedPaths.Count > 0)
                        {
                            var paths = new List<string>() { operation.Paths[0] };
                            paths.AddRange(operation.GroupedPaths);
                            var calculatedResult = CalculateAggregatenPath(paths);

                            var mainOperationId = FindOperationId(splitSwaggerResult, calculatedResult.Item1, operation.HttpVerb, out var foundMainOperation);
                            aggregatedResult.IdMappings[mainOperationId] = mainOperationId;
                            foundMainOperation.IsMainOperation = true;
                            mainOperation = foundMainOperation;

                            foreach (var path in calculatedResult.Item2)
                            {
                                var opeartionId = FindOperationId(splitSwaggerResult, path, operation.HttpVerb, out var foundOperation);
                                aggregatedResult.IdMappings[opeartionId] = mainOperationId;
                                foundOperation.IsMainOperation = false;
                                groupedOperations.Add(foundOperation);
                            }
                        }
                        else
                        {
                            operation.IsMainOperation = true;
                            aggregatedResult.IdMappings[operation.Id] = operation.Id;
                            mainOperation = operation;
                        }

                        aggregatedResult.AggregateOperations.Add(new GraphAggregateEntity
                        {
                            MainOperation = mainOperation,
                            GroupedOperations = groupedOperations,
                            IsFunctionOrAction = operation.IsFunctionOrAction
                        });
                    }
                }
            }

            return aggregatedResult;
        }

        private string FindOperationId(SplitSwaggerResult splitSwaggerResult, string path, string httpVerb, out OperationEntity foundOperation)
        {
            foreach (var operationGroup in splitSwaggerResult.OperationGroups)
            {
                foreach (var operation in operationGroup.Operations)
                {
                    if (string.Equals(path, operation.Paths[0], StringComparison.OrdinalIgnoreCase) && string.Equals(httpVerb, operation.HttpVerb))
                    {
                        foundOperation = operation;
                        return operation.Id;
                    }
                }
            }
            throw new Exception($"Can not find the operation id by x-ms-docs-grouped-path: {path} and httpVerb: {httpVerb}");
        }
    }
}
