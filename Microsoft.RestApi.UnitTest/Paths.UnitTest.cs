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
    public class PathsUnitTest : BaseUnitTest
    {
        [Fact]
        public void OperationEntity_GetPaths()
        {
            var openApiDocument = LoadOpenApiDocument("../../samples/GetPaths.yaml");
            var operation = openApiDocument.Paths.Values.First().Operations.Values.First();

            var paths = RestOperationTransformer.TransformPaths("/pets", operation, null);
            Assert.NotNull(paths);
            Assert.Equal(5, paths.Count);
            Assert.Equal("/pets", paths[0]);
            Assert.Equal("/pets/children", paths[1]);
            Assert.Equal("/me/pets/children", paths[2]);
            Assert.Equal("/sites/pets/children", paths[3]);
            Assert.Equal("/users/pets/children", paths[4]);
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
            var paths = RestOperationTransformer.TransformPaths("/pets", operation, requiredQueryParameters);
            Assert.NotNull(paths);
            Assert.Equal(5, paths.Count);
            Assert.Equal("/pets?$select={$select}", paths[0]);
            Assert.Equal("/pets/children?$select={$select}", paths[1]);
            Assert.Equal("/me/pets/children?$case1={$case1}&$select={$select}", paths[2]);
            Assert.Equal("/sites/pets/children?$select={$select}&$zase1={$zase1}", paths[3]);
            Assert.Equal("/users/pets/children?$select={$select}&what", paths[4]);
        }
    }
}
