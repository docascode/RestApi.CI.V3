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
    public class RequestBodyUnitTest : BaseUnitTest
    {
        [Fact]
        public void OperationEntity_GetRequestBody()
        {
            var openApiDocument = LoadOpenApiDocument("../../samples/GetRequestBody.yaml");
            var operation = openApiDocument.Paths.Values.First().Operations.Values.First();

            var expects = LoadExpectedJsonObject<List<RequestBodyEntity>>("../../expects/Requestbodies.json");
            var requestbodies = RestOperationTransformer.TransformRequestBody(operation, string.Empty);

            Assert.NotNull(requestbodies);
            Assert.Equal(requestbodies.Count, expects.Count);

            foreach (var expect in expects)
            {
                var foundRequestBody = requestbodies.SingleOrDefault(p => p.MediaType == expect.MediaType);
                Assert.NotNull(foundRequestBody);
                Assert.Equal(JsonUtility.ToJsonString(expect), JsonUtility.ToJsonString(foundRequestBody));
            }
        }

        [Fact]
        public void OperationEntity_GetRequestBody_With_ServerId()
        {
            var openApiDocument = LoadOpenApiDocument("../../samples/GetRequestBody2.yaml");
            var operation = openApiDocument.Paths.Values.First().Operations.Values.First();

            var expects = LoadExpectedJsonObject<List<RequestBodyEntity>>("../../expects/Requestbodies2.json");
            var requestbodies = RestOperationTransformer.TransformRequestBody(operation, "mockServerId");

            Assert.NotNull(requestbodies);
            Assert.Equal(requestbodies.Count, expects.Count);

            foreach (var expect in expects)
            {
                var foundRequestBody = requestbodies.SingleOrDefault(p => p.MediaType == expect.MediaType);
                Assert.NotNull(foundRequestBody);
                Assert.Equal(JsonUtility.ToJsonString(expect), JsonUtility.ToJsonString(foundRequestBody));
            }
        }
    }
}
