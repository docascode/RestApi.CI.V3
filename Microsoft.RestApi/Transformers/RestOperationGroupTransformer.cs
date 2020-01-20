namespace Microsoft.RestApi.Transformers
{
    using Microsoft.RestApi.Models;

    public class RestOperationGroupTransformer
    {
        public static OperationGroupEntity Transform(TransformModel transformModel)
        {
            return new OperationGroupEntity
            {
                Id = transformModel.OperationGroupId,
                ApiVersion = transformModel.OpenApiDoc.Info?.Version,
                Name = transformModel.OperationGroupName,
                Service = transformModel.ServiceName,
                Summary = transformModel.OpenApiTag.Description,
                GroupType = "operations"
            };
        }
    }
}
