namespace Microsoft.RestApi.Transformers
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.RestApi.Models;

    public class RestComponentsTransformer
    {
        public static ComponentGroupEntity Transform(TransformModel transformModel)
        {
            var openApiDocument = transformModel.OpenApiDoc;

            var components = new List<ComponentEntity>();

            var componentGroupId = TransformHelper.GetComponentGroupId(transformModel.OpenApiDoc.Servers, transformModel.ServiceName, transformModel.ComponentGroupName);
            if (openApiDocument.Components?.Schemas != null)
            {
                foreach (var schema in openApiDocument.Components?.Schemas)
                {
                    var properties = TransformHelper.GetPropertiesFromSchema(schema.Value, componentGroupId);

                    var component = new ComponentEntity
                    {
                        Id = TransformHelper.GetComponentId(transformModel.OpenApiDoc.Servers, transformModel.ServiceName, transformModel.ComponentGroupName, schema.Key),
                        Service = transformModel.ServiceName,
                        ApiVersion = transformModel.OpenApiDoc.Info.Version,
                        Name = schema.Key,
                        Description = schema.Value.Description ?? schema.Value.Title,
                        PropertyItems = properties.ToList()
                    };

                    components.Add(component);
                }
            }

            var componentGroup = new ComponentGroupEntity
            {
                Id = componentGroupId,
                Service = transformModel.ServiceName,
                ApiVersion = transformModel.OpenApiDoc.Info.Version,
                Components = components
            };
            return componentGroup;
        }
    }
}
