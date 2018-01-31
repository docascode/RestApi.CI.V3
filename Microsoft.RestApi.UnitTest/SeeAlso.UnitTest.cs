namespace Microsoft.RestApi.UnitTest
{
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.RestApi.Common;
    using Microsoft.RestApi.Models;
    using Microsoft.RestApi.Transformers;

    using Xunit;

    [Trait("Owner", "yannw@microsoft.com")]
    [Trait("Priority", "1")]
    [Trait("Category", "OperationEntity")]
    public class SeeAlsoUnitTest : BaseUnitTest
    {
        [Fact]
        public void OperationEntity_GetSeeAlso()
        {
            var openApiDocument = LoadOpenApiDocument("../../samples/GetSeeAlso.yaml");
            var operation = openApiDocument.Paths.Values.First().Operations.Values.First();

            var seeAlso = RestOperationTransformer.TransformExternalDocs(operation);
            Assert.NotNull(seeAlso);
            Assert.Equal(3, seeAlso.Count);
            Assert.Equal("Find more info here", seeAlso[0].Description);
            Assert.Equal("description A", seeAlso[1].Description);
            Assert.Equal("description B", seeAlso[2].Description);
            Assert.Equal("https://example.com/", seeAlso[0].Url);
            Assert.Equal("https://exampleA.com", seeAlso[1].Url);
            Assert.Equal("https://exampleB.com", seeAlso[2].Url);
        }
    }
}
