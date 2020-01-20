namespace Microsoft.RestApi
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.NetworkInformation;
    using System.Text.RegularExpressions;
    using Microsoft.OpenApi.Models;
    using Microsoft.RestApi.Common;
    using Microsoft.RestApi.Models;
    using Newtonsoft.Json;

    public static class Utility
    {
        private static readonly JsonSerializer JsonSerializer = new JsonSerializer
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented
        };

        public static readonly Regex YamlHeaderRegex = new Regex(@"^\-{3}(?:\s*?)\n([\s\S]+?)(?:\s*?)\n\-{3}(?:\s*?)(?:\n|$)", RegexOptions.Compiled | RegexOptions.Singleline, TimeSpan.FromSeconds(10));
        public static readonly YamlDotNet.Serialization.Deserializer YamlDeserializer = new YamlDotNet.Serialization.Deserializer();
        public static readonly YamlDotNet.Serialization.Serializer YamlSerializer = new YamlDotNet.Serialization.Serializer();
        public static readonly string Pattern = @"(?:{0}|[A-Z]+?(?={0}|[A-Z][a-z]|$)|[A-Z](?:[0-9]*?)(?:[a-z]*?)(?={0}|[A-Z]|$)|(?:[a-z]+?)(?={0}|[A-Z]|$))";
        public static readonly HashSet<string> Keyword = new HashSet<string> {
            "BI", "IP", "ML", "MAM", "OS", "VMs", "VM", "APIM", "vCenters", "WANs", "WAN", "IDs", "ID", "REST", "OAuth2", "SignalR", "iOS", "IOS",
            "PlayFab", "OpenId", "NuGet"
        };

        public static object GetYamlHeaderByMeta(string filePath, string metaName)
        {
            var yamlHeader = GetYamlHeader(filePath);
            object result;
            if (yamlHeader != null && yamlHeader.TryGetValue(metaName, out result))
            {
                return result;
            }
            return null;
        }

        public static Dictionary<string, object> GetYamlHeader(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File path {filePath} not exists when parsing yaml header.");
            }

            var markdown = File.ReadAllText(filePath);

            var match = YamlHeaderRegex.Match(markdown);
            if (match.Length == 0)
            {
                return null;
            }

            // ---
            // a: b
            // ---
            var value = match.Groups[1].Value;
            try
            {
                using (StringReader reader = new StringReader(value))
                {
                    return YamlDeserializer.Deserialize<Dictionary<string, object>>(reader);
                }
            }
            catch (Exception)
            {
                Console.WriteLine();
                return null;
            }
        }

        public static void Serialize(string path, object obj)
        {
            using (var stream = File.Create(path))
            using (var writer = new StreamWriter(stream))
            {
                JsonSerializer.Serialize(writer, obj);
            }
        }

        public static string FirstLetterToLower(this string str)
        {
            if (str == null)
            {
                return null;
            }
            if (str.Length > 1)
            {
                return char.ToLower(str[0]) + str.Substring(1);
            }
            return str.ToLower();
        }

        public static string ConvertSecurityTypeToSchemaString(this string str)
        {
            if (str == null) return null;

            if (string.Compare(str, "OAuth2", true) == 0)
            {
                return "oauth2";
            }

            return str.FirstLetterToUpper();
        }

        public static string FirstLetterToUpper(this string str)
        {
            if (str == null)
            {
                return null;
            }
            if (str.Length > 1)
            {
                return char.ToUpper(str[0]) + str.Substring(1);
            }
            return str.ToUpper();
        }

        public static string ExtractPascalNameByRegex(string name, List<string> noSplitWords)
        {
            if (name.Contains(" "))
            {
                return name;
            }
            if (name.Contains("_") || name.Contains("-"))
            {
                return name.Replace('_', ' ').Replace('-', ' ');
            }
            if (name.Contains("."))
            {
                name = name.Replace(".", "");
            }

            var result = new List<string>();
            var p = string.Format(Pattern, string.Join("|", noSplitWords?.Count > 0 ? Keyword.Concat(noSplitWords).Distinct() : Keyword));
            while (name.Length > 0)
            {
                var m = Regex.Match(name, p);
                if (!m.Success)
                {
                    return name;
                }
                result.Add(m.Value.ToLower());
                name = name.Substring(m.Length);
            }
            return string.Join(" ", result).FirstLetterToUpper();
        }

        public static string FormatJsonString(object jsonValue)
        {
            if (jsonValue == null)
            {
                return null;
            }
            try
            {
                return JsonUtility.ToIndentedJsonString(jsonValue).Replace("\r\n", "\n");
            }
            catch
            {
                return null;
            }
        }

        private static string Normalize(string path)
        {
            return string.IsNullOrEmpty(path) ? string.Empty : path.Replace(" ", "").Replace("$", "").Replace("..", ".").Trim('.').ToLower();
        }

        public static string GetId(string serviceName, string sourceFileName, string groupName, string operationName)
        {
            var id = $"{Normalize(serviceName)}.{Normalize(sourceFileName)}.{Normalize(groupName)}.{Normalize(operationName)}";
            return Normalize(id);
        }

        public static string GetId(string groupId, string operationName)
        {
            var id = $"{groupId}.{Normalize(operationName)}";
            return Normalize(id);
        }

        public static OperationV3Entity CleanOperation(this OperationV3Entity operation)
        {
            if(operation.Parameters != null && operation.Parameters.Count == 0)
            {
                operation.Parameters = null;
            }

            if(operation.Callbacks != null && operation.Callbacks.Count == 0)
            {
                operation.Callbacks = null;
            }

            if (operation.Paths != null && operation.Paths.Count == 0)
            {
                operation.Paths = null;
            }

            if (operation.Responses != null && operation.Responses.Count == 0)
            {
                operation.Responses = null;
            }

            if (operation.Securities != null && operation.Securities.Count == 0)
            {
                operation.Securities = null;
            }

            if (operation.Servers != null && operation.Servers.Count == 0)
            {
                operation.Servers = null;
            }

            if(operation.RequestBody != null && operation.RequestBody.Bodies != null && !operation.RequestBody.Bodies.Any())
            {
                operation.RequestBody.Bodies = null;
            }

            if(operation.Responses != null)
            {
                operation.Responses.ForEach(response =>
                {
                    if (response.Bodies != null && !response.Bodies.Any())
                    {
                        response.Bodies = null;
                    }
                });
            }
            return operation;
        }
    }
}
