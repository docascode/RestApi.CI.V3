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
            var requestbodies = RestOperationTransformer.TransformRequestBody(operation);
            var test = JsonUtility.ToJsonString(requestbodies);
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
