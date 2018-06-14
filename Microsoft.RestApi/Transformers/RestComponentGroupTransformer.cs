namespace Microsoft.RestApi.Transformers
{
    using Microsoft.RestApi.Models;

    public class RestComponentGroupTransformer
    {
        public static ComponentGroupEntity Transform(TransformModel transformModel)
        {
            var openApiDocument = transformModel.OpenApiDoc;

            var componentGroup = new ComponentGroupEntity
            {
                Id = transformModel.ComponentGroupId,
                Name = transformModel.ComponentGroupName,
                Service = transformModel.ServiceName,
                ApiVersion = transformModel.OpenApiDoc.Info.Version
            };
            return componentGroup;
        }
    }
}
