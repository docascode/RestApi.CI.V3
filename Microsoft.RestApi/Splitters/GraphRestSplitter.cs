namespace Microsoft.RestApi.Splitters
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.YamlSerialization;
    using Microsoft.RestApi.Models;
    using Microsoft.RestApi.Transformers;

    public class GraphRestSplitter : RestSplitter
    {
        public static readonly YamlSerializer YamlSerializer = new YamlSerializer();
        public static readonly YamlDeserializer YamlDeserializer = new YamlDeserializer();

        public GraphRestSplitter(string sourceRootDir, string targetRootDir, string mappingFilePath, string outputDir, RestTransformerFactory transformerFactory)
            : base(sourceRootDir, targetRootDir, mappingFilePath, outputDir, transformerFactory)
        {
        }

        public override void WriteRestToc(StreamWriter writer, string subTocPrefix, List<string> tocLines, SortedDictionary<string, List<SwaggerToc>> subTocDict, string targetApiVersionDir)
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

                var finalTocList = GenerateFinalTocDict(pair.Value);
                var customSortedTocList = new Dictionary<string, List<SwaggerToc>>();
                if (MappingFile.FirstLevelTocOrders?.Count() > 0)
                {
                    foreach(var order in MappingFile.FirstLevelTocOrders)
                    {
                        if(finalTocList.TryGetValue(order, out var findTocs))
                        {
                            customSortedTocList.Add(order, findTocs);
                        }
                    }
                }
                foreach (var firstLevelGroupToc in finalTocList)
                {
                    if (!customSortedTocList.TryGetValue(firstLevelGroupToc.Key, out var findTocs))
                    {
                        customSortedTocList.Add(firstLevelGroupToc.Key, firstLevelGroupToc.Value);
                    }
                }

                foreach (var firstLevelGroupToc in customSortedTocList)
                {
                    var fistLevelTocPrefix = string.Empty;
                    if (!string.IsNullOrEmpty(firstLevelGroupToc.Key))
                    {
                        fistLevelTocPrefix = SplitHelper.IncreaseSharpCharacter(fistLevelTocPrefix);
                    }
                    var href = GetFirstLevelConceptual(firstLevelGroupToc.Key + ".md", targetApiVersionDir);

                    writer.WriteLine(!string.IsNullOrEmpty(href)
                               ? $"{subTocPrefix}##{subGroupTocPrefix} [{Utility.ExtractPascalNameByRegex(firstLevelGroupToc.Key)}]({href})"
                               : $"{subTocPrefix}##{subGroupTocPrefix} {Utility.ExtractPascalNameByRegex(firstLevelGroupToc.Key)}");

                    List<string> secondLevelSortOrders;
                    if (MappingFile.SecondLevelTocOrders == null || !MappingFile.SecondLevelTocOrders.TryGetValue(firstLevelGroupToc.Key, out secondLevelSortOrders))
                    {
                        secondLevelSortOrders = new List<string>();
                    }
                    var secondLevelGroupTocs = new List<SwaggerToc>();

                    if (secondLevelSortOrders.Count > 0)
                    {
                        secondLevelGroupTocs = firstLevelGroupToc.Value.OrderBy(x =>
                        {
                            var index = secondLevelSortOrders.IndexOf(x.Title);
                            if (index == -1)
                            {
                                return Int64.MaxValue;
                            }
                            return index;
                        }).ThenBy(s => s.Title).ToList();
                    }
                    else
                    {
                        secondLevelGroupTocs = firstLevelGroupToc.Value.OrderBy(s => s.Title).ToList();
                    }

                    foreach (var secondLevelGroupToc in secondLevelGroupTocs)
                    {
                        var primaryComponent = false;
                        if(secondLevelSortOrders.IndexOf(secondLevelGroupToc.Title) > 0)
                        {
                            primaryComponent = true;
                        }

                        var componentId = GetSecondLevelComponentId(pair.Value, MappingFile.ComponentPrefix + secondLevelGroupToc.Title.ToLower() + ".yml");
                        writer.WriteLine(!string.IsNullOrEmpty(componentId)
                            ? $"{subTocPrefix}##{subGroupTocPrefix}{fistLevelTocPrefix} [{Utility.ExtractPascalNameByRegex(secondLevelGroupToc.Title)}](xref:{componentId})"
                            : $"{subTocPrefix}##{subGroupTocPrefix}{fistLevelTocPrefix} {Utility.ExtractPascalNameByRegex(secondLevelGroupToc.Title)}");
                        if (secondLevelGroupToc.ChildrenToc.Count > 0)
                        {
                            foreach (var child in secondLevelGroupToc.ChildrenToc)
                            {
                                writer.WriteLine($"{subTocPrefix}###{subGroupTocPrefix}{fistLevelTocPrefix} [{Utility.ExtractPascalNameByRegex(child.Title)}](xref:{child.Uid})");
                            }

                            var componentFilePath = GetSecondLevelComponentPath(pair.Value, MappingFile.ComponentPrefix + secondLevelGroupToc.Title.ToLower() + ".yml", targetApiVersionDir);
                            if (!string.IsNullOrEmpty(componentFilePath) && File.Exists(componentFilePath))
                            {
                                if (primaryComponent)
                                {
                                    List<SwaggerToc> allTocs = new List<SwaggerToc>();
                                    foreach(var secondToc in secondLevelGroupTocs)
                                    {
                                        allTocs.AddRange(secondToc.ChildrenToc);
                                    }
                                    UpdateTheComponentYaml(componentFilePath, allTocs);
                                }
                                else
                                {
                                    UpdateTheComponentYaml(componentFilePath, secondLevelGroupToc.ChildrenToc);
                                }
                            }
                        }
                    }
                }
            }
        }

        private SortedDictionary<string, List<SwaggerToc>> GenerateFinalTocDict(List<SwaggerToc> swaggerTocList)
        {
            var finalTocDict = new SortedDictionary<string, List<SwaggerToc>>();

            foreach (var swaggerToc in swaggerTocList)
            {
                if (!swaggerToc.IsComponentGroup && swaggerToc.Title.Contains(MappingFile.TagSeparator))
                {
                    var groups = swaggerToc.Title.Split(new[] { MappingFile.TagSeparator }, StringSplitOptions.None);
                    if (groups.Count() == 2)
                    {
                        List<SwaggerToc> newSwaggerTocList;
                        if (!finalTocDict.TryGetValue(groups[0], out newSwaggerTocList))
                        {
                            newSwaggerTocList = new List<SwaggerToc>();
                            finalTocDict.Add(groups[0], newSwaggerTocList);
                        }
                        newSwaggerTocList.Add(new SwaggerToc(groups[1], swaggerToc.FilePath, swaggerToc.Uid, swaggerToc.ChildrenToc));
                    }
                    else
                    {
                        //warning
                    }
                }
                else
                {
                    //warning
                }
            }
            return finalTocDict;
        }

        private string GetFirstLevelConceptual(string conceptualFile, string targetApiVersionDir)
        {
            return SplitHelper.GenerateHref(Path.Combine(TargetRootDir, MappingFile.ConceptualFolder), conceptualFile, targetApiVersionDir);
        }

        private string GetSecondLevelComponentId(List<SwaggerToc> swaggerTocList, string componentFile)
        {
            foreach (var swaggerToc in swaggerTocList)
            {
                if (swaggerToc.IsComponentGroup)
                {
                    foreach(var childSwaggerToc in swaggerToc.ChildrenToc)
                    {
                        var fileName = childSwaggerToc.FilePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
                        var componentName = fileName.ToLower().Split(Path.DirectorySeparatorChar)?.Last();

                        if (!string.IsNullOrEmpty(componentName) && componentName.Equals(componentFile))
                        {
                            return childSwaggerToc.Uid;
                        }
                    }
                }
            }
            return null;
        }

        private string GetSecondLevelComponentPath(List<SwaggerToc> swaggerTocList, string componentFile, string targetApiVersionDir)
        {
            foreach (var swaggerToc in swaggerTocList)
            {
                if (swaggerToc.IsComponentGroup)
                {
                    foreach (var childSwaggerToc in swaggerToc.ChildrenToc)
                    {
                        var fileName = childSwaggerToc.FilePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
                        var componentName = fileName.ToLower().Split(Path.DirectorySeparatorChar)?.Last();

                        if (!string.IsNullOrEmpty(componentName) && componentName.Equals(componentFile))
                        {
                            return Path.Combine(targetApiVersionDir, fileName);
                        }
                    }
                }
            }
            return null;
        }

        public static void UpdateTheComponentYaml(string componentYamlFile, List<SwaggerToc> secondLevelTocs)
        {
            ComponentEntity componentEntity = null;
            using (var reader = new StreamReader(componentYamlFile))
            {
                componentEntity = YamlDeserializer.Deserialize<ComponentEntity>(reader);
                componentEntity.Operations = componentEntity.Operations ?? new List<string>();
                foreach (var secondLevelToc in secondLevelTocs)
                {
                    if (!componentEntity.Operations.Any(o => o == secondLevelToc.Uid))
                    {
                        componentEntity.Operations.Add(secondLevelToc.Uid);
                    }
                }
            }
            if (componentEntity != null)
            {
                using (var writer = new StreamWriter(componentYamlFile))
                {
                    writer.WriteLine("### YamlMime:RESTComponentV3");
                    YamlSerializer.Serialize(writer, componentEntity);
                }
            }
        }
    }
}
