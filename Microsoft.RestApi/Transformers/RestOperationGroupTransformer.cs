namespace Microsoft.RestApi.Transformers
{
    using System.Collections.Generic;

    using Microsoft.RestApi.Models;
    using Microsoft.RestApi.Splitters;

    public class RestOperationGroupTransformer
    {
        public static OperationGroupEntity Transform(TransformModel transformModel)
        {
            var openApiOperations = SplitHelper.FindOperationsByTag(transformModel.OpenApiDoc.Paths, transformModel.OpenApiTag);
            var operations = new List<Operation>();
            foreach (var openApiOperation in openApiOperations)
            {
                var operationName = openApiOperation.Value.OperationId;
                var operation = new Operation
                {
                    Id = TransformHelper.GetOperationId(transformModel.OpenApiDoc.Servers, transformModel.ServiceName, transformModel.OperationGroupName, operationName)
                    // Remove this for now
                    //Summary = TransformHelper.GetOperationSummary(openApiOperation.Value?.Summary, openApiOperation.Value?.Description)
                };
                operations.Add(operation);
            }
            return new OperationGroupEntity
            {
                Id = TransformHelper.GetOperationGroupId(transformModel.OpenApiDoc.Servers, transformModel.ServiceName, transformModel.OperationGroupName),
                ApiVersion = transformModel.OpenApiDoc.Info?.Version,
                Name = transformModel.OperationGroupName,
                Service = transformModel.ServiceName,
                Operations = operations,
                Summary = transformModel.OpenApiTag.Description
            };
        }
    }
}
