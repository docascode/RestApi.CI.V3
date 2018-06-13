namespace Microsoft.RestApi.Transformers
{
    using System.Collections.Generic;

    using Microsoft.RestApi.Models;
    using Microsoft.RestApi.Splitters;

    public class RestOperationGroupTransformer
    {
        public static OperationGroupEntity Transform(TransformModel transformModel)
        {
            var filteredRestPathOperation = SplitHelper.FindOperationsByTag(transformModel.OpenApiDoc.Paths, transformModel.OpenApiTag);
            var operations = new List<string>();
            foreach (var openApiOperation in filteredRestPathOperation.Operations)
            {
                var operationName = openApiOperation.Operation.Value.OperationId;
                var operation = TransformHelper.GetId(transformModel.ServiceName, transformModel.OperationGroupName, operationName);

                operations.Add(operation);
            }
            return new OperationGroupEntity
            {
                Id = TransformHelper.GetId(transformModel.ServiceName, transformModel.OperationGroupName, null),
                ApiVersion = transformModel.OpenApiDoc.Info?.Version,
                Name = transformModel.OperationGroupName,
                Service = transformModel.ServiceName,
                Operations = operations,
                Summary = transformModel.OpenApiTag.Description
            };
        }
    }
}
