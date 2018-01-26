namespace Microsoft.RestApi.UnitTest
{
    using Microsoft.RestApi.Transformers;

    using Xunit;

    [Trait("Owner", "juchen@microsoft.com")]
    [Trait("Priority", "1")]
    [Trait("Category", "OperationEntity")]
    public class ServersUnitTest : BaseUnitTest
    {
        [Fact]
        public void OperationEntity_GetServers()
        {
            var openApiDocument = LoadOpenApiDocument("../../samples/GetServers.yaml");
            var serverEntities = TransformHelper.GetServerEnities(openApiDocument.Servers);
            Assert.NotNull(serverEntities);
            Assert.True(serverEntities.Count == 1);
            Assert.Equal("https://developer.uspto.gov/v1", serverEntities[0].Name);

            Assert.NotNull(serverEntities[0].ServerVariables);
            Assert.True(serverEntities[0].ServerVariables.Count == 2);
            Assert.Equal("scheme", serverEntities[0].ServerVariables[0].Name);
            Assert.Equal("https", serverEntities[0].ServerVariables[0].DefaultValue);
            Assert.Equal("The Data Set API is accessible via https and http", serverEntities[0].ServerVariables[0].Description);
            Assert.Equal(2, serverEntities[0].ServerVariables[0].Values.Count);
            Assert.Equal("https", serverEntities[0].ServerVariables[0].Values[0]);
            Assert.Equal("http", serverEntities[0].ServerVariables[0].Values[1]);

            Assert.Equal("basepath", serverEntities[0].ServerVariables[1].Name);
            Assert.Equal("v1", serverEntities[0].ServerVariables[1].DefaultValue);
            Assert.Equal("the base path", serverEntities[0].ServerVariables[1].Description);
            Assert.Equal(2, serverEntities[0].ServerVariables[1].Values.Count);
            Assert.Equal("v1", serverEntities[0].ServerVariables[1].Values[0]);
            Assert.Equal("v2", serverEntities[0].ServerVariables[1].Values[1]);
        }

        [Fact]
        public void OperationEntity_GetServers_No_ServerVariables()
        {
            var openApiDocument = LoadOpenApiDocument("../../samples/GetServers2.yaml");
            var serverEntities = TransformHelper.GetServerEnities(openApiDocument.Servers);
            Assert.NotNull(serverEntities);
            Assert.True(serverEntities.Count == 1);
            Assert.Equal("https://developer.uspto.gov", serverEntities[0].Name);
            Assert.Null(serverEntities[0].ServerVariables);
        }
    }
}
