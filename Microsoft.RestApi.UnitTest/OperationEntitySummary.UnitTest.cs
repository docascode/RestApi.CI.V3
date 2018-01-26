namespace Microsoft.RestApi.UnitTest
{
    using System.Linq;

    using Microsoft.RestApi.Transformers;

    using Xunit;

    [Trait("Owner", "juchen@microsoft.com")]
    [Trait("Priority", "1")]
    [Trait("Category", "OperationEntity")]
    public class OperationEntitySummaryUnitTest : BaseUnitTest
    {
        [Theory]
        [InlineData("../../samples/GetSummary.yaml", "List all pets")]
        [InlineData("../../samples/GetSummary2.yaml", "List all pets List all pets details")]
        public void OperationEntity_GetSummary(string filePath, string expeted)
        {
            var openApiDocument = LoadOpenApiDocument(filePath);
            var operation = openApiDocument.Paths.Values.First().Operations.Values.First();
            var result = TransformHelper.GetOperationSummary(operation.Summary, operation.Description);
            Assert.Equal(expeted, result);
        }
    }
}
