namespace Microsoft.RestApi.UnitTest
{
    using System.Linq;

    using Microsoft.RestApi.Common;
    using Microsoft.RestApi.Models;
    using Microsoft.RestApi.Transformers;

    using Xunit;

    [Trait("Owner", "juchen@microsoft.com")]
    [Trait("Priority", "1")]
    [Trait("Category", "OperationEntity")]
    public class ComponentUnitTest : BaseUnitTest
    {
        [Fact]
        public void ComponentEntity_GetComponent()
        {
            var openApiDocument = LoadOpenApiDocument("../../samples/GetComponent.yaml");
            var operation = openApiDocument.Paths.Values.First().Operations.Values.First();
            var model = new TransformModel
            {
                OpenApiDoc = openApiDocument,
                ServiceName = "mockServerName",
                ComponentGroupName = "mockComponentGroup"
            };
            var componentGroup = RestComponentsTransformer.Transform(model);

            var expects = LoadExpectedJsonObject<ComponentGroupEntity>("../../expects/Components.json");

            Assert.NotNull(componentGroup);
            Assert.NotNull(componentGroup.Components);
            Assert.Equal(componentGroup.Components.Count(), expects.Components.Count);

            foreach (var expect in expects.Components)
            {
                var foundComponent = componentGroup.Components.SingleOrDefault(p => p.Id == expect.Id);
                Assert.NotNull(foundComponent);
                Assert.Equal(JsonUtility.ToJsonString(expect), JsonUtility.ToJsonString(foundComponent));
            }
        }
    }
}
