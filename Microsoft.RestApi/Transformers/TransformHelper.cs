namespace Microsoft.RestApi.Transformers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.OpenApi.Models;
    using Microsoft.RestApi.Models;
    using Microsoft.OpenApi.Any;
    using Newtonsoft.Json;

    public static class TransformHelper
    {
        private static HashSet<string> PrimitiveTypes = new HashSet<string> { "integer", "number", "string", "boolean" };
        public static string GetOperationSummary(string summary, string description)
        {
            var content = summary;
            if (!string.IsNullOrEmpty(description) && !string.Equals(summary, description))
            {
                content = string.IsNullOrEmpty(summary) ? description : $"{summary} {description}";
            }
            return content;
        }

        public static IList<OperationV3Entity.ServerEntity> GetServerEnities(IList<OpenApiServer> apiServers)
        {
            if (apiServers == null) return new List<OperationV3Entity.ServerEntity>();
            var servers = new List<OperationV3Entity.ServerEntity>();

            foreach (var apiServer in apiServers)
            {
                var name = apiServer.Url;
                var description = apiServer.Description;

                var serverVariables = new List<OperationV3Entity.ServerVariableEntity>();
                foreach (var variable in apiServer.Variables)
                {
                    var serverVariable = new OperationV3Entity.ServerVariableEntity
                    {
                        Name = variable.Key,
                        DefaultValue = variable.Value.Default,
                        Description = variable.Value.Description,
                        Values = variable.Value.Enum
                    };
                    serverVariables.Add(serverVariable);
                }
                servers.Add(new OperationV3Entity.ServerEntity
                {
                    Name = name,
                    Description = description,
                    ServerVariables = serverVariables.Count > 0 ? serverVariables : null
                });
            }

            return servers;
        }

        public static string GetStatusCodeString(string statusCode)
        {
            switch (statusCode)
            {
                case "200":
                    return "200 OK";
                case "201":
                    return "201 Created";
                case "202":
                    return "202 Accepted";
                case "204":
                    return "204 No Content";
                case "400":
                    return "400 Bad Request";
                default:
                    return "Other Status Codes";
            }
        }

        public static string GetOpenApiPathItemKey(OpenApiDocument openApiDocument, OpenApiOperation openApiOperation)
        {
            foreach (var path in openApiDocument.Paths)
            {
                foreach (var operation in path.Value.Operations)
                {
                    if (openApiOperation.OperationId == operation.Value.OperationId)
                    {
                        return path.Key;
                    }
                }
            }
            throw new KeyNotFoundException($"Can not find the {openApiOperation.OperationId}");
        }

        public static string GetValueFromPrimitiveType(IOpenApiAny anyPrimitive)
        {
            if (anyPrimitive is OpenApiInteger integerValue)
            {
                return integerValue.Value.ToString();
            }
            if (anyPrimitive is OpenApiLong longValue)
            {
                return longValue.Value.ToString();
            }
            if (anyPrimitive is OpenApiFloat floatValue)
            {
                return floatValue.Value.ToString();
            }
            if (anyPrimitive is OpenApiDouble doubleValue)
            {
                return doubleValue.Value.ToString();
            }
            if (anyPrimitive is OpenApiString stringValue)
            {
                return stringValue.Value;
            }
            if (anyPrimitive is OpenApiByte byteValue)
            {
                return byteValue.Value.ToString();
            }
            if (anyPrimitive is OpenApiBinary binaryValue)
            {
                return binaryValue.Value.ToString();
            }
            if (anyPrimitive is OpenApiBoolean boolValue)
            {
                return boolValue.Value.ToString();
            }
            if (anyPrimitive is OpenApiDate dateValue)
            {
                return dateValue.Value.ToString();
            }
            if (anyPrimitive is OpenApiDateTime dateTimeValue)
            {
                return dateTimeValue.Value.ToString();
            }
            if (anyPrimitive is OpenApiPassword passwordValue)
            {
                return passwordValue.Value.ToString();
            }
            return string.Empty;
        }

        private static IList<string> GetValueFromListAny(IList<IOpenApiAny> anyList)
        {
            var results = new List<string>();
            foreach (var anyValue in anyList)
            {
                if (anyValue.AnyType == AnyType.Primitive)
                {
                    results.Add(GetValueFromPrimitiveType(anyValue));
                }
            }
            return results.OrderBy(r => r).ToList();
        }

        public static string GetReferenceId(OpenApiReference openApiReference, string componentGroupId)
        {
            if (openApiReference != null)
            {
                var referenceId = $"{componentGroupId}.{openApiReference.Id}";
                return referenceId.Replace(" ", "").Trim('.').ToLower();

            }
            return null;
        }

        public static IList<ParameterEntity> TransformParameters(TransformModel transformModel, IDictionary<string, OpenApiParameter> parameters, bool isComponent = false)
        {
            var parameterEntities = new List<ParameterEntity>();
            foreach (var openApiParameter in parameters)
            {
                parameterEntities.Add(TransformParameter(transformModel, openApiParameter, isComponent));
            }
            return parameterEntities;
        }

        public static ParameterEntity TransformParameter(TransformModel transformModel, KeyValuePair<string, OpenApiParameter> parameter, bool isComponent = false)
        {
            var parameterEntity = new ParameterEntity();
            var openApiParameter = parameter.Value;
            if (parameter.Value.Reference != null && !isComponent)
            {
                parameterEntity = new ParameterEntity
                {
                    ReferenceTo = Utility.GetId(transformModel.ServiceName, ComponentGroup.Parameters.ToString(), parameter.Value.Reference.Id)
                };
            }
            else
            {
                parameterEntity = new ParameterEntity
                {
                    Name = openApiParameter.Name,
                    AllowReserved = openApiParameter.AllowReserved,
                    Description = openApiParameter.Description,
                    In = openApiParameter.In.ToString().ToLower(),
                    Examples = TransformExamples(transformModel, openApiParameter.Examples),
                    IsRequired = openApiParameter.Required,
                    IsReadOnly = openApiParameter.Schema?.ReadOnly ?? false,
                    IsAnyOf = openApiParameter.Schema?.AnyOf?.Any() == true,
                    IsAllOf = openApiParameter.Schema?.AllOf?.Any() == true,
                    IsOneOf = openApiParameter.Schema?.OneOf?.Any() == true,
                    Nullable = openApiParameter.Schema?.Nullable ?? true,
                    IsDeprecated = openApiParameter.Deprecated,
                    Pattern = openApiParameter.Schema?.Pattern,
                    Format = openApiParameter.Schema?.Format,
                    Types = new List<PropertyTypeEntity>
                    {
                        ParseOpenApiSchema(openApiParameter.Schema, transformModel)
                    }
                };
            }

            if (isComponent)
            {
                parameterEntity.Service = transformModel.ServiceName;
                parameterEntity.Id = Utility.GetId(transformModel.ServiceName, ComponentGroup.Parameters.ToString(), parameter.Key);
                parameterEntity.ApiVersion = transformModel.OpenApiDoc.Info.Version;
            }

            return parameterEntity;
        }

        public static List<ResponseHeaderEntity> TransformResponseHeaders(TransformModel transformModel, IDictionary<string, OpenApiHeader> responseHeaders, bool isComponent = false)
        {
            var responseHeaderEntities = new List<ResponseHeaderEntity>();
            foreach (var responseHeader in responseHeaders)
            {
                ResponseHeaderEntity responseHeaderEntity;
                if (responseHeader.Value.Reference != null && !isComponent)
                {
                    responseHeaderEntity = new ResponseHeaderEntity
                    {
                        ReferenceTo = Utility.GetId(transformModel.ServiceName, ComponentGroup.ReponseHeaders.ToString(), responseHeader.Value.Reference.Id)
                    };
                }
                else
                {
                    responseHeaderEntity = new ResponseHeaderEntity
                    {
                        Name = responseHeader.Key,
                        AllowReserved = responseHeader.Value.AllowReserved,
                        Description = responseHeader.Value.Description,
                        Examples = TransformExamples(transformModel, responseHeader.Value.Examples),
                        IsRequired = responseHeader.Value.Required,
                        IsReadOnly = responseHeader.Value.Schema?.ReadOnly ?? false,
                        IsAnyOf = responseHeader.Value.Schema?.AnyOf?.Any() == true,
                        IsAllOf = responseHeader.Value.Schema?.AllOf?.Any() == true,
                        IsOneOf = responseHeader.Value.Schema?.OneOf?.Any() == true,
                        Nullable = responseHeader.Value.Schema?.Nullable ?? true,
                        IsDeprecated = responseHeader.Value.Deprecated,
                        Pattern = responseHeader.Value.Schema?.Pattern,
                        Format = responseHeader.Value.Schema?.Format,
                        Types = new List<PropertyTypeEntity>
                    {
                        ParseOpenApiSchema( responseHeader.Value.Schema, transformModel)
                    }
                    };
                }

                if (isComponent)
                {
                    responseHeaderEntity.Service = transformModel.ServiceName;
                    responseHeaderEntity.Id = Utility.GetId(transformModel.ServiceName, ComponentGroup.ReponseHeaders.ToString(), responseHeader.Key);
                    responseHeaderEntity.ApiVersion = transformModel.OpenApiDoc.Info.Version;
                }

                responseHeaderEntities.Add(responseHeaderEntity);
            }
            return responseHeaderEntities;
        }

        public static List<ResponseEntity> TransformResponses(TransformModel transformModel, IDictionary<string, OpenApiResponse> responses, bool isComponent = false)
        {
            if (responses == null || !responses.Any()) return null;

            var responseEntities = new List<ResponseEntity>();

            foreach (var openApiResponse in responses)
            {
                if (openApiResponse.Value.Reference != null && !isComponent)
                {
                    responseEntities.Add(new ResponseEntity
                    {
                        ReferenceTo = Utility.GetId(transformModel.ServiceName, ComponentGroup.Responses.ToString(), openApiResponse.Value.Reference.Id)
                    });
                }
                else
                {
                    var responseEntity = new ResponseEntity
                    {
                        Description = openApiResponse.Value.Description,
                        Bodies = new List<BodyEntity>()
                    };

                    if (isComponent)
                    {
                        responseEntity.Service = transformModel.ServiceName;
                        responseEntity.Name = openApiResponse.Key;
                        responseEntity.Id = Utility.GetId(transformModel.ServiceName, ComponentGroup.Responses.ToString(), openApiResponse.Key);
                        responseEntity.ApiVersion = transformModel.OpenApiDoc.Info.Version;
                    }
                    else
                    {
                        responseEntity.StatusCode = openApiResponse.Key;
                    }

                    if (openApiResponse.Value.Headers != null && openApiResponse.Value.Headers.Any())
                    {
                        responseEntity.Headers = TransformResponseHeaders(transformModel, openApiResponse.Value.Headers);
                    }

                    foreach (var responseContent in openApiResponse.Value.Content)
                    {
                        var body = new BodyEntity
                        {
                            Examples = TransformExamples(transformModel, responseContent.Value.Examples),
                            MediaType = responseContent.Key,
                            Type = ParseOpenApiSchema(responseContent.Value.Schema, transformModel)
                        };

                        responseEntity.Bodies.Add(body);
                    }
                    responseEntities.Add(responseEntity);
                }
            }

            return responseEntities;
        }

        public static List<RequestBodyEntity> TransformRequestBodies(TransformModel transformModel, IDictionary<string, OpenApiRequestBody> requestBodies, bool isComponent = false)
        {
            var requestEntities = new List<RequestBodyEntity>();
            foreach (var openApiRequestBody in requestBodies)
            {
                requestEntities.Add(TransformRequestBody(transformModel, openApiRequestBody, isComponent));
            }

            return requestEntities;
        }

        public static RequestBodyEntity TransformRequestBody(TransformModel transformModel, KeyValuePair<string, OpenApiRequestBody> requestBody, bool isComponent = false)
        {
            //var requestBodies = new List<RequestBodyEntity>();
            var sourceRequestBody = requestBody.Value;
            if (sourceRequestBody == null) return null;
            if (sourceRequestBody.Reference != null && !isComponent)
            {
                return new RequestBodyEntity
                {
                    ReferenceTo = Utility.GetId(transformModel.ServiceName, ComponentGroup.RequestBodies.ToString(), sourceRequestBody.Reference.Id)
                };
            }

            var requestBodyEntity = new RequestBodyEntity
            {
                Description = sourceRequestBody.Description,
                isRequired = sourceRequestBody.Required,
                Bodies = new List<BodyEntity>()
            };

            foreach (var requestContent in sourceRequestBody.Content)
            {
                var body = new BodyEntity
                {
                    Examples = TransformExamples(transformModel, requestContent.Value.Examples),
                    MediaType = requestContent.Key,
                    Type = ParseOpenApiSchema(requestContent.Value.Schema, transformModel)
                };

                requestBodyEntity.Bodies.Add(body);
            }

            if (isComponent)
            {
                requestBodyEntity.Service = transformModel.ServiceName;
                requestBodyEntity.Name = requestBody.Key;
                requestBodyEntity.Id = Utility.GetId(transformModel.ServiceName, ComponentGroup.RequestBodies.ToString(), requestBody.Key);
                requestBodyEntity.ApiVersion = transformModel.OpenApiDoc.Info.Version;
            }

            return requestBodyEntity;
        }

        public static List<ExampleEntity> TransformExamples(TransformModel transformModel, IDictionary<string, OpenApiExample> examples, bool isComponent = false)
        {
            if (examples == null || !examples.Any()) return null;
            var exampleEntities = new List<ExampleEntity>();
            foreach (var example in examples)
            {
                ExampleEntity exampleEntity;
                if (example.Value.Reference != null && !isComponent)
                {
                    exampleEntity = new ExampleEntity
                    {
                        ReferenceTo = Utility.GetId(transformModel.ServiceName, ComponentGroup.Examples.ToString(), example.Value.Reference.Id)
                    };
                }
                else
                {
                    exampleEntity = new ExampleEntity
                    {
                        Name = example.Key,
                        Value = JsonConvert.SerializeObject(example.Value.Value),
                        Description = example.Value.Description
                    };
                }

                if (isComponent)
                {
                    exampleEntity.Service = transformModel.ServiceName;
                    exampleEntity.Id = Utility.GetId(transformModel.ServiceName, ComponentGroup.Examples.ToString(), example.Key);
                    exampleEntity.ApiVersion = transformModel.OpenApiDoc.Info.Version;
                }

                exampleEntities.Add(exampleEntity);
            }
            return exampleEntities;
        }

        public static List<PropertyTypeEntity> TransformSchemas(TransformModel transformModel, IDictionary<string, OpenApiSchema> schemas, bool isComponent = false)
        {
            var types = new List<PropertyTypeEntity>();
            foreach (var shcema in schemas)
            {
                var type = ParseOpenApiSchema(shcema.Value, transformModel, isComponent);
                type.Id = Utility.GetId(transformModel.ServiceName, ComponentGroup.Schemas.ToString(), shcema.Key);
                type.Name = shcema.Key;
                types.Add(type);
            }

            return types;
        }

        internal static List<SecurityEntity> TransformSecurities(TransformModel transformModel, IDictionary<string, OpenApiSecurityScheme> securitySchemes)
        {
            var securities = new List<SecurityEntity>();
            foreach (var openApiSeurity in securitySchemes)
            {
                var value = openApiSeurity.Value;
                var security = new SecurityEntity
                {
                    Id = Utility.GetId(transformModel.ServiceName, ComponentGroup.Securities.ToString(), openApiSeurity.Key),
                    Name = openApiSeurity.Key,
                    ApiKeyName = value.Name,
                    Type = value.Type.ToString(),
                    BearerFormat = value.BearerFormat,
                    Description = value.Description,
                    Service = transformModel.ServiceName,
                    ApiVersion = transformModel.OpenApiDoc.Info.Version,
                    Scheme = value.Scheme,
                    OpenIdConnectUrl = value.OpenIdConnectUrl?.ToString(),
                    In = value.In.ToString().ToLower(),
                    Flows = GetFlow(value.Flows)
                };
                securities.Add(security);
            }

            return securities;
        }

        private static List<FlowEntity> GetFlow(OpenApiOAuthFlows openApiFlows)
        {
            if (openApiFlows == null) return null;

            var flows = new List<FlowEntity>();
            if (openApiFlows.AuthorizationCode != null)
            {
                var flow = TransformFlow(openApiFlows.AuthorizationCode);
                flow.Type = "AuthorizationCode";
                flows.Add(flow);
            }

            if (openApiFlows.ClientCredentials != null)
            {
                var flow = TransformFlow(openApiFlows.ClientCredentials);
                flow.Type = "ClientCredentials";
                flows.Add(flow);
            }

            if (openApiFlows.Implicit != null)
            {
                var flow = TransformFlow(openApiFlows.Implicit);
                flow.Type = "Implicit";
                flows.Add(flow);
            }

            if (openApiFlows.Password != null)
            {
                var flow = TransformFlow(openApiFlows.Password);
                flow.Type = "Password";
                flows.Add(flow);
            }

            return flows;
        }

        private static FlowEntity TransformFlow(OpenApiOAuthFlow openApiFlow)
        {
            return new FlowEntity
            {
                AuthorizationUrl = openApiFlow.AuthorizationUrl.ToString(),
                RefreshUrl = openApiFlow.RefreshUrl.ToString(),
                TokenUrl = openApiFlow.TokenUrl.ToString(),
                Scopes = openApiFlow.Scopes.Select(scope => new SecurityScopeEntity { Name = scope.Key, Description = scope.Value}).ToList()
            };
        }

        public static PropertyTypeEntity ParseOpenApiSchema(OpenApiSchema openApiSchema, TransformModel transformModel, bool isComponent = false)
        {
            var type = new PropertyTypeEntity();
            type.ReferenceTo = openApiSchema.Type;
            if (openApiSchema.Type == "object" || openApiSchema.Properties?.Count > 0)
            {
                if (openApiSchema.Reference != null && !isComponent)
                {
                    type.ReferenceTo = Utility.GetId(transformModel.ServiceName, ComponentGroup.Schemas.ToString(), openApiSchema.Reference.Id);
                }
                else if (openApiSchema.AdditionalProperties != null)
                {
                    type.IsDictionary = true;
                    type.AdditionalTypes = new List<string> { GetTypeReferenceId(openApiSchema.AdditionalProperties.Type, transformModel) };
                }
                else if (openApiSchema.Properties?.Count > 0)
                {
                    type.Properties = new List<PropertyEntity>();
                    type.Properties.AddRange(GetPropertiesFromSchema(openApiSchema, transformModel));
                    type.ReferenceTo = null;
                }
            }
            else if (openApiSchema.Type == "array")
            {
                if (openApiSchema.Items?.Enum?.Count > 0)
                {
                    type.Kind = "enum";
                    type.ReferenceTo = openApiSchema.Items.Type;
                    type.Values = GetValueFromListAny(openApiSchema.Items.Enum).ToList();
                }
                else
                {
                    type.ReferenceTo = openApiSchema.Items?.Reference != null ?
                        Utility.GetId(transformModel.ServiceName, ComponentGroup.Schemas.ToString(), openApiSchema.Items.Reference.Id) :
                        openApiSchema.Items?.Type;
                    if (type.ReferenceTo == "array" || type.ReferenceTo == "object")
                    {
                        //throw new Exception($"Type {type.ReferenceTo} is not defined.");
                    }
                }
                type.IsArray = true;
            }
            else
            {
                if (openApiSchema.Enum?.Count > 0)
                {
                    type.Kind = "enum";
                    type.Values = GetValueFromListAny(openApiSchema.Enum).ToList();
                }
            }

            if (isComponent)
            {
                type.Service = transformModel.ServiceName;
                type.ApiVersion = transformModel.OpenApiDoc.Info.Version;
            }

            if (type.ReferenceTo != null && PrimitiveTypes.Contains(type.ReferenceTo))
            {
                type.IsPrimitiveTypes = true;
            }

            return type;
        }

        private static string GetTypeReferenceId(string type, TransformModel transformModel)
        {
            if (PrimitiveTypes.Contains(type))
                return type;
            return Utility.GetId(transformModel.ServiceName, ComponentGroup.Schemas.ToString(), type);
        }

        public static IList<PropertyEntity> GetPropertiesFromSchema(OpenApiSchema openApiSchema, TransformModel transformModel)
        {
            var properties = new List<PropertyEntity>();
            var required = openApiSchema.Required;
            if (openApiSchema.Type == "object" || openApiSchema.Properties?.Count > 0)
            {
                foreach (var property in openApiSchema.Properties)
                {
                    var types = new List<PropertyTypeEntity>();
                    if (property.Value.AnyOf?.Count() > 0)
                    {
                        foreach (var anyOf in property.Value.AnyOf)
                        {
                            types.Add(ParseOpenApiSchema(anyOf, transformModel));
                        }
                    }
                    else if (property.Value.OneOf?.Count() > 0)
                    {
                        foreach (var oneOf in property.Value.OneOf)
                        {
                            types.Add(ParseOpenApiSchema(oneOf, transformModel));
                        }
                    }
                    else if (property.Value.AllOf?.Count() > 0)
                    {
                        foreach (var allOf in property.Value.AllOf)
                        {
                            types.Add(ParseOpenApiSchema(allOf, transformModel));
                        }
                    }
                    else if (property.Value.Not != null)
                    {
                        types.Add(ParseOpenApiSchema(property.Value.Not, transformModel));
                    }
                    else
                    {
                        types.Add(ParseOpenApiSchema(property.Value, transformModel));
                    }

                    properties.Add(new PropertyEntity
                    {
                        Name = property.Key,
                        IsReadOnly = property.Value.ReadOnly,
                        IsRequired = required.Contains(property.Key),
                        IsDeprecated = property.Value.Deprecated,
                        Nullable = property.Value.Nullable,
                        Description = property.Value.Description ?? property.Value.Title,
                        Pattern = property.Value.Pattern,
                        Format = property.Value.Format,
                        IsAnyOf = property.Value.AnyOf?.Count() > 0,
                        IsOneOf = property.Value.OneOf?.Count() > 0,
                        IsAllOf = property.Value.AllOf?.Count() > 0,
                        IsNot = property.Value.Not != null,
                        Types = types
                    });
                }
            }
            else if (openApiSchema.Enum?.Count > 0)
            {
                var enumValues = GetValueFromListAny(openApiSchema.Enum);
                foreach (var enumValue in enumValues)
                {
                    properties.Add(new PropertyEntity
                    {
                        Name = enumValue,
                        Types = new List<PropertyTypeEntity> { new PropertyTypeEntity { ReferenceTo = openApiSchema.Type } }
                    });
                }
            }

            return properties;
        }
    }
}
