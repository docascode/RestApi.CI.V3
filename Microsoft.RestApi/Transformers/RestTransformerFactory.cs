namespace Microsoft.RestApi.Transformers
{
    using System.Collections.Generic;
    using System.IO;

    using Microsoft.DocAsCode.YamlSerialization;
    using Microsoft.RestApi.Models;
    using System;

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
                    writer.WriteLine("### YamlMime:RESTOperationGroupV3");
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
                    writer.WriteLine("### YamlMime:RESTOperationV3");
                    YamlSerializer.Serialize(writer, operationInfo);
                }
            }
        }

        public IList<FileNameInfo> TransformerComponents(TransformModel transformModel, string targetDir, string componentGroupFileName, string componentsDir)
        {
            var componentFileNameInfos = new List<FileNameInfo>();
            var componentGroup = RestComponentsTransformer.Transform(transformModel);
            if (componentGroup != null)
            {
                using (var writer = new StreamWriter(Path.Combine(targetDir, componentGroupFileName)))
                {
                    writer.WriteLine("### YamlMime:RESTComponentGroupV3");
                    YamlSerializer.Serialize(writer, componentGroup);
                }

                // todo: if exist component.Name == transformModel.GroupName should throw exception.
                foreach (var component in componentGroup.Components)
                {
                    if (string.Equals(transformModel.ComponentGroupName, component.Name))
                    {
                        throw new Exception($"The component should not have name as same as {transformModel.ComponentGroupName}");
                    }
                    using (var writer = new StreamWriter(Path.Combine(componentsDir, component.Name + ".yml")))
                    {
                        writer.WriteLine("### YamlMime:RESTComponentV3");
                        YamlSerializer.Serialize(writer, component);
                    }
                    componentFileNameInfos.Add(new FileNameInfo
                    {
                        TocName = component.Name,
                        FileName = Path.Combine(transformModel.ComponentGroupName, component.Name + ".yml")
                    }); 
                }
            }
            return componentFileNameInfos;
        }
    }
}
