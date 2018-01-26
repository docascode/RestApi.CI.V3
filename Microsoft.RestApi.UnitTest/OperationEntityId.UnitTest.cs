namespace Microsoft.RestApi.UnitTest
{
    using Microsoft.RestApi.Transformers;

    using Xunit;

    [Trait("Owner", "juchen@microsoft.com")]
    [Trait("Priority", "1")]
    [Trait("Category", "OperationEntity")]
    public class OperationEntityIdUnitTest : BaseUnitTest
    {
        [Theory]
        [InlineData("../../samples/GetOperationId.yaml", "serviceName", "groupName", "operationId", "developer.uspto.gov.v1.servicename.groupname.operationid")]
        [InlineData("../../samples/GetOperationId2.yaml", "service Name", "group Name", "operation Id", "developer.uspto.gov.servicename.groupname.operationid")]
        public void OperationEntity_GetOperationId(string filePath, string serviceName, string groupName, string operationId, string expeted)
        {
            var openApiDocument = LoadOpenApiDocument(filePath);
            var result = TransformHelper.GetOperationId(openApiDocument.Servers, serviceName, groupName, operationId);
            Assert.Equal(expeted, result);
        }
    }
}
