namespace Microsoft.RestApi.Transformers
{
    using System.Collections.Generic;

    using Microsoft.OpenApi.Models;
    using Microsoft.RestApi.Models;
    using Microsoft.OpenApi.Any;
    using System.Linq;
    using System;

    public class RestOperationTransformer
    {
        public static OperationV3Entity Transform(TransformModel transformModel) //, ref Dictionary<string, OpenApiSchema> needExtractedSchemas, ref Dictionary<string, OpenApiSchema> needExtractedSchemas)
        {
            var allParameters = TransformHelper.TransformParameters(transformModel,
                GetRawParameters(transformModel).ToDictionary(k => k.Name, v => v));
            var allResponses = TransformHelper.TransformResponses(transformModel, transformModel.Operation.Value.Responses.ToDictionary(k => k.Key, v => v.Value));
            var requiredQueryUriParameters = allParameters.Where(p => p.IsRequired && p.In == "query").ToList();
            var optionalQueryUriParameters = allParameters.Where(p => !p.IsRequired && p.In == "query").ToList();
            return new OperationV3Entity
            {
                Id = transformModel.OperationId,
                Callbacks = null,
                Name = transformModel.OperationName,
                Service = transformModel.ServiceName,
                GroupName = transformModel.OperationGroupName,
                Summary = transformModel.Operation.Value.Summary,
                Description = transformModel.Operation.Value.Description,
                ApiVersion = transformModel.OpenApiDoc.Info.Version,
                IsDeprecated = transformModel.Operation.Value.Deprecated,
                HttpVerb = transformModel.Operation.Key.ToString().ToUpper(),
                Servers = TransformHelper.GetServerEnities(GetRawServers(transformModel)),
                Parameters = allParameters,
                Paths = TransformPaths(transformModel.OpenApiPath, transformModel.Operation.Value, requiredQueryUriParameters),
                // remove this for now
                // OptionalParameters = TransformOptionalParameters(optionalQueryUriParameters),
                Responses = allResponses,
                RequestBody = TransformHelper.TransformRequestBody(transformModel, new KeyValuePair<string, OpenApiRequestBody>("", transformModel.Operation.Value.RequestBody)),
                Securities = GetSecurities(transformModel),
                SeeAlso = TransformExternalDocs(transformModel.Operation.Value)
            };
        }

        private static IList<OpenApiParameter> GetRawParameters(TransformModel transformModel)
        {
            if (transformModel.Operation.Value.Parameters != null && transformModel.Operation.Value.Parameters.Any())
                return transformModel.Operation.Value.Parameters;

            return transformModel.OpenApiPath.Value.Parameters;
        }

        private static List<OperationV3Entity.Security> GetSecurities(TransformModel transformModel)
        {
            if (transformModel.Operation.Value.Security != null || transformModel.Operation.Value.Security.Count == 0) return null;

            return transformModel.Operation.Value.Security.SelectMany(s => s.Select(c => new OperationV3Entity.Security
            {
                Scopes = c.Value,
                SecurityId = Utility.GetId(transformModel.ServiceName, ComponentGroup.Securities.ToString(), c.Key.Reference.Id)
            })).ToList();
        }

        private static IList<OpenApiServer> GetRawServers(TransformModel transformModel)
        {
            if(transformModel.Operation.Value.Servers != null && transformModel.Operation.Value.Servers.Any())
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

        public static List<string> TransformPaths(KeyValuePair<string, OpenApiPathItem> defaultOpenApiPathItem, OpenApiOperation openApiOperation, IList<ParameterEntity> requiredQueryUriParameters)
        {
            var paths = new List<string> { defaultOpenApiPathItem.Key };

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
