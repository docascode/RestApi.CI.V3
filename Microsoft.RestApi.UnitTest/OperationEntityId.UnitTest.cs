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
        [InlineData("../../samples/GetOperationId.yaml", "serviceName", "groupName", "operationId", "servicename.groupname.operationid")]
        [InlineData("../../samples/GetOperationId2.yaml", "service Name", "group Name", "operation Id", "servicename.groupname.operationid")]
        public void OperationEntity_GetOperationId(string filePath, string serviceName, string groupName, string operationId, string expeted)
        {
            var openApiDocument = LoadOpenApiDocument(filePath);
            var result = Utility.GetId(serviceName, groupName, operationId);
            Assert.Equal(expeted, result);
        }
    }
}
