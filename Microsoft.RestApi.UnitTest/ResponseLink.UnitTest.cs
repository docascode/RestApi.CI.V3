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
    [Trait("Category", "ResponseLinkEntity")]
    public class ResponseLinkUnitTest : BaseUnitTest
    {
        [Fact]
        public void ResponseLinkEntity_GetResponseLinks()
        {
            var openApiDocument = LoadOpenApiDocument("../../samples/GetResponseLinks.yaml");
            var operation = openApiDocument.Paths.Values.First().Operations.Values.First();
            var transformModel = new TransformModel
            {
                OpenApiDoc = openApiDocument,
                ServiceName = string.Empty
            };
            var expects = LoadExpectedJsonObject<List<ResponseLinkEntity>>("../../expects/ResponseLinks.json");

            var responseLinks = RestOperationTransformer.GetResponseLinks(transformModel, operation.Responses.First().Value.Links);

            Assert.NotNull(responseLinks);
            Assert.Equal(responseLinks.Count, expects.Count);
            foreach (var expect in expects)
            {
                var foundResponseLink = responseLinks.SingleOrDefault(p => p.Key == expect.Key);
                Assert.NotNull(foundResponseLink);
                Assert.Equal(JsonUtility.ToJsonString(expect), JsonUtility.ToJsonString(foundResponseLink));
            }
        }
    }
}
