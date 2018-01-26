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

        public static string GetOperationId(IList<OpenApiServer> servers, string serviceName, string groupName, string operationName)
        {
            var serverPaths = GetServerEnities(servers);
            var defaultServerPath = serverPaths.FirstOrDefault()?.Name;
            var defaultOperationId = $"{serviceName}.{groupName}.{operationName}";
            if (!string.IsNullOrEmpty(defaultServerPath))
            {
                var uri = new Uri(defaultServerPath);
                var basePath = uri.AbsolutePath?.Replace('/', '.').Trim('.');
                var hostWithBasePath = $"{uri.Host}.{basePath}".Replace(" ", "").Trim('.');
                defaultOperationId = $"{hostWithBasePath}.{serviceName}.{groupName}.{operationName}";
            }
            return defaultOperationId.Replace(" ", "").Trim('.').ToLower();
        }

        public static string GetOperationGroupId(IList<OpenApiServer> servers, string serviceName, string groupName)
        {
            var serverPaths = GetServerEnities(servers);
            var defaultServerPath = serverPaths.FirstOrDefault()?.Name;
            var defaultOperationId = $"{serviceName}.{ groupName}";
            if (!string.IsNullOrEmpty(defaultServerPath))
            {
                var uri = new Uri(defaultServerPath);
                var basePath = uri.AbsolutePath?.Replace('/', '.').Trim('.');
                var hostWithBasePath = $"{uri.Host}.{basePath}".Replace(" ", "").Trim('.');
                defaultOperationId = $"{hostWithBasePath}.{serviceName}.{ groupName}";
            }
            return defaultOperationId.Replace(" ", "").Trim('.').ToLower();
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

        private static IList<string> GetValueFromListAny(IList<IOpenApiAny> anyList)
        {
            var results = new List<string>();
            if (anyList is OpenApiArray anyValues)
            {
                foreach (var anyValue in anyValues)
                {
                    if (anyValue.AnyType == AnyType.Primitive)
                    {
                        results.Add(Convert.ToString(anyValue));
                    }
                }
            }
            return results;
        }
        public static PropertyTypeEntity ParseOpenApiSchema(OpenApiSchema openApiSchema)
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
                if (openApiSchema.Items.Enum?.Count > 0)
                {
                    type.Kind = "enum";
                    type.Id = openApiSchema.Items.Type;
                    type.Values = GetValueFromListAny(openApiSchema.Items.Enum);
                }
                else
                {
                    type.Id = ParseOpenApiSchema(openApiSchema.Items).Id;
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

        //private static PropertyTypeEntity ParseOpenApiSchema(OpenApiSchema openApiSchema)
        //{
        //    if (openApiSchema.Type == "object")
        //    {
        //        if (openApiSchema.Properties != null)
        //        {
        //            foreach (var property in openApiSchema.Properties)
        //            {
        //                property.Key = "",
        //                ParseOpenApiSchema(property.Value);
        //            }
        //        }
        //        else if (openApiSchema.AdditionalProperties != null)
        //        {

        //        }
        //    }
        //    else if (openApiSchema.Type == "array")
        //    {
        //        if (openApiSchema.Items.Enum != null)
        //        {
        //            openApiSchema.Items.Enum;
        //            openApiSchema.Items.Type;
        //        }

        //        openApiSchema.Items.Reference
        //    }
        //    else if (openApiSchema.Reference != null)
        //    {

        //    }
        //    else
        //    {
        //        openApiSchema.Type;
        //    }
        //}
    }
}
