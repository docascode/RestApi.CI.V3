namespace Microsoft.RestApi.Transformers
{
    using System.Collections.Generic;

    using Microsoft.OpenApi.Models;
    using Microsoft.RestApi.Models;
    using Microsoft.OpenApi.Any;

    public class RestOperationTransformer
    {

        public static OperationEntity Transform(TransformModel transformModel)
        {
            return new OperationEntity
            {
                Id = TransformHelper.GetOperationId(transformModel.OpenApiDoc.Servers, transformModel.ServiceName, transformModel.GroupName, transformModel.OperationName),
                Name = transformModel.OperationName,
                Service = transformModel.ServiceName,
                GroupName = transformModel.GroupName,
                Summary = TransformHelper.GetOperationSummary(transformModel.Operation.Value.Summary, transformModel.Operation.Value.Description),
                ApiVersion = transformModel.OpenApiDoc.Info.Version,
                IsDeprecated = transformModel.Operation.Value.Deprecated,
                // todo: need to transform
                IsPreview = TransformIsPreview(transformModel.OpenApiDoc),
                Responses = TransformerResponses(transformModel.Operation.Value),
                Parameters = TransformerParameters(transformModel.Operation.Value),
                RequestHeaders = new List<ParameterEntity>(),
                RequestBodies = new List<RequestBodyEntity>(),
                Paths = new List<PathEntity>(),
                Examples = new List<ExampleEntity>(),
                Definitions = new List<DefinitionEntity>(),
                Securities = new List<SecurityEntity>()
            };
        }

        private static bool TransformIsPreview(OpenApiDocument openApiDocument)
        {
            if (openApiDocument.Extensions.TryGetValue("x-ms-preview", out IOpenApiAny isPreview))
            {
                var result = isPreview as OpenApiBoolean;
                if(result != null)
                {
                    return result.Value;
                }
            }
            return false;
        }

        private static IList<ParameterEntity> TransformerParameters(OpenApiOperation openApiOperation)
        {
            var parameterEntities = new List<ParameterEntity>();
            foreach (var openApiParameter in openApiOperation.Parameters)
            {
                var parameterEntity = new ParameterEntity
                {
                    Name = openApiParameter.Name,
                    Description = openApiParameter.Description,
                    IsRequired = openApiParameter.Required,
                    IsReadOnly = openApiParameter.Schema?.ReadOnly ?? false,
                    Pattern = openApiParameter.Schema?.Pattern,
                    Format = openApiParameter.Schema?.Format
                };
                parameterEntities.Add(parameterEntity);
            }
            return parameterEntities;
        }

        private static void ResolveOpenApiSchema(OpenApiSchema openApiSchema)
        {

        }

        private static IList<BaseParameterTypeEntity> GetTypeEntities(IDictionary<string, OpenApiMediaType> contents)
        {
            var typesName = new List<BaseParameterTypeEntity>();
            foreach(var content in contents)
            {
                //content.Key
                var type = new BaseParameterTypeEntity
                {
                    IsArray = content.Value.Schema.Type == "Array",
                    IsDictionary = content.Value.Schema.Type == "Array",
                };
            }
            return typesName;
        }

        private static IList<ResponseEntity> TransformerResponses(OpenApiOperation openApiOperation)
        {
            var responseEntities = new List<ResponseEntity>();
            foreach(var openApiResponse in openApiOperation.Responses)
            {
                var responseEntity = new ResponseEntity
                {
                    Name = TransformHelper.GetStatusCodeString(openApiResponse.Key),
                    Description = openApiResponse.Value.Description,
                    Types = GetTypeEntities(openApiResponse.Value.Content),
                    TypesTitle = string.Empty
                };
                responseEntities.Add(responseEntity);
            }
            return responseEntities;
        }
    }
}
