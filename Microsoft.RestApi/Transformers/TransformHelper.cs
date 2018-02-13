namespace Microsoft.RestApi.Transformers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.OpenApi.Models;
    using Microsoft.RestApi.Models;
    using Microsoft.OpenApi.Any;

    public static class TransformHelper
    {
        public static string GetOperationSummary(string summary, string description)
        {
            var content = summary;
            if (!string.IsNullOrEmpty(description) && !string.Equals(summary, description))
            {
                content = string.IsNullOrEmpty(summary) ? description : $"{summary} {description}";
            }
            return content;
        }

        public static IList<ServerEntity> GetServerEnities(IList<OpenApiServer> apiServers)
        {
            var servers = new List<ServerEntity>();
            if (apiServers != null)
            {
                foreach (var apiServer in apiServers)
                {
                    var name = apiServer.Url;
                    var serverVariables = new List<ServerVariableEntity>();
                    foreach (var variable in apiServer.Variables)
                    {
                        name = name.Replace($"{{{variable.Key}}}", variable.Value.Default);
                        var serverVariable = new ServerVariableEntity
                        {
                            Name = variable.Key,
                            DefaultValue = variable.Value.Default,
                            Description = variable.Value.Description,
                            Values = variable.Value.Enum
                        };
                        serverVariables.Add(serverVariable);
                    }
                    servers.Add(new ServerEntity
                    {
                        Name = name,
                        ServerVariables = serverVariables.Count > 0 ? serverVariables : null
                    });
                }
            }
            return servers;
        }

        private static string GetServiceId(IList<OpenApiServer> servers, string serviceName)
        {
            var serverPaths = GetServerEnities(servers);
            var defaultServerPath = serverPaths.FirstOrDefault()?.Name;
            var defaultServiceId = $"{serviceName}";
            if (!string.IsNullOrEmpty(defaultServerPath))
            {
                var uri = new Uri(defaultServerPath);
                var basePath = uri.AbsolutePath?.Replace('/', '.').Trim('.');
                var hostWithBasePath = $"{uri.Host}.{basePath}".Replace(" ", "").Trim('.');
                defaultServiceId = $"{hostWithBasePath}.{serviceName}";
            }
            return defaultServiceId.Replace(" ", "").Trim('.').ToLower();
        }

        public static string GetOperationId(IList<OpenApiServer> servers, string serviceName, string groupName, string operationName)
        {
            var serviceId = GetServiceId(servers, serviceName);
            var operationId = $"{serviceId}.{groupName}.{operationName}";
            return operationId.Replace(" ", "").Trim('.').ToLower();
        }

        public static string GetOperationGroupId(IList<OpenApiServer> servers, string serviceName, string groupName)
        {
            var serviceId = GetServiceId(servers, serviceName);
            var operationId = $"{serviceId}.{groupName}";
            return operationId.Replace(" ", "").Trim('.').ToLower();
        }

        public static string GetComponentId(IList<OpenApiServer> servers, string serviceName, string componentGroupName, string componentName)
        {
            return GetOperationId(servers, serviceName, componentGroupName, componentName);
        }

        public static string GetComponentGroupId(IList<OpenApiServer> servers, string serviceName, string componentGroupName)
        {
            return GetOperationGroupId(servers, serviceName, componentGroupName);
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

        private static string GetValueFromPrimitiveType(IOpenApiAny anyPrimitive)
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
            return results;
        }

        public static string GetReferenceId(OpenApiReference openApiReference, string componentGroupId)
        {
            if(openApiReference != null)
            {
                var referenceId = $"{componentGroupId}.{openApiReference.Id}";
                return referenceId.Replace(" ", "").Trim('.').ToLower();
                
            }
            return null;
        }

        public static PropertyTypeEntity ParseOpenApiSchema(OpenApiSchema openApiSchema, string componentGroupId)
        {
            var type = new PropertyTypeEntity
            {
                Id = GetReferenceId(openApiSchema.Reference, componentGroupId) ?? openApiSchema.Type
            };

            if (openApiSchema.Type == "object")
            {
                if (openApiSchema.Reference != null)
                {
                    type.Id = GetReferenceId(openApiSchema.Reference, componentGroupId);
                    return type;
                }

                if (openApiSchema.AdditionalProperties != null)
                {
                    type.IsDictionary = true;
                    type.AdditionalTypes = new List<IdentifiableEntity> { new IdentifiableEntity { Id = openApiSchema.AdditionalProperties.Type } };
                    return type;
                }

                if (openApiSchema.Properties?.Count > 0)
                {
                    type.AnonymousChildren = new List<PropertyEntity>();
                    type.AnonymousChildren.AddRange(GetPropertiesFromSchema(openApiSchema, componentGroupId));
                    type.Id = null;
                    return type;
                }

                return type;
            }
            else if (openApiSchema.Type == "array")
            {
                if (openApiSchema.Items.Enum?.Count > 0)
                {
                    type.Kind = "enum";
                    type.Id = openApiSchema.Items.Type;
                    type.Values = GetValueFromListAny(openApiSchema.Items.Enum);
                }
                else
                {
                    type.Id = ParseOpenApiSchema(openApiSchema.Items, componentGroupId).Id;
                }
                type.IsArray = true;
                return type;
            }
            else
            {
                if (openApiSchema.Enum?.Count > 0)
                {
                    type.Kind = "enum";
                    type.Values = GetValueFromListAny(openApiSchema.Enum);
                }
            }
            return type;
        }

        public static IList<PropertyEntity> GetPropertiesFromSchema(OpenApiSchema openApiSchema, string componentGroupId)
        {
            var properties = new List<PropertyEntity>();
            if (openApiSchema.Type == "object")
            {
                foreach (var property in openApiSchema.Properties)
                {
                    var types = new List<PropertyTypeEntity>();
                    if(property.Value.AnyOf?.Count() > 0)
                    {
                        foreach(var anyOf in property.Value.AnyOf)
                        {
                            types.Add(ParseOpenApiSchema(anyOf, componentGroupId));
                        }
                    }
                    else if(property.Value.OneOf?.Count() > 0)
                    {
                        foreach (var oneOf in property.Value.OneOf)
                        {
                            types.Add(ParseOpenApiSchema(oneOf, componentGroupId));
                        }
                    }
                    else
                    {
                        types.Add(ParseOpenApiSchema(property.Value, componentGroupId));
                    }

                    properties.Add(new PropertyEntity
                    {
                        Name = property.Key,
                        IsReadOnly = property.Value.ReadOnly,
                        IsDeprecated = property.Value.Deprecated,
                        AllowEmptyValue = property.Value.Nullable,
                        Description = property.Value.Description ?? property.Value.Title,
                        Pattern = property.Value.Pattern,
                        Format = property.Value.Format,
                        IsAnyOf = property.Value.AnyOf?.Count() > 0,
                        IsOneOf = property.Value.OneOf?.Count() > 0,
                        Types = types
                    });
                }
            }

            foreach (var allOf in openApiSchema.AllOf)
            {
                properties.AddRange(GetPropertiesFromSchema(allOf, componentGroupId));
            }
            return properties;
        }
    }
}
