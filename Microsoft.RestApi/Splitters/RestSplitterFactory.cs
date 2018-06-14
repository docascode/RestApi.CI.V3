namespace Microsoft.RestApi.Splitters
{
    using Microsoft.RestApi.Common;
    using Microsoft.RestApi.Models;
    using Microsoft.RestApi.Transformers;

    public class RestSplitterFactory
    {
        public static RestSplitter GetRestSplitter(string sourceRootDir, string targetRootDir, string mappingFilePath, string outputDir)
        {
            var mappingFile = JsonUtility.ReadFromFile<MappingFile>(mappingFilePath).SortMappingFile();

            switch (mappingFile.Product)
            {
                case "graph":
                    return new GraphRestSplitter(sourceRootDir, targetRootDir, mappingFilePath, outputDir);
                default:
                    return new RestSplitter(sourceRootDir, targetRootDir, mappingFilePath, outputDir);
            }
        }
    }
}
