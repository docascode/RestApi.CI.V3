namespace Microsoft.RestApi.Transformers
{
    using System.Collections.Generic;

    using Microsoft.OpenApi.Models;
    using Microsoft.RestApi.Models;
    using Microsoft.OpenApi.Any;
    using System.Linq;

    public class RestOperationTransformer
    {
        public static OperationEntity Transform(TransformModel transformModel)
        {
            var uriParameters = TransformUriParameters(transformModel.Operation.Value);
            return new OperationEntity
            {
                Id = TransformHelper.GetOperationId(transformModel.OpenApiDoc.Servers, transformModel.ServiceName, transformModel.GroupName, transformModel.OperationName),
                Name = transformModel.OperationName,
                Service = transformModel.ServiceName,
                GroupName = transformModel.GroupName,
                Summary = TransformHelper.GetOperationSummary(transformModel.Operation.Value.Summary, transformModel.Operation.Value.Description),
                ApiVersion = transformModel.OpenApiDoc.Info.Version,
                IsDeprecated = transformModel.Operation.Value.Deprecated,
                Operator = transformModel.Operation.Key.ToString(),
                Servers = TransformHelper.GetServerEnities(transformModel.OpenApiDoc.Servers),
                Paths = TransformPaths(transformModel.OpenApiDoc, transformModel.Operation.Value),
                OptionalParameters = TransformOptionalParameters(uriParameters.Where(prop => prop.In == "Query").ToList()),
                UriParameters = uriParameters,
                Responses = TransformResponses(transformModel.Operation.Value),
                RequestBodies = TransformRequestBody(transformModel.Operation.Value),
                Definitions = TransformDefinitions(transformModel.OpenApiDoc),
                Securities = new List<SecurityEntity>()
            };
        }

        private static IList<string> TransformPaths(OpenApiDocument openApiDocument, OpenApiOperation openApiOperation)
        {
            var paths = new List<string>();
            foreach (var path in openApiDocument.Paths)
            {
                foreach (var operation in path.Value.Operations)
                {
                    if (openApiOperation.OperationId == operation.Value.OperationId)
                    {
                        paths.Add(path.Key);
                    }
                }
            }

            if (openApiOperation.Extensions.TryGetValue("x-ms-additional-paths", out var openApiArrayAdditionalPaths))
            {
                if (openApiArrayAdditionalPaths is OpenApiArray additionalPaths)
                {
                    foreach (var additionalPath in additionalPaths)
                    {
                        if (additionalPath is OpenApiString pathStringValue)
                        {
                            paths.Add(pathStringValue.Value);
                        }
                    }
                }
            }
            return paths;
        }

        private static IList<ParameterEntity> TransformUriParameters(OpenApiOperation openApiOperation)
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
                    AllowEmptyValue = openApiParameter.AllowEmptyValue,
                    IsDeprecated = openApiParameter.Deprecated,
                    Pattern = openApiParameter.Schema?.Pattern,
                    Format = openApiParameter.Schema?.Format,
                    Types = new List<PropertyTypeEntity> { new PropertyTypeEntity { Id = openApiParameter.Schema?.Type } }
                };
                parameterEntities.Add(parameterEntity);
            }
            return parameterEntities;
        }

        private static IList<OptionalParameter> TransformOptionalParameters(IList<ParameterEntity> uriParameters)
        {
            var optionalParameters = new List<OptionalParameter>();
            foreach (var uriParameter in uriParameters)
            {
                optionalParameters.Add(new OptionalParameter
                {
                    Name = uriParameter.Name,
                    Value = $"{{{uriParameter.Name}}}"
                });
            }
            return optionalParameters;
        }

        private static PropertyTypeEntity GetPropertyTypeFromOpenApiSchema(OpenApiSchema openApiSchema)
        {
            var type = new PropertyTypeEntity
            {
                Id = openApiSchema.Reference?.Id ?? openApiSchema.Type
            };

            if (openApiSchema.Type == "object")
            {
                if (openApiSchema.Reference != null)
                {
                    type.Id = openApiSchema.Reference?.Id;
                    return type;
                }

                if (openApiSchema.AdditionalProperties != null)
                {
                    type.IsDictionary = true;
                    type.AdditionalTypes = new List<IdentifiableEntity> { new IdentifiableEntity { Id = openApiSchema.AdditionalProperties.Type } };
                    return type;
                }

                if (openApiSchema.Properties != null)
                {
                    type.Id = string.Empty;
                    return type;
                }

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
                    Types = new List<PropertyTypeEntity> { openApiReference?.Id != null ? new PropertyTypeEntity { Id = openApiReference?.Id } : GetPropertyTypeFromOpenApiSchema(content.Value.Schema) }
                });
            }
            return responseContentTypeAndBodyEntities;
        }

        private static IList<ResponseEntity> TransformResponses(OpenApiOperation openApiOperation)
        {
            var responseEntities = new List<ResponseEntity>();
            if (openApiOperation.Responses?.Count > 0)
            {
                foreach (var openApiResponse in openApiOperation.Responses)
                {
                    var bodies = GetResponseContentTypeAndBodies(openApiResponse.Value.Reference, openApiResponse.Value.Content);
                    var responseEntity = new ResponseEntity
                    {
                        Name = TransformHelper.GetStatusCodeString(openApiResponse.Key),
                        Description = openApiResponse.Value.Description,
                        ResponseContentTypeAndBodies = bodies.Count > 0 ? bodies : null,
                        ResponseHeades = null // todo
                    };
                    responseEntities.Add(responseEntity);
                }
            }
            return responseEntities;
        }

        public static IList<PropertyEntity> GetPropertiesFromSchema(OpenApiSchema openApiSchema)
        {
            var properties = new List<PropertyEntity>();
            if (openApiSchema.Type == "object")
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

            foreach (var allOf in openApiSchema.AllOf)
            {
                properties.AddRange(GetPropertiesFromSchema(allOf));
            }
            return properties;
        }

        private static IList<RequestBodyEntity> TransformRequestBody(OpenApiOperation openApiOperation)
        {
            var requestBodies = new List<RequestBodyEntity>();
            if (openApiOperation.RequestBody != null)
            {
                foreach (var requestContent in openApiOperation.RequestBody.Content)
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

        private static IList<DefinitionEntity> TransformDefinitions(OpenApiDocument openApiDocument)
        {
            var definitions = new List<DefinitionEntity>();
            if (openApiDocument.Components?.Schemas != null)
            {
                foreach (var schema in openApiDocument.Components?.Schemas)
                {
                    var properties = GetPropertiesFromSchema(schema.Value);

                    var definition = new DefinitionEntity
                    {
                        Name = schema.Key,
                        Description = schema.Value.Description ?? schema.Value.Title,
                        PropertyItems = properties
                    };

                    definitions.Add(definition);
                }
            }
            return definitions;
        }
    }
}
