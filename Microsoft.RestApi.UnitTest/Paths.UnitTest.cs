namespace Microsoft.RestApi.UnitTest
{
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.RestApi.Models;
    using Microsoft.RestApi.Transformers;

    using Xunit;

    [Trait("Owner", "juchen@microsoft.com")]
    [Trait("Priority", "1")]
    [Trait("Category", "OperationEntity")]
    public class PathsUnitTest : BaseUnitTest
    {
        [Fact]
        public void OperationEntity_GetPaths()
        {
            var openApiDocument = LoadOpenApiDocument("../../samples/GetPaths.yaml");
            var operation = openApiDocument.Paths.Values.First().Operations.Values.First();

            var paths = RestOperationTransformer.TransformPaths(openApiDocument.Paths.First(), operation, null);
            Assert.NotNull(paths);
            Assert.Equal(1, paths.Count);
            Assert.Equal("/pets", paths[0]);
        }

        [Fact]
        public void OperationEntity_GetPaths_Required_Parameters()
        {
            var openApiDocument = LoadOpenApiDocument("../../samples/GetPaths2.yaml");
            var operation = openApiDocument.Paths.Values.First().Operations.Values.First();

            var uriParameterEntities = new List<ParameterEntity>
            {
                new ParameterEntity{ In = "Path", IsRequired = true, Name = "id" },
                new ParameterEntity{ In = "Query", IsRequired = true, Name = "$select" },
                new ParameterEntity{ In = "Query", IsRequired = false, Name = "$limit" },
                new ParameterEntity{ In = "Header", IsRequired = true, Name = "token" }
            };
            var requiredQueryParameters = uriParameterEntities.Where(p => p.IsRequired && p.In == "Query").ToList();
            var paths = RestOperationTransformer.TransformPaths(openApiDocument.Paths.First(), operation, requiredQueryParameters);
            Assert.NotNull(paths);
            Assert.Equal(1, paths.Count);
            Assert.Equal("/pets?$select={$select}", paths[0]);
        }
    }
}
