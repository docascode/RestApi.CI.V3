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
                RequestBodies = TransformerRequestBody(transformModel.Operation.Value),
                Paths = TransformePaths(transformModel.OpenApiDoc, transformModel.Operation.Key.ToString(), transformModel.Operation.Value),
                Examples = new List<ExampleEntity>(),
                Definitions = new List<DefinitionEntity>(),
                Securities = new List<SecurityEntity>()
            };
        }

        private static string FindThenPath(OpenApiDocument openApiDocument, OpenApiOperation openApiOperation)
        {
            foreach (var path in openApiDocument.Paths)
            {
                foreach(var operation in path.Value.Operations)
                {
                    if(openApiOperation.OperationId == operation.Value.OperationId)
                    {
                        return path.Key;
                    }
                }
            }
            throw new KeyNotFoundException($"Can not find the {openApiOperation.OperationId}");
        }

        private static IList<PathEntity> TransformePaths(OpenApiDocument openApiDocument, string method,  OpenApiOperation openApiOperation)
        {
            var paths = new List<PathEntity>();
            var path =  FindThenPath(openApiDocument, openApiOperation);
            var prefixPaths = TransformHelper.GetServerPaths(openApiDocument.Servers);
            foreach(var prefixPath in prefixPaths)
            {
                paths.Add(new PathEntity
                {
                    Content = $"{method.ToUpper()} {prefixPath}{path}"
                });
            }
            return paths;
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
                    In = openApiParameter.In.ToString(),
                    IsRequired = openApiParameter.Required,
                    IsReadOnly = openApiParameter.Schema?.ReadOnly ?? false,
                    Pattern = openApiParameter.Schema?.Pattern,
                    Format = openApiParameter.Schema?.Format,
                    Types = new List<PropertyTypeEntity> { new PropertyTypeEntity { Id = openApiParameter.Schema?.Type } }
                };
                parameterEntities.Add(parameterEntity);
            }
            return parameterEntities;
        }

        private static PropertyTypeEntity GetPropertyTypeFromOpenApiSchema(OpenApiSchema openApiSchema)
        {
            var type = new PropertyTypeEntity
            {
                Id = openApiSchema.Reference?.Id ?? openApiSchema.Type
            };

            if (openApiSchema.Type == "object")
            {
                if (openApiSchema.AdditionalProperties != null)
                {
                    type.IsDictionary = true;
                    type.AdditionalTypes = new List<IdentifiableEntity> { new IdentifiableEntity { Id = openApiSchema.AdditionalProperties.Type } };
                    return type;
                }
                if (openApiSchema.Properties != null)
                {
                    type.Id = openApiSchema.Title;
                    return type;
                }
                type.Id = openApiSchema.Reference?.Id;
                return type;
            }
            else if (openApiSchema.Type == "array")
            {
                type.Id = GetPropertyTypeFromOpenApiSchema(openApiSchema.Items).Id;
                type.IsArray = true;
                return type;
            }
            return type;
        }

        private static IList<ResponseContentTypeAndBodyEntity> GetResponseContentTypeAndBodies(OpenApiReference openApiReference, IDictionary<string, OpenApiMediaType> contents)
        {
            var responseContentTypeAndBodyEntities = new List<ResponseContentTypeAndBodyEntity>();
            foreach (var content in contents)
            {
                var propertyTypeEntities = new List<PropertyTypeEntity>();

                responseContentTypeAndBodyEntities.Add(new ResponseContentTypeAndBodyEntity
                {
                    ContentType = content.Key,
                    Types = new List<PropertyTypeEntity> { openApiReference?.Id != null ? new PropertyTypeEntity { Id = openApiReference?.Id  } : GetPropertyTypeFromOpenApiSchema(content.Value.Schema) }
                });
            }
            return responseContentTypeAndBodyEntities;
        }

        private static IList<ResponseEntity> TransformerResponses(OpenApiOperation openApiOperation)
        {
            var responseEntities = new List<ResponseEntity>();
            if(openApiOperation.Responses?.Count > 0)
            {
                foreach (var openApiResponse in openApiOperation.Responses)
                {
                    var bodies = GetResponseContentTypeAndBodies(openApiResponse.Value.Reference, openApiResponse.Value.Content);
                    var responseEntity = new ResponseEntity
                    {
                        Name = TransformHelper.GetStatusCodeString(openApiResponse.Key),
                        Description = openApiResponse.Value.Description,
                        ResponseContentTypeAndBodies = bodies.Count > 0 ? bodies : null,
                        ResponseeContentTypeAndHeaders = null // todo
                    };
                    responseEntities.Add(responseEntity);
                }
            }
            return responseEntities;
        }

        public static IList<PropertyEntity> GetPropertiesFromSchema(OpenApiSchema openApiSchema)
        {
            var properties = new List<PropertyEntity>();
            if(openApiSchema.Type == "object")
            {
                foreach (var property in openApiSchema.Properties)
                {
                    properties.Add(new PropertyEntity
                    {
                        Name = property.Key,
                        Types = new List<PropertyTypeEntity> { GetPropertyTypeFromOpenApiSchema(property.Value) }
                    });
                }
            }
            else
            {

            }
            
            foreach(var allOf in openApiSchema.AllOf)
            {
                properties.AddRange(GetPropertiesFromSchema(allOf));
            }
            return properties;
        }

        private static IList<RequestBodyEntity> TransformerRequestBody(OpenApiOperation openApiOperation)
        {
            var requestBodies = new List<RequestBodyEntity>();
            if (openApiOperation.RequestBody != null)
            {
                foreach(var requestContent in openApiOperation.RequestBody.Content)
                {
                    requestBodies.Add(new RequestBodyEntity
                    {
                        ContentType = requestContent.Key,
                        Description = openApiOperation.RequestBody.Description,
                        RequestBodyItems = new List<RequestBodyItemEntity> { new RequestBodyItemEntity
                        {
                            Name = openApiOperation.RequestBody.Reference?.Id ??  GetPropertyTypeFromOpenApiSchema(requestContent.Value.Schema).Id,
                            Description = "",
                            Parameters =  GetPropertiesFromSchema(requestContent.Value.Schema)
                        } }
                    });
                    
                   
                }
            }
            return requestBodies;
        }
    }
}
