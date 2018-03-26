namespace Microsoft.RestApi
{
    using System;

    using Microsoft.RestApi.Splitters;
    using Microsoft.RestApi.Transformers;

    public class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                if (args.Length != 4)
                {
                    Console.WriteLine($"Usage: {AppDomain.CurrentDomain.FriendlyName} [source_root_directory] [target_root_directory] [mappingfile.json] [output_directory]");
                    return 1;
                }
                var transformerFactory = new RestTransformerFactory();
                var restFileInfos = new GraphRestSplitter(args[0], args[1], args[2], args[3], transformerFactory);
                restFileInfos.Process();
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurs: {ex.Message}");
                return 1;
            }
        }
    }
}
