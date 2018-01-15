namespace Microsoft.RestApi.Transformers
{
    using System.Collections.Generic;

    using Microsoft.OpenApi.Models;
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
                IsDeprecated = transformModel.Operation.Value.Deprecated,
                // todo: need to transform
                IsPreview = false,
                Responses = new List<ResponseEntity>(),
                Parameters = new List<ParameterEntity>(),
                RequestHeaders = new List<ParameterEntity>(),
                RequestBodies = new List<RequestBodyEntity>(),
                Paths = new List<PathEntity>(),
                Examples = new List<ExampleEntity>(),
                Definitions = new List<DefinitionEntity>(),
                Securities = new List<SecurityEntity>()
            };
        }
    }
}
