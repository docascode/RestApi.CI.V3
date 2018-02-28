namespace Microsoft.RestApi.UnitTest
{
    using System.Linq;

    using Microsoft.RestApi.Transformers;

    using Xunit;

    [Trait("Owner", "yannw@microsoft.com")]
    [Trait("Priority", "1")]
    [Trait("Category", "OperationEntity")]
    public class SecurityUnitTest : BaseUnitTest
    {
        [Fact]
        public void OperationEntity_GetSecurity()
        {
            var openApiDocument = LoadOpenApiDocument("../../samples/GetSecurity.yaml");
            var operation = openApiDocument.Paths.Values.First().Operations.Values.First();

            var security = RestOperationTransformer.TransformSecurity(operation.Security);
            Assert.NotNull(security);
            Assert.Equal(2, security.Count);
            Assert.Equal("Azure Active Directory OAuth2 Flow.", security[0].Description);
            Assert.Equal("Azure Active Directory OAuth2 Flow2.", security[1].Description);
            Assert.Equal(2, security[0].Flows.Count);
            Assert.Equal("authorizationCode", security[0].Flows[0].Name);
            Assert.Equal("implicit", security[0].Flows[1].Name);
            Assert.Equal(2, security[0].Flows[0].Scopes.Count);
            Assert.Equal("user_impersonation", security[0].Flows[0].Scopes[0].Name);
            Assert.Equal("read", security[0].Flows[0].Scopes[1].Name);
        }
    }
}
