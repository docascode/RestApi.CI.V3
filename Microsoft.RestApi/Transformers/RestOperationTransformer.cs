namespace Microsoft.RestApi.Transformers
{
    using System.Collections.Generic;

    using Microsoft.OpenApi.Models;
    using Microsoft.RestApi.Models;
    using Microsoft.OpenApi.Any;
    using Microsoft.OpenApi.Expressions;
    using Microsoft.OpenApi.Writers;
    using System.Linq;
    using System.IO;
    using System;
    using Newtonsoft.Json;

    public class RestOperationTransformer
    {
        public static OperationV3Entity Transform(
            TransformModel transformModel,
            ref Dictionary<string, OpenApiSchema> needExtractedSchemas,
            ref Dictionary<string, OpenApiPathItem> needExtractedCallbacks)
        {
            var allParameters = TransformParameters(transformModel,
                GetRawParameters(transformModel),
                ref needExtractedSchemas);

            var allResponses = TransformResponses(transformModel,
                transformModel.Operation.Value?.Responses?.ToDictionary(k => k.Key, v => v.Value),
                ref needExtractedSchemas);

            var requestBody = TransformRequestBody(transformModel,
                transformModel.Operation.Value.RequestBody,
                ref needExtractedSchemas);

            return new OperationV3Entity
            {
                Id = transformModel.OperationId,
                Callbacks = TransformCallbacks(transformModel, transformModel.Operation.Value.Callbacks, ref needExtractedCallbacks),
                Name = transformModel.OperationName,
                Service = transformModel.ServiceName,
                GroupName = transformModel.OperationGroupName,
                Summary = transformModel.Operation.Value.Summary,
                Description = transformModel.Operation.Value.Description,
                ApiVersion = transformModel.OpenApiDoc.Info.Version,
                IsDeprecated = transformModel.Operation.Value.Deprecated,
                HttpVerb = transformModel.Operation.Key.ToString().ToUpper(),
                Servers = GetServerEnities(GetRawServers(transformModel)),
                Parameters = allParameters,
                Paths = TransformPaths(transformModel.OpenApiPath, allParameters),
                Responses = allResponses,
                RequestBody = requestBody,
                Securities = GetSecurities(transformModel),
                SeeAlso = TransformExternalDocs(transformModel.Operation.Value)
            };
        }

        private static List<ExampleEntity> TransformExamples(TransformModel transformModel, IDictionary<string, OpenApiExample> examples)
        {
            if (examples == null || !examples.Any()) return null;
            var exampleEntities = new List<ExampleEntity>();
            foreach (var example in examples)
            {
                using (var stringWriter = new StringWriter())
                {
                    var openapiWriter = new OpenApiJsonWriter(stringWriter);
                    example.Value.Value.Write(openapiWriter);
                    exampleEntities.Add(new ExampleEntity
                    {
                        Name = example.Key,
                        Value = stringWriter.ToString(),
                        Description = example.Value.Description
                    });
                }
            }
            return exampleEntities;
        }

        private static List<CallbackEntity> TransformCallbacks(
            TransformModel transformModel,
            IDictionary<string, OpenApiCallback> callbacks,
            ref Dictionary<string, OpenApiPathItem> needExtractedCallbacks)
        {
            if (callbacks == null || !callbacks.Any())
            {
                return null;
            }
            
            if (needExtractedCallbacks == null)
            {
                needExtractedCallbacks = new Dictionary<string, OpenApiPathItem>();
            }

            var callbackEntities = new List<CallbackEntity>();
            foreach (var callback in callbacks)
            {
                var callbackEntity = new CallbackEntity { Name = callback.Key, CallbackOperations = new List<string>() };
                var callbackBaseId = callback.Value.Reference != null ? callback.Value.Reference.Id : transformModel.Operation.Value.OperationId + callback.Key;
                //var baseId = Utility.GetId(transformModel.ServiceName, ComponentGroup.Callbacks.ToString(), callbackId);
                if (callback.Value.PathItems?.Count > 0)
                {
                    foreach(var pathItem in callback.Value.PathItems)
                    {
                        needExtractedCallbacks[pathItem.Key.Expression] = pathItem.Value;

                        var pathBaseId = callbackBaseId + pathItem.Key;
                        if (pathItem.Value.Operations?.Count > 0)
                        {
                            foreach(var operation in pathItem.Value.Operations)
                            {
                                if (string.IsNullOrEmpty(operation.Value.OperationId))
                                {
                                    operation.Value.OperationId = operation.Key + pathBaseId;
                                }

                                callbackEntity.CallbackOperations.Add(Utility.GetId(transformModel.ServiceName, ComponentGroup.Callbacks.ToString(), operation.Value.OperationId));
                            }
                        }
                    }
                }
                callbackEntities.Add(callbackEntity);
            }

            return callbackEntities;
        }

        private static RequestBodyEntity TransformRequestBody(
            TransformModel transformModel,
            OpenApiRequestBody requestBody,
            ref Dictionary<string, OpenApiSchema> needExtractedSchemas)
        {
            //var requestBodies = new List<RequestBodyEntity>();
            if (requestBody == null) return null;

            var requestBodyEntity = new RequestBodyEntity
            {
                Description = requestBody.Description,
                isRequired = requestBody.Required,
                Bodies = new List<BodyEntity>()
            };

            foreach (var requestContent in requestBody.Content)
            {
                var body = new BodyEntity
                {
                    Examples = TransformExamples(transformModel, requestContent.Value.Examples),
                    MediaType = requestContent.Key,
                    Type = TransformHelper.ParseOpenApiSchema("requestBody", requestContent.Value.Schema, transformModel, ref needExtractedSchemas)
                };

                requestBodyEntity.Bodies.Add(body);
            }

            return requestBodyEntity;
        }

        public static List<ResponseEntity> TransformResponses(
            TransformModel transformModel,
            IDictionary<string, OpenApiResponse> responses,
            ref Dictionary<string, OpenApiSchema> needExtractedSchemas)
        {
            if (responses == null || !responses.Any()) return null;

            var responseEntities = new List<ResponseEntity>();

            foreach (var openApiResponse in responses)
            {
                var responseEntity = new ResponseEntity
                {
                    Description = openApiResponse.Value.Description,
                    Bodies = new List<BodyEntity>(),
                    StatusCode = openApiResponse.Key
                };

                if (openApiResponse.Value.Headers != null && openApiResponse.Value.Headers.Any())
                {
                    responseEntity.Headers = TransformResponseHeaders(transformModel, openApiResponse.Value.Headers, ref needExtractedSchemas);
                }

                foreach (var responseContent in openApiResponse.Value.Content)
                {
                    var body = new BodyEntity
                    {
                        Examples = TransformExamples(transformModel, responseContent.Value.Examples),
                        MediaType = responseContent.Key,
                        Type = TransformHelper.ParseOpenApiSchema("response", responseContent.Value.Schema, transformModel, ref needExtractedSchemas)
                    };

                    responseEntity.Bodies.Add(body);
                }
                responseEntities.Add(responseEntity);
            }

            return responseEntities;
        }

        private static List<ResponseHeaderEntity> TransformResponseHeaders(
            TransformModel transformModel,
            IDictionary<string, OpenApiHeader> responseHeaders,
            ref Dictionary<string, OpenApiSchema> needExtractedSchemas)
        {
            var responseHeaderEntities = new List<ResponseHeaderEntity>();
            foreach (var responseHeader in responseHeaders)
            {
                ResponseHeaderEntity responseHeaderEntity;
                {
                    responseHeaderEntity = new ResponseHeaderEntity
                    {
                        Name = responseHeader.Key,
                        AllowReserved = responseHeader.Value.AllowReserved,
                        Description = responseHeader.Value.Description,
                        Examples = TransformExamples(transformModel, responseHeader.Value.Examples),
                        IsRequired = responseHeader.Value.Required,
                        IsAnyOf = responseHeader.Value.Schema?.AnyOf?.Any() == true,
                        IsAllOf = responseHeader.Value.Schema?.AllOf?.Any() == true,
                        IsOneOf = responseHeader.Value.Schema?.OneOf?.Any() == true,
                        Nullable = responseHeader.Value.Schema?.Nullable ?? true,
                        IsDeprecated = responseHeader.Value.Deprecated,
                        Pattern = responseHeader.Value.Schema?.Pattern,
                        Format = responseHeader.Value.Schema?.Format,
                        Types = new List<PropertyTypeEntity>
                        {
                            TransformHelper.ParseOpenApiSchema(responseHeader.Key,  responseHeader.Value.Schema, transformModel, ref needExtractedSchemas)
                        }
                    };
                }

                responseHeaderEntities.Add(responseHeaderEntity);
            }
            return responseHeaderEntities;
        }

        private static IList<ParameterEntity> TransformParameters(
            TransformModel transformModel,
            IList<OpenApiParameter> parameters,
            ref Dictionary<string, OpenApiSchema> needExtractedSchemas)
        {
            if (parameters == null) return null;

            var parameterEntities = new List<ParameterEntity>();
            foreach (var openApiParameter in parameters)
            {
                parameterEntities.Add(TransformParameter(transformModel, openApiParameter, ref needExtractedSchemas));
            }
            return parameterEntities;
        }

        private static ParameterEntity TransformParameter(
            TransformModel transformModel,
            OpenApiParameter parameter,
            ref Dictionary<string, OpenApiSchema> needExtractedSchemas)
        {
            return new ParameterEntity
            {
                Name = parameter.Name,
                AllowReserved = parameter.AllowReserved,
                Description = parameter.Description,
                In = parameter.In?.ToString().ToLower(),
                Examples = TransformExamples(transformModel, parameter.Examples),
                IsRequired = parameter.Required,
                IsAnyOf = parameter.Schema?.AnyOf?.Any() == true,
                IsAllOf = parameter.Schema?.AllOf?.Any() == true,
                IsOneOf = parameter.Schema?.OneOf?.Any() == true,
                Nullable = parameter.Schema?.Nullable ?? true,
                IsDeprecated = parameter.Deprecated,
                Pattern = parameter.Schema?.Pattern,
                Format = parameter.Schema?.Format,
                Types = new List<PropertyTypeEntity>
                    {
                        TransformHelper.ParseOpenApiSchema(parameter.Name, parameter.Schema, transformModel, ref needExtractedSchemas)
                    }
            };
        }

        private static IList<ServerEntity> GetServerEnities(IList<OpenApiServer> apiServers)
        {
            if (apiServers == null) return null;
            var servers = new List<ServerEntity>();

            foreach (var apiServer in apiServers)
            {
                var name = apiServer.Url;
                var description = apiServer.Description;

                var serverVariables = new List<ServerVariableEntity>();
                if (apiServer.Variables != null)
                {
                    foreach (var variable in apiServer.Variables)
                    {
                        var serverVariable = new ServerVariableEntity
                        {
                            Name = variable.Key,
                            DefaultValue = variable.Value.Default,
                            Description = variable.Value.Description,
                            Values = variable.Value.Enum
                        };
                        serverVariables.Add(serverVariable);
                    }
                }

                servers.Add(new ServerEntity
                {
                    Name = name,
                    Description = description,
                    ServerVariables = serverVariables.Count > 0 ? serverVariables : null
                });
            }

            return servers;
        }

        private static IList<OpenApiParameter> GetRawParameters(TransformModel transformModel)
        {
            if (transformModel.Operation.Value?.Parameters?.Count > 0)
                return transformModel.Operation.Value.Parameters;

            return transformModel.OpenApiPath.Value?.Parameters;
        }

        private static List<SecurityEntity> GetSecurities(TransformModel transformModel)
        {
            if (transformModel.Operation.Value.Security == null || transformModel.Operation.Value.Security.Count == 0) return null;

            return transformModel.Operation.Value.Security.SelectMany(s => s.Select(c =>
            {
                var security = c.Key;
                var scopes = c.Value;

                return new SecurityEntity
                {
                    Name = security.Reference?.Id,
                    Description = security.Description,
                    ApiKeyName = security.Name,
                    BearerFormat = security.BearerFormat,
                    In = security.In.ToString().ToLower(),
                    OpenIdConnectUrl = security.OpenIdConnectUrl?.ToString(),
                    Scheme = security.Scheme,
                    Type = security.Type.ToString(),
                    Flows = GetFlow(security.Flows, scopes)
                };
            })).ToList();
        }


        private static List<FlowEntity> GetFlow(OpenApiOAuthFlows openApiFlows, IList<string> usedScopes)
        {
            if (openApiFlows == null) return null;

            var flows = new List<FlowEntity>();
            if (openApiFlows.AuthorizationCode != null)
            {
                var flow = TransformFlow(openApiFlows.AuthorizationCode, usedScopes);
                flow.Type = "AuthorizationCode";
                flows.Add(flow);
            }

            if (openApiFlows.ClientCredentials != null)
            {
                var flow = TransformFlow(openApiFlows.ClientCredentials, usedScopes);
                flow.Type = "ClientCredentials";
                flows.Add(flow);
            }

            if (openApiFlows.Implicit != null)
            {
                var flow = TransformFlow(openApiFlows.Implicit, usedScopes);
                flow.Type = "Implicit";
                flows.Add(flow);
            }

            if (openApiFlows.Password != null)
            {
                var flow = TransformFlow(openApiFlows.Password, usedScopes);
                flow.Type = "Password";
                flows.Add(flow);
            }

            return flows;
        }

        private static FlowEntity TransformFlow(OpenApiOAuthFlow openApiFlow, IList<string> usedScopes)
        {
            return new FlowEntity
            {
                AuthorizationUrl = openApiFlow.AuthorizationUrl.ToString(),
                RefreshUrl = openApiFlow.RefreshUrl.ToString(),
                TokenUrl = openApiFlow.TokenUrl.ToString(),
                Scopes = usedScopes != null ? openApiFlow.Scopes.Where(scope => usedScopes.Contains(scope.Key))
                    .Select(scope => new SecurityScopeEntity { Name = scope.Key, Description = scope.Value })
                    .ToList() : null
            };
        }

        private static IList<OpenApiServer> GetRawServers(TransformModel transformModel)
        {
            if (transformModel.Operation.Value.Servers != null && transformModel.Operation.Value.Servers.Any())
            {
                return transformModel.Operation.Value.Servers;
            }

            if (transformModel.OpenApiPath.Value.Servers != null && transformModel.OpenApiPath.Value.Servers.Any())
            {
                return transformModel.OpenApiPath.Value.Servers;
            }

            return transformModel.OpenApiDoc.Servers;
        }

        public static bool IsFunctionOrAction(OpenApiOperation openApiOperation)
        {
            if (openApiOperation.Extensions.TryGetValue("x-ms-docs-operation-type", out var openApiString))
            {
                if (openApiString is OpenApiString stringValue)
                {
                    if (!string.Equals(stringValue.Value, "operation", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static List<PathEntity> TransformPaths(KeyValuePair<string, OpenApiPathItem> defaultOpenApiPathItem, IList<ParameterEntity> parameters)
        {
            var pathEntities = new List<PathEntity>();

            var paths = defaultOpenApiPathItem.Key?.Split('?');
            var requiredQueryStrings = parameters.Where(p => p.IsRequired && p.In == "query");
            var requiredPath = paths[0];

            if (requiredQueryStrings.Any())
            {
                requiredPath = requiredPath + "?" + FormatPathQueryStrings(paths.Count() > 1 ? paths[1] : null, requiredQueryStrings);
            }

            pathEntities.Add(new PathEntity
            {
                Content = requiredPath,
                IsOptional = false
            });

            var allQueryStrings = parameters.Where(p => p.In == "query");
            var optionPath = paths[0];
            if (!allQueryStrings.All(p => p.IsRequired))
            {
                optionPath = optionPath + "?" + FormatPathQueryStrings(paths.Count() > 1 ? paths[1] : null, allQueryStrings);

                pathEntities.Add(new PathEntity
                {
                    Content = optionPath,
                    IsOptional = true
                });
            }
            return pathEntities;
        }

        private static string FormatPathQueryStrings(string initParameters, IEnumerable<ParameterEntity> queryParameters)
        {
            var queries = new List<string>();
            if (!string.IsNullOrEmpty(initParameters))
            {
                var initStrings = initParameters.Split('&').Select(p =>
                {
                    if (!queryParameters.Any(q => q.Name == p?.Split('=')[0]))
                    {
                        return p;
                    }
                    return null;
                });
                queries.AddRange(initStrings.Where(s => !string.IsNullOrEmpty(s)));
            }
            var queryStrings = queryParameters.Select(p =>
            {
                return $"{p.Name}={{{p.Name}}}";
            });
            queries.AddRange(queryStrings);
            return string.Join("&", queries);
        }

        public static IList<string> GetGroupedPaths(KeyValuePair<string, OpenApiPathItem> defaultOpenApiPathItem, OpenApiOperation openApiOperation)
        {
            var paths = new List<string>();

            if (defaultOpenApiPathItem.Value.Extensions.TryGetValue("x-ms-docs-grouped-path", out var openApiArrayAdditionalPaths))
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

        public static List<string> TransformExternalDocs(OpenApiOperation openApiOperation)
        {
            if (openApiOperation.ExternalDocs == null) return null;

            var seealsos = new List<string>();
            if (openApiOperation.ExternalDocs != null)
            {
                seealsos.Add($"[{openApiOperation.ExternalDocs.Description}]({openApiOperation.ExternalDocs.Url.ToString()})");
            }

            //if (openApiOperation.Extensions.TryGetValue("x-ms-seeAlso", out var openApiSeeAlsos))
            //{
            //    if (openApiSeeAlsos is OpenApiArray seeAlsos)
            //    {
            //        foreach (var seeAlso in seeAlsos)
            //        {
            //            if (seeAlso is OpenApiObject seeAlsoObject)
            //            {
            //                var seeAlsoEntity = new SeeAlsoEntity();
            //                if (seeAlsoObject.TryGetValue("description", out var description))
            //                {
            //                    if (description is OpenApiString descriptionStringValue)
            //                    {
            //                        seeAlsoEntity.Description = descriptionStringValue.Value;
            //                    }
            //                }

            //                if (seeAlsoObject.TryGetValue("url", out var url))
            //                {
            //                    if (url is OpenApiString urlStringValue)
            //                    {
            //                        seeAlsoEntity.Url = urlStringValue.Value;
            //                    }
            //                }

            //                seeAlsoEntities.Add(seeAlsoEntity);
            //            }
            //        }
            //    }
            //}

            return seealsos;
        }

        private static FlowEntity NewFlowEntity(string name, IList<string> scopeNames, OpenApiOAuthFlow openApiOAuthFlow)
        {
            var scopes = new List<SecurityScopeEntity>();
            foreach (var scopeName in scopeNames)
            {
                var flowScope = openApiOAuthFlow.Scopes.SingleOrDefault(s => s.Key.Equals(scopeName));
                if (flowScope.Value != null)
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
