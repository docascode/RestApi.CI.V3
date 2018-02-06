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
                    writer.WriteLine("### YamlMime:RESTOperationGroup");
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

        public void TransformerComponents(TransformModel transformModel, string targetDir, string componentGroupFileName, string componentsDir)
        {
            var componentGroup = RestComponentsTransformer.Transform(transformModel);
            if (componentGroup != null)
            {
                using (var writer = new StreamWriter(Path.Combine(targetDir, componentGroupFileName)))
                {
                    writer.WriteLine("### YamlMime:RESTComponentGroup");
                    YamlSerializer.Serialize(writer, componentGroup);
                }

                foreach (var component in componentGroup.Components)
                {
                    using (var writer = new StreamWriter(Path.Combine(componentsDir, component.Name + ".yml")))
                    {
                        writer.WriteLine("### YamlMime:RESTComponent");
                        YamlSerializer.Serialize(writer, component);
                    }
                }
                
            }
        }
    }
}
