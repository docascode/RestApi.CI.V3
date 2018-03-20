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
            var componentGroupId = TransformHelper.GetComponentGroupId(transformModel.OpenApiDoc.Servers, transformModel.ServiceName, transformModel.ComponentGroupName);
            var allUriParameters = TransformUriParameters(transformModel.Operation.Value, componentGroupId);
            var requiredQueryUriParameters = allUriParameters.Where(p => p.IsRequired && p.In == "query").ToList();
            var optionalQueryUriParameters = allUriParameters.Where(p => !p.IsRequired && p.In == "query").ToList();
            return new OperationEntity
            {
                Id = TransformHelper.GetOperationId(transformModel.OpenApiDoc.Servers, transformModel.ServiceName, transformModel.OperationGroupName, transformModel.OperationName),
                Name = transformModel.OperationName,
                Service = transformModel.ServiceName,
                GroupName = transformModel.OperationGroupName,
                Summary = TransformHelper.GetOperationSummary(transformModel.Operation.Value.Summary, transformModel.Operation.Value.Description),
                ApiVersion = transformModel.OpenApiDoc.Info.Version,
                IsDeprecated = transformModel.Operation.Value.Deprecated,
                HttpVerb = transformModel.Operation.Key.ToString().ToUpper(),
                Servers = TransformHelper.GetServerEnities(transformModel.OpenApiDoc.Servers),
                Paths = TransformPaths(transformModel.Path, transformModel.Operation.Value, requiredQueryUriParameters),
                // remove this for now
                // OptionalParameters = TransformOptionalParameters(optionalQueryUriParameters),
                RequestParameters = allUriParameters,
                Responses = TransformResponses(transformModel.Operation.Value, componentGroupId),
                RequestBodies = TransformRequestBody(transformModel.Operation.Value, componentGroupId),
                Securities = TransformSecurity(transformModel.Operation.Value.Security.Count != 0 ? transformModel.Operation.Value.Security : transformModel.OpenApiDoc.SecurityRequirements),
                SeeAlsos = TransformExternalDocs(transformModel.Operation.Value)
            };
        }

        public static IList<string> TransformPaths(string defaultPath, OpenApiOperation openApiOperation, IList<ParameterEntity> requiredQueryUriParameters)
        {
            var paths = new List<string> { defaultPath };

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

        public static IList<ParameterEntity> TransformUriParameters(OpenApiOperation openApiOperation, string componentGroupId)
        {
            var parameterEntities = new List<ParameterEntity>();
            foreach (var openApiParameter in openApiOperation.Parameters)
            {
                var parameterEntity = new ParameterEntity
                {
                    Name = openApiParameter.Name,
                    Description = openApiParameter.Description,
                    In = openApiParameter.In.ToString().ToLower(),
                    IsRequired = openApiParameter.Required,
                    IsReadOnly = openApiParameter.Schema?.ReadOnly ?? false,
                    Nullable = openApiParameter.Schema?.Nullable ?? true,
                    IsDeprecated = openApiParameter.Deprecated,
                    Pattern = openApiParameter.Schema?.Pattern,
                    Format = openApiParameter.Schema?.Format,
                    Types = new List<PropertyTypeEntity>
                    {
                        TransformHelper.ParseOpenApiSchema(openApiParameter.Schema, componentGroupId)
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

        public static IList<RequestBodyEntity> TransformRequestBody(OpenApiOperation openApiOperation, string componentGroupId)
        {
            var requestBodies = new List<RequestBodyEntity>();
            if (openApiOperation.RequestBody != null)
            {
                foreach (var requestContent in openApiOperation.RequestBody.Content)
                {
                    var requestBodySchemas = new List<RequestBodySchemaEntity>
                    {
                        // todo, if there exist oneof/anyof will add all them to the array.
                        new RequestBodySchemaEntity
                        {
                            Name = "default",
                            Properties = TransformHelper.GetPropertiesFromSchema(requestContent.Value.Schema, componentGroupId)
                        }
                    };

                    requestBodies.Add(new RequestBodyEntity
                    {
                        MediaType = requestContent.Key,
                        Description = openApiOperation.RequestBody.Description,
                        RequestBodySchemas = requestBodySchemas
                    });
                }
            }
            return requestBodies;
        }


        public static IList<ResponseEntity> TransformResponses(OpenApiOperation openApiOperation, string componentGroupId)
        {
            var responseEntities = new List<ResponseEntity>();
            if (openApiOperation.Responses?.Count > 0)
            {
                foreach (var openApiResponse in openApiOperation.Responses)
                {
                    var bodies = GetResponseMediaTypeAndBodies(openApiResponse.Value.Reference, openApiResponse.Value.Content, componentGroupId);
                    var responseEntity = new ResponseEntity
                    {
                        Name = TransformHelper.GetStatusCodeString(openApiResponse.Key),
                        StatusCode = openApiResponse.Key,
                        Description = openApiResponse.Value.Description,
                        ResponseMediaTypeAndBodies = bodies.Count > 0 ? bodies : null,
                        ResponseHeades = null // todo
                    };
                    responseEntities.Add(responseEntity);
                }
            }
            return responseEntities;
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
                    In = openApiSecurityScheme.In.ToString().ToLower(),
                    Flows = flows
                };
                securities.Add(securityEntity);
            }

            return securities;
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

        private static IList<ResponseMediaTypeAndBodyEntity> GetResponseMediaTypeAndBodies(OpenApiReference openApiReference, IDictionary<string, OpenApiMediaType> contents, string componentGroupId)
        {
            var responseMediaTypeAndBodyEntities = new List<ResponseMediaTypeAndBodyEntity>();
            foreach (var content in contents)
            {
                var propertyTypeEntities = new List<PropertyTypeEntity>();
                var propertyEntities = new List<PropertyEntity>();
                if (!string.IsNullOrEmpty(openApiReference?.Id) && openApiReference?.ReferenceV3?.Contains("responses") == false)
                {
                    propertyTypeEntities.Add(new PropertyTypeEntity
                    {
                        Id = TransformHelper.GetReferenceId(openApiReference, componentGroupId)
                    });
                }
                else
                {
                    var type = TransformHelper.ParseOpenApiSchema(content.Value.Schema, componentGroupId);
                    if(type.AnonymousChildren != null && type.AnonymousChildren.Count > 0)
                    {
                        propertyEntities.AddRange(type.AnonymousChildren);
                    }
                    else
                    {
                        type.AnonymousChildren = null;
                        propertyTypeEntities.Add(type);
                    }
                }

                responseMediaTypeAndBodyEntities.Add(new ResponseMediaTypeAndBodyEntity
                {
                    MediaType = content.Key,
                    ResponseBodyTypes = propertyTypeEntities.Count > 0 ? propertyTypeEntities : null,
                    ResponseBodySchemas = propertyEntities.Count > 0 ? propertyEntities : null
                });
            }
            return responseMediaTypeAndBodyEntities;
        }

        private static FlowEntity NewFlowEntity(string name, IList<string> scopeNames, OpenApiOAuthFlow openApiOAuthFlow)
        {
            var scopes = new List<SecurityScopeEntity>();
            foreach (var scopeName in scopeNames)
            {
                var flowScope = openApiOAuthFlow.Scopes.SingleOrDefault(s => s.Key.Equals(scopeName));
                if(flowScope.Value != null)
                {
                    var scope = new SecurityScopeEntity
                    {
                        Name = scopeName,
                        Description = flowScope.Value
                    };
                    scopes.Add(scope);
                }
            }

            return new FlowEntity
            {
                Name = name,
                AuthorizationUrl = openApiOAuthFlow.AuthorizationUrl?.ToString(),
                TokenUrl = openApiOAuthFlow.TokenUrl?.ToString(),
                Scopes = scopes
            };
        }
    }
}
