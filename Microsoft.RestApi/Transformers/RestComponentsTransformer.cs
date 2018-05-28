namespace Microsoft.RestApi.Transformers
{
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.DocAsCode.Common;
    using Microsoft.OpenApi.Any;
    using Microsoft.OpenApi.Models;
    using Microsoft.RestApi.Models;

    using Newtonsoft.Json.Linq;

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
                        PropertyItems = properties.ToList(),
                        Example = GetComponentExample(schema.Value)
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

        private static string GetComponentExample(OpenApiSchema schema)
        {
            if(schema.Example == null)
            {
                return null;
            }
            return JsonUtility.ToJsonString(GetComponentExampleCore(schema.Example));
        }

        private static JToken GetComponentExampleCore(IOpenApiAny example)
        {
            if (example.AnyType == AnyType.Object)
            {
                var exampleObject = (OpenApiObject)example;
                JObject jObject = new JObject();
                foreach (var eo in exampleObject)
                {
                    var value = GetComponentExampleCore(eo.Value);
                    jObject.Add(new JProperty(eo.Key, value));
                }
                return jObject;
            }
            else if (example.AnyType == AnyType.Array)
            {
                var exampleArray = (OpenApiArray)example;
                JArray jArray = new JArray();
                foreach (var ea in exampleArray)
                {
                    jArray.Add(GetComponentExampleCore(ea));
                }
                return jArray;
            }
            else
            {
                return TransformHelper.GetValueFromPrimitiveType(example);
            }
        }
    }
}
