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
            var allUriParameters = TransformUriParameters(transformModel.Operation.Value);
            var requiredQueryUriParameters = allUriParameters.Where(p => p.IsRequired && p.In == "Query").ToList();
            var optionalQueryUriParameters = allUriParameters.Where(p => !p.IsRequired && p.In == "Query").ToList();
            return new OperationEntity
            {
                Id = TransformHelper.GetOperationId(transformModel.OpenApiDoc.Servers, transformModel.ServiceName, transformModel.GroupName, transformModel.OperationName),
                Name = transformModel.OperationName,
                Service = transformModel.ServiceName,
                GroupName = transformModel.GroupName,
                Summary = TransformHelper.GetOperationSummary(transformModel.Operation.Value.Summary, transformModel.Operation.Value.Description),
                ApiVersion = transformModel.OpenApiDoc.Info.Version,
                IsDeprecated = transformModel.Operation.Value.Deprecated,
                HttpVerb = transformModel.Operation.Key.ToString(),
                Servers = TransformHelper.GetServerEnities(transformModel.OpenApiDoc.Servers),
                Paths = TransformPaths(transformModel.OpenApiDoc, transformModel.Operation.Value, requiredQueryUriParameters),
                OptionalParameters = TransformOptionalParameters(optionalQueryUriParameters),
                UriParameters = allUriParameters,
                //todo
                Responses = TransformResponses(transformModel.Operation.Value),
                RequestBodies = TransformRequestBody(transformModel.Operation.Value),
                Definitions = TransformDefinitions(transformModel.OpenApiDoc),

                Securities = TransformSecurity(transformModel.Operation.Value.Security.Count != 0 ? transformModel.Operation.Value.Security: transformModel.OpenApiDoc.SecurityRequirements),
                SeeAlsos = TransformExternalDocs(transformModel.Operation.Value)
            };
        }

        public static IList<string> TransformPaths(OpenApiDocument openApiDocument, OpenApiOperation openApiOperation, IList<ParameterEntity> requiredQueryUriParameters)
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
            if (requiredQueryUriParameters?.Count > 0)
            {
                var finalPaths = new List<string>();
                foreach (var path in paths)
                {
                    var parameters = requiredQueryUriParameters.Select(p => new { Name = p.Name, Value = $"{{{p.Name}}}" }).ToList();
                    if (path.Contains("?"))
                    {
                        var basepath = path.Split('?')[0];
                        var querystringString = path.Split('?')[1];
                        var querystrings = querystringString.Split('&');

                        var allQueryStrings = new List<string>();
                        foreach (var querystring in querystrings)
                        {
                            if (querystring.Contains("="))
                            {
                                var pairKey = querystring.Split('=')[0];
                                var pairValue = querystring.Split('=')[1];
                                if (!parameters.Any(p => p.Name == pairKey))
                                {
                                    parameters.Add(new { Name = pairKey, Value = pairValue });
                                }
                            }
                            else
                            {
                                allQueryStrings.Add(querystring);
                            }
                        }
                        allQueryStrings.AddRange(parameters.Select(p => $"{p.Name}={p.Value}"));
                        allQueryStrings.Sort();
                        finalPaths.Add(basepath + "?" + string.Join("&", allQueryStrings));
                    }
                    else
                    {
                        finalPaths.Add(path + "?" + string.Join("&", parameters.OrderBy(p => p.Name).Select(p => $"{p.Name}={p.Value}")));
                    }
                }
                return finalPaths;
            }
            return paths;
        }

        public static IList<ParameterEntity> TransformUriParameters(OpenApiOperation openApiOperation)
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
                    Types = new List<PropertyTypeEntity>
                    {
                        TransformHelper.ParseOpenApiSchema(openApiParameter.Schema)
                    }
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

        private static IList<ResponseContentTypeAndBodyEntity> GetResponseContentTypeAndBodies(OpenApiReference openApiReference, IDictionary<string, OpenApiMediaType> contents)
        {
            var responseContentTypeAndBodyEntities = new List<ResponseContentTypeAndBodyEntity>();
            foreach (var content in contents)
            {
                var propertyTypeEntities = new List<PropertyTypeEntity>();

                responseContentTypeAndBodyEntities.Add(new ResponseContentTypeAndBodyEntity
                {
                    ContentType = content.Key,
                    Types = new List<PropertyTypeEntity> { openApiReference?.Id != null ? new PropertyTypeEntity { Id = openApiReference?.Id } : TransformHelper.ParseOpenApiSchema(content.Value.Schema) }
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
                        Types = new List<PropertyTypeEntity> { TransformHelper.ParseOpenApiSchema(property.Value) }
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
                            Name = openApiOperation.RequestBody.Reference?.Id ??  TransformHelper.ParseOpenApiSchema(requestContent.Value.Schema).Id,
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

        public static IList<SecurityEntity> TransformSecurity(IList<OpenApiSecurityRequirement> openApiSecurityRequirements)
        {
            var securities = new List<SecurityEntity>();

            foreach (var openApiSecurityRequirement in openApiSecurityRequirements)
            {
                var keyValue = openApiSecurityRequirement.Single();
                var openApiSecurityScheme = keyValue.Key;
                var flows = new List<FlowEntity>();

                OpenApiOAuthFlow openApiOAuthFlow;
                if ((openApiOAuthFlow = openApiSecurityScheme.Flows.AuthorizationCode) != null)
                {
                    flows.Add(NewFlowEntity("authorizationCode", keyValue.Value, openApiOAuthFlow));
                }

                if ((openApiOAuthFlow = openApiSecurityScheme.Flows.ClientCredentials) != null)
                {
                    flows.Add(NewFlowEntity("clientCredentials", keyValue.Value, openApiOAuthFlow));
                }

                if ((openApiOAuthFlow = openApiSecurityScheme.Flows.Implicit) != null)
                {
                    flows.Add(NewFlowEntity("implicit", keyValue.Value, openApiOAuthFlow));
                }

                if ((openApiOAuthFlow = openApiSecurityScheme.Flows.Password) != null)
                {
                    flows.Add(NewFlowEntity("password", keyValue.Value, openApiOAuthFlow));
                }


                var securityEntity = new SecurityEntity
                {
                    Type = openApiSecurityScheme.Type.ToString(),
                    Description = openApiSecurityScheme.Description,
                    In = openApiSecurityScheme.In.ToString(),
                    Flows = flows
                };
                securities.Add(securityEntity);
            }

            return securities;
        }

        public static FlowEntity NewFlowEntity(string name, IList<string> scopeNames, OpenApiOAuthFlow openApiOAuthFlow)
        {
            var scopes = new List<SecurityScopeEntity>();
            foreach (var scopeName in scopeNames)
            {
                var scope = new SecurityScopeEntity
                {
                    Name = scopeName,
                    Description = openApiOAuthFlow.Scopes.Where(s => s.Key.Equals(scopeName)).Single().Value
                };
                scopes.Add(scope);
            }

            return new FlowEntity
            {
                Name = name,
                AuthorizationUrl = openApiOAuthFlow.AuthorizationUrl.ToString(),
                TokenUrl = openApiOAuthFlow.TokenUrl.ToString(),
                Scopes = scopes
            };
        }

        public static IList<SeeAlsoEntity> TransformExternalDocs(OpenApiOperation openApiOperation)
        {
            var seeAlsoEntities = new List<SeeAlsoEntity>();
            if (openApiOperation.ExternalDocs != null)
            {
                var seeAlsoEntity = new SeeAlsoEntity
                {
                    Description = openApiOperation.ExternalDocs.Description,
                    Url = openApiOperation.ExternalDocs.Url.ToString()
                };
                seeAlsoEntities.Add(seeAlsoEntity);
            }

            if (openApiOperation.Extensions.TryGetValue("x-ms-seeAlso", out var openApiSeeAlsos))
            {
                if (openApiSeeAlsos is OpenApiArray seeAlsos)
                {
                    foreach (var seeAlso in seeAlsos)
                    {
                        if (seeAlso is OpenApiObject seeAlsoObject)
                        {
                            var seeAlsoEntity = new SeeAlsoEntity();
                            if (seeAlsoObject.TryGetValue("description", out var description))
                            {
                                if (description is OpenApiString descriptionStringValue)
                                {
                                    seeAlsoEntity.Description = descriptionStringValue.Value;
                                }
                            }

                            if (seeAlsoObject.TryGetValue("url", out var url))
                            {
                                if (url is OpenApiString urlStringValue)
                                {
                                    seeAlsoEntity.Url = urlStringValue.Value;
                                }
                            }

                            seeAlsoEntities.Add(seeAlsoEntity);
                        }
                    }
                }
            }

            return seeAlsoEntities;
        }
    }
}
