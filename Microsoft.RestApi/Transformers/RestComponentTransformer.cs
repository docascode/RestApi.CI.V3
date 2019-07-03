namespace Microsoft.RestApi.Transformers
{
    using Microsoft.OpenApi.Models;
    using Microsoft.RestApi.Models;
    using System.Collections.Generic;

    public class RestComponentTransformer
    {

        public static List<PropertyTypeEntity> TransformSchemas(TransformModel transformModel, IDictionary<string, OpenApiSchema> schemas, ref Dictionary<string, OpenApiSchema> needExtractedSchemas, bool isComponent = false)
        {
            var types = new List<PropertyTypeEntity>();
            var schemaNames = new HashSet<string>();

            foreach (var schema in schemas)
            {
                var type = TransformHelper.ParseOpenApiSchema(schema.Key, schema.Value, transformModel, ref needExtractedSchemas, isComponent);
                type.Id = Utility.GetId(transformModel.ServiceId, transformModel.SourceFileName, ComponentGroup.Schemas.ToString(), schema.Key);
                type.Name = schema.Key;
                types.Add(type);

                schemaNames.Add(schema.Key);
            }

            var newNeedExtractedSchemas = needExtractedSchemas;
            while (newNeedExtractedSchemas?.Count > 0)
            {
                needExtractedSchemas = new Dictionary<string, OpenApiSchema>();
                foreach (var schema in newNeedExtractedSchemas)
                {
                    if (schemaNames.Contains(schema.Key))
                    {
                        TransformHelper.Errors.Add($"Please move schema definition in property \"{TransformHelper.GetRawPropertyName(schema.Key)}\" to schema components in file {transformModel.SourceFilePath}");
                    }
                    else
                    {
                        var type = TransformHelper.ParseOpenApiSchema(schema.Key, schema.Value, transformModel, ref needExtractedSchemas, isComponent);
                        type.Id = Utility.GetId(transformModel.ServiceId, transformModel.SourceFileName, ComponentGroup.Schemas.ToString(), schema.Key);
                        type.Name = schema.Key;
                        types.Add(type);
                    }

                    schemaNames.Add(schema.Key);
                }
                newNeedExtractedSchemas = needExtractedSchemas;
            }

            return types;
        }

        public static IEnumerable<OperationV3Entity> TransformCallbacks(TransformModel componentGroup, OpenApiDocument openApiDoc, Dictionary<string, OpenApiPathItem> needExtractedCallbacks, ref Dictionary<string, OpenApiSchema> needExtractedSchemas)
        {
            var operations = new List<OperationV3Entity>();
            foreach (var pathItem in needExtractedCallbacks)
            {
                foreach (var operation in pathItem.Value.Operations)
                {
                    componentGroup.ServiceName = componentGroup.ServiceName;
                    componentGroup.OperationId = Utility.GetId(componentGroup.ServiceId, componentGroup.SourceFileName, componentGroup.ComponentGroupName, operation.Value.OperationId);
                    componentGroup.OperationName = Utility.ExtractPascalNameByRegex(operation.Value.OperationId, componentGroup.MappingFile.NoSplitWords);
                    componentGroup.Operation = operation;
                    componentGroup.OpenApiPath = pathItem;

                    var ignoredCallbacks = new Dictionary<string, OpenApiPathItem>();
                    var ignoredLinkObjects = new Dictionary<string, List<OpenApiLink>>();
                    operations.Add(RestOperationTransformer.Transform(componentGroup, ref needExtractedSchemas, ref ignoredCallbacks, ref ignoredLinkObjects));
                }
            }
            return operations;
        }
    }
}
