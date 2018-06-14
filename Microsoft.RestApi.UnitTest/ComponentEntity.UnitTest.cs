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

            var components = new List<ComponentEntity>();
            if (openApiDocument.Components?.Schemas != null)
            {
                foreach (var schema in openApiDocument.Components?.Schemas)
                {
                    model.ComponentId = Utility.GetId(model.ServiceName, model.ComponentGroupName, schema.Key);
                    model.ComponentName = schema.Key;
                    model.OpenApiSchema = schema.Value;
                    components.Add(RestComponentTransformer.Transform(model));
                }
            }

            var expects = LoadExpectedJsonObject<ComponentGroupEntity>("../../expects/Components.json");

           
            Assert.Equal(components.Count(), expects.Components.Count);

            foreach (var expect in expects.Components)
            {
                var foundComponent = components.SingleOrDefault(p => p.Id == expect.Id);
                Assert.NotNull(foundComponent);
                Assert.Equal(JsonUtility.ToJsonString(expect), JsonUtility.ToJsonString(foundComponent));
            }
        }
    }
}
