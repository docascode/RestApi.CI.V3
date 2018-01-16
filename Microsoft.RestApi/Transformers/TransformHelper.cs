namespace Microsoft.RestApi.Transformers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.OpenApi.Models;

    public static class TransformHelper
    {
        public static string GetOperationSummary(string summary, string description)
        {
            var content = summary;
            if (!string.IsNullOrEmpty(description) && summary != description)
            {
                content = string.IsNullOrEmpty(summary) ? description : $"{summary} {description}";
            }
            return content;
        }

        public static IList<string> GetServerPaths(IList<OpenApiServer> servers)
        {
            var paths = new List<string>();
            if (servers != null)
            {
                foreach (var server in servers)
                {
                    var url = server.Url;
                    foreach (var variable in server.Variables)
                    {
                        url = url.Replace($"{{{variable.Key}}}", variable.Value.Default);
                    }
                    paths.Add(url);
                }
            }
            return paths;
        }

        public static string GetOperationId(IList<OpenApiServer> servers, string serviceName, string groupName, string operationName)
        {
            var serverPaths = GetServerPaths(servers);
            var defaultServerPath = serverPaths.FirstOrDefault();
            var defaultOperationId = $"{serviceName}.{ groupName}.{ operationName}";
            if (!string.IsNullOrEmpty(defaultServerPath))
            {
                var uri = new Uri(defaultServerPath);
                var basePath = uri.AbsolutePath?.Replace('/', '.').Trim('.');
                var hostWithBasePath = $"{uri.Host}.{basePath}".Replace(" ", "").Trim('.');
                defaultOperationId = $"{hostWithBasePath}.{serviceName}.{ groupName}.{operationName}";
            }
            return defaultOperationId.Replace(" ", "").Trim('.').ToLower();
        }

        public static string GetOperationGroupId(IList<OpenApiServer> servers, string serviceName, string groupName)
        {
            var serverPaths = GetServerPaths(servers);
            var defaultServerPath = serverPaths.FirstOrDefault();
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
    }
}
