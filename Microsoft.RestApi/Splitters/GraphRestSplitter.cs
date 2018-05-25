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

        public override void ResolveComponent(string componentYamlFile, RestTocGroup restTocGroup)
        {
            ComponentEntity componentEntity = null;
            using (var reader = new StreamReader(componentYamlFile))
            {
                componentEntity = YamlDeserializer.Deserialize<ComponentEntity>(reader);
                componentEntity.Operations = componentEntity.Operations ?? new List<string>();
                componentEntity.Links = componentEntity.Links ?? new List<ResponseLinkEntity>();

                if (restTocGroup.OperationOrComponents?.Count > 0)
                {
                    foreach (var operation in restTocGroup.OperationOrComponents)
                    {
                        if (!componentEntity.Operations.Any(o => o == operation.Id))
                        {
                            componentEntity.Operations.Add(operation.Id);
                        }
                        if (operation.OperationInfo != null && operation.OperationInfo.Responses?.Count > 0)
                        {
                            foreach (var response in operation.OperationInfo.Responses)
                            {
                                if (response.ResponseLinks?.Count > 0)
                                {
                                    foreach (var link in response.ResponseLinks)
                                    {
                                        if (!componentEntity.Links.Any(l => l.Key == link.Key))
                                        {
                                            componentEntity.Links.Add(new ResponseLinkEntity
                                            {
                                                Key = link.Key,
                                                OperationId = link.OperationId
                                            });
                                        }
                                    }
                                }
                            }
                        }
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
