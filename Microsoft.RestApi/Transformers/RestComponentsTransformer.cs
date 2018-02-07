namespace Microsoft.RestApi.Transformers
{
    using System.Collections.Generic;

    using Microsoft.RestApi.Models;

    public class RestComponentsTransformer
    {
        public static ComponentGroupEntity Transform(TransformModel transformModel)
        {
            var openApiDocument = transformModel.OpenApiDoc;

            var components = new List<ComponentEntity>();
            if (openApiDocument.Components?.Schemas != null)
            {
                foreach (var schema in openApiDocument.Components?.Schemas)
                {
                    var properties = TransformHelper.GetPropertiesFromSchema(schema.Value);

                    var component = new ComponentEntity
                    {
                        Id = TransformHelper.GetOperationId(transformModel.OpenApiDoc.Servers, transformModel.ServiceName, transformModel.GroupName, schema.Key),
                        Service = transformModel.ServiceName,
                        ApiVersion = transformModel.OpenApiDoc.Info.Version,
                        Name = schema.Key,
                        Description = schema.Value.Description ?? schema.Value.Title,
                        PropertyItems = properties
                    };

                    components.Add(component);
                }
            }

            var componentGroup = new ComponentGroupEntity
            {
                Id = TransformHelper.GetOperationGroupId(transformModel.OpenApiDoc.Servers, transformModel.ServiceName, transformModel.GroupName),
                Service = transformModel.ServiceName,
                ApiVersion = transformModel.OpenApiDoc.Info.Version,
                Components = components
            };
            return componentGroup;
        }
    }
}
