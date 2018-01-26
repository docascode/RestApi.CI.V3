namespace Microsoft.RestApi.UnitTest
{
    using System;
    using System.IO;
    using System.Reflection;

    using Microsoft.RestApi.Common;
    using Microsoft.OpenApi.Models;
    using Microsoft.OpenApi.Readers;

    using Xunit;

    public class BaseUnitTest
    {
        public static string GetCurrentAssemblyFolder()
        {
            var assemblyUri = new Uri(Assembly.GetExecutingAssembly().GetName().CodeBase);
            return Path.GetDirectoryName(assemblyUri.LocalPath);
        }

        public OpenApiDocument LoadOpenApiDocument(string filePath)
        {
            using (var streamReader = File.OpenText(Path.Combine(GetCurrentAssemblyFolder(), filePath)))
            {
                var openApiDocument = new OpenApiStreamReader().Read(streamReader.BaseStream, out var context);
                Assert.False(context.Errors?.Count > 0);
                return openApiDocument;
            }
        }

        public T LoadExpectedJsonObject<T>(string filePath)
        {
            using (var streamReader = File.OpenText(Path.Combine(GetCurrentAssemblyFolder(), filePath)))
            {
                return JsonUtility.FromJsonStream<T>(streamReader.BaseStream);
            }
        }
    }
}
