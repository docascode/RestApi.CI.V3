namespace Microsoft.RestApi.Splitters
{
    using Microsoft.RestApi.Transformers;

    public class RestSplitterFactory
    {
        public static RestSplitter GetRestSplitter(string product, string sourceRootDir, string targetRootDir, string mappingFilePath, string outputDir, RestTransformerFactory transformerFactory)
        {
            switch (product)
            {
                case "Graph":
                    return new GraphRestSplitter(sourceRootDir, targetRootDir, mappingFilePath, outputDir, transformerFactory);
                default:
                    return new RestSplitter(sourceRootDir, targetRootDir, mappingFilePath, outputDir, transformerFactory);
            }
        }
    }
}
