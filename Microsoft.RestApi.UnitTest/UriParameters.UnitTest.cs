namespace Microsoft.RestApi.UnitTest
{
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.RestApi.Common;
    using Microsoft.RestApi.Models;
    using Microsoft.RestApi.Transformers;

    using Xunit;

    [Trait("Owner", "juchen@microsoft.com")]
    [Trait("Priority", "1")]
    [Trait("Category", "OperationEntity")]
    public class UriParametersUnitTest : BaseUnitTest
    {
        [Fact]
        public void OperationEntity_GetUriParameters()
        {
            var openApiDocument = LoadOpenApiDocument("../../samples/GetUriParameters.yaml");
            var operation = openApiDocument.Paths.Values.First().Operations.Values.First();

            var expects = LoadExpectedJsonObject<List<ParameterEntity>>("../../expects/UriParameters.json");
            var uriParameterEntities = RestOperationTransformer.TransformUriParameters(operation, string.Empty);
            Assert.NotNull(uriParameterEntities);
            Assert.Equal(expects.Count, uriParameterEntities.Count);

            foreach(var expect in expects)
            {
                var foundParameter = uriParameterEntities.SingleOrDefault(p => p.Name == expect.Name);
                Assert.NotNull(foundParameter);
                Assert.Equal(JsonUtility.ToJsonString(expect), JsonUtility.ToJsonString(foundParameter));
            }
        }

        [Fact]
        public void OperationEntity_GetUriParameters_WithServerId()
        {
            var openApiDocument = LoadOpenApiDocument("../../samples/GetUriParameters.yaml");
            var operation = openApiDocument.Paths.Values.First().Operations.Values.First();

            var expects = LoadExpectedJsonObject<List<ParameterEntity>>("../../expects/UriParameters.json");
            var uriParameterEntities = RestOperationTransformer.TransformUriParameters(operation, "mockServerId");
            Assert.NotNull(uriParameterEntities);
            Assert.Equal(expects.Count, uriParameterEntities.Count);

            foreach (var expect in expects)
            {
                var foundParameter = uriParameterEntities.SingleOrDefault(p => p.Name == expect.Name);
                Assert.NotNull(foundParameter);
                Assert.Equal(JsonUtility.ToJsonString(expect), JsonUtility.ToJsonString(foundParameter));
            }
        }
    }
}
