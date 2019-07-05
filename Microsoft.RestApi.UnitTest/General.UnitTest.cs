namespace Microsoft.RestApi.UnitTest
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.RestApi.Common;
    using Microsoft.RestApi.Models;
    using Microsoft.RestApi.Splitters;
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
            var repoRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"samples\general");
            var restSplitter = new RestSplitter(repoRoot, repoRoot, repoRoot + @"\mapping.json", repoRoot + @"\tmp");
            restSplitter.Process();
        }
    }
}
