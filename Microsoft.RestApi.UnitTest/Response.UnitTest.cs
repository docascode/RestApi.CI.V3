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
    public class ResponseUnitTest : BaseUnitTest
    {
        [Fact]
        public void OperationEntity_GetResponse()
        {
            var openApiDocument = LoadOpenApiDocument("../../samples/GetResponse.yaml");
            var operation = openApiDocument.Paths.Values.First().Operations.Values.First();

            var expects = LoadExpectedJsonObject<List<ResponseEntity>>("../../expects/Responses.json");
            var responses = RestOperationTransformer.TransformResponses(operation, string.Empty);

            Assert.NotNull(responses);
            Assert.Equal(responses.Count, expects.Count);

            foreach (var expect in expects)
            {
                var foundResponse = responses.SingleOrDefault(p => p.Name == expect.Name);
                Assert.NotNull(foundResponse);
                Assert.Equal(JsonUtility.ToJsonString(expect), JsonUtility.ToJsonString(foundResponse));
            }
        }

        [Fact]
        public void OperationEntity_GetResponse_With_ServerId()
        {
            var openApiDocument = LoadOpenApiDocument("../../samples/GetResponse2.yaml");
            var operation = openApiDocument.Paths.Values.First().Operations.Values.First();

            var expects = LoadExpectedJsonObject<List<ResponseEntity>>("../../expects/Responses2.json");
            var responses = RestOperationTransformer.TransformResponses(operation, "mockServerId");

            Assert.NotNull(responses);
            Assert.Equal(responses.Count, expects.Count);

            foreach (var expect in expects)
            {
                var foundResponse = responses.SingleOrDefault(p => p.Name == expect.Name);
                Assert.NotNull(foundResponse);
                Assert.Equal(JsonUtility.ToJsonString(expect), JsonUtility.ToJsonString(foundResponse));
            }
        }
    }
}
