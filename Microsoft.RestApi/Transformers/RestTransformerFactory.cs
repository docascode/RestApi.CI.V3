namespace Microsoft.RestApi.Transformers
{
    using System.Collections.Generic;
    using System.IO;

    using Microsoft.DocAsCode.YamlSerialization;
    using Microsoft.RestApi.Models;
    using System;

    public class RestTransformerFactory
    {
        public static readonly string YamlExtension = ".yml";
        public static readonly YamlSerializer YamlSerializer = new YamlSerializer();

        public FileNameInfo TransformerOperationGroup(TransformModel transformModel, string targetDir)
        {
            var operationGroupInfo = RestOperationGroupTransformer.Transform(transformModel);
            if (operationGroupInfo != null)
            {
                if (!Directory.Exists(targetDir))
                {
                    throw new ArgumentException($"{nameof(targetDir)} '{targetDir}' should exist.");
                }

                var filePath = TransformHelper.GetOperationGroupPath(transformModel.OpenApiDoc.Servers, transformModel.ServiceName, transformModel.OperationGroupPath);

                var absolutePath = Path.Combine(targetDir, $"{filePath}{YamlExtension}");
                if (!Directory.Exists(Path.GetDirectoryName(absolutePath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
                }
                using (var writer = new StreamWriter(absolutePath))
                {
                    writer.WriteLine("### YamlMime:RESTOperationGroupV3");
                    YamlSerializer.Serialize(writer, operationGroupInfo);
                }
                return new FileNameInfo
                {
                    FileId = operationGroupInfo.Id,
                    FileName = $"{filePath}{YamlExtension}"
                };
            }
            throw new Exception("Transform operation group failed");
        }

        public FileNameInfo TransformerOperation(TransformModel transformModel, string targetDir)
        {
            var operationInfo = RestOperationTransformer.Transform(transformModel);
            if (operationInfo != null)
            {
                var filePath = TransformHelper.GetOperationPath(transformModel.OpenApiDoc.Servers, transformModel.ServiceName, transformModel.OperationGroupPath, transformModel.OperationName);

                var absolutePath = Path.Combine(targetDir, $"{filePath}{YamlExtension}");
                if (!Directory.Exists(Path.GetDirectoryName(absolutePath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
                }

                using (var writer = new StreamWriter(absolutePath))
                {
                    writer.WriteLine("### YamlMime:RESTOperationV3");
                    YamlSerializer.Serialize(writer, operationInfo);
                }
                return new FileNameInfo
                {
                    FileId = operationInfo.Id,
                    FileName = $"{filePath}{YamlExtension}"
                };
            }
            throw new Exception("Transform operation failed");
        }

        public FileNameInfo TransformerComponents(TransformModel transformModel, string targetDir, string ComponentGroupName)
        {
            var componentFileNameInfos = new List<FileNameInfo>();
            var componentGroup = RestComponentsTransformer.Transform(transformModel);
            if (componentGroup != null)
            {
                var filePath = TransformHelper.GetComponentGroupPath(transformModel.OpenApiDoc.Servers, transformModel.ServiceName, transformModel.ComponentGroupName);
                var absolutePath = Path.Combine(targetDir, $"{filePath}{YamlExtension}");
                if (!Directory.Exists(Path.GetDirectoryName(absolutePath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
                }

                using (var writer = new StreamWriter(absolutePath))
                {
                    writer.WriteLine("### YamlMime:RESTComponentGroupV3");
                    YamlSerializer.Serialize(writer, componentGroup);
                }

                foreach (var component in componentGroup.Components)
                {
                    if (string.Equals(transformModel.ComponentGroupName, component.Name))
                    {
                        throw new Exception($"The component should not have name as same as {transformModel.ComponentGroupName}");
                    }

                    var componentFilePath = TransformHelper.GetComponentPath(transformModel.OpenApiDoc.Servers, transformModel.ServiceName, transformModel.ComponentGroupName, component.Name);
                    var componentAbsolutePath = Path.Combine(targetDir, $"{componentFilePath}{YamlExtension}");
                    if (!Directory.Exists(Path.GetDirectoryName(componentAbsolutePath)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(componentAbsolutePath));
                    }
                    using (var writer = new StreamWriter(componentAbsolutePath))
                    {
                        writer.WriteLine("### YamlMime:RESTComponentV3");
                        YamlSerializer.Serialize(writer, component);
                    }
                    componentFileNameInfos.Add(new FileNameInfo
                    {
                        TocName = component.Name,
                        FileName = $"{componentFilePath}{YamlExtension}",
                        FileId = component.Id
                    }); 
                }
                return new FileNameInfo
                {
                    FileId = componentGroup.Id,
                    FileName = $"{filePath}{YamlExtension}",
                    TocName = ComponentGroupName,
                    ChildrenFileNameInfo = componentFileNameInfos
                };
            }

            throw new Exception("Transform component group and components failed");
        }
    }
}
