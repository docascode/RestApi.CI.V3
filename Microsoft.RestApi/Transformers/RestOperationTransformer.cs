namespace Microsoft.RestApi.Transformers
{
    using Microsoft.RestApi.Models;

    public class RestOperationTransformer
    {
        public static OperationEntity Transform(TransformModel transformModel)
        {
            return new OperationEntity
            {
                Id = TransformHelper.GetOperationId(transformModel.OpenApiDoc.Servers, transformModel.ServiceName, transformModel.GroupName, transformModel.OperationName),
                Name = transformModel.OperationName,
                Service = transformModel.ServiceName,
                GroupName = transformModel.GroupName,
                Summary = TransformHelper.GetOperationSummary(transformModel.Operation.Value.Summary, transformModel.Operation.Value.Description),
                ApiVersion = transformModel.OpenApiDoc.Info.Version,
                IsDeprecated = transformModel.Operation.Value.Deprecated
            };
        }
    }
}
