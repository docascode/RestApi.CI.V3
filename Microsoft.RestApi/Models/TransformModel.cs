namespace Microsoft.RestApi.Models
{
    using System.Collections.Generic;

    using Microsoft.OpenApi.Models;

    public class TransformModel
    {
        public OpenApiDocument OpenApiDoc { get; set; }

        public OpenApiTag OpenApiTag { get; set; }

        public KeyValuePair<string, OpenApiPathItem> OpenApiPath { get; set; }

        public KeyValuePair<OperationType, OpenApiOperation> Operation { get; set; }

        public OpenApiSchema OpenApiSchema { get; set; }

        public string ServiceName { get; set; }

        public string OperationGroupId { get; set; }

        public string OperationGroupName { get; set; }

        public string ComponentGroupId { get; set; }

        public string ComponentGroupName { get; set; }

        public string OperationId { get; set; }

        public string OperationName { get; set; }
       
        public string ComponentId { get; set; }

        public string ComponentName { get; set; }
    }
}
