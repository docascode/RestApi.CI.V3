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

    [Trait("Owner", "terryjin@microsoft.com")]
    [Trait("Priority", "1")]
    [Trait("Category", "General")]
    public class GeneralTest : BaseUnitTest
    {
        [Fact]
        public void General()
        {
            var repoRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"samples\general");
            var restSplitter = new RestSplitter(repoRoot, repoRoot, repoRoot + @"\mapping.json", repoRoot + @"\tmp");
            restSplitter.Process();
            Assert.False(restSplitter.Errors.Any());

            var resultFolder = repoRoot + @"\tmp";
            var expectFolder = repoRoot + @"\expect";

            CompareFilesByFolder(resultFolder, expectFolder);
        }

        private void CompareFilesByFolder(string resultFolder, string expectFolder)
        {
            var resultFiles = Directory.EnumerateFiles(resultFolder, "*.*", SearchOption.AllDirectories).OrderBy(f => f).ToArray();
            var expectFiles = Directory.EnumerateFiles(expectFolder, "*.*", SearchOption.AllDirectories).OrderBy(f => f).ToArray();

            Assert.Equal(resultFiles.Count(), expectFiles.Count());

            for (int index = 0; index < resultFiles.Count(); index++)
            {
                var resultContent = File.ReadAllText(resultFiles[index]);
                var expectContent = File.ReadAllText(expectFiles[index]);
                Assert.Equal(resultContent, expectContent);
                Assert.Equal(resultFiles[index].Replace(resultFolder, ""),
                    expectFiles[index].Replace(expectFolder, ""));
            }
        }
    }
}
