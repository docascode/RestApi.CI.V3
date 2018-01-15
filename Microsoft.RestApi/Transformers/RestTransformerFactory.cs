namespace Microsoft.RestApi.Transformers
{
    using System.IO;

    using Microsoft.DocAsCode.YamlSerialization;
    using Microsoft.RestApi.Models;

    public class RestTransformerFactory
    {
        public static readonly YamlSerializer YamlSerializer = new YamlSerializer();

        public void TransformerOperationGroup(TransformModel transformModel, string targetDir, string fileName)
        {
            var operationGroupInfo = RestOperationGroupTransformer.Transform(transformModel);
            if (operationGroupInfo != null)
            {
                using (var writer = new StreamWriter(Path.Combine(targetDir, fileName)))
                {
                    writer.WriteLine("### YamlMime:RESTOperation");
                    YamlSerializer.Serialize(writer, operationGroupInfo);
                }
            }
        }

        public void TransformerOperation(TransformModel transformModel, string targetDir, string fileName)
        {
            var operationInfo = RestOperationTransformer.Transform(transformModel);
            if (operationInfo != null)
            {
                using (var writer = new StreamWriter(Path.Combine(targetDir, fileName)))
                {
                    writer.WriteLine("### YamlMime:RESTOperation");
                    YamlSerializer.Serialize(writer, operationInfo);
                }
            }
        }
    }
}
