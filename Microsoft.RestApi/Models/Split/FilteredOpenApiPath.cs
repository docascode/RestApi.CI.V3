namespace Microsoft.RestApi.Models
{
    using Microsoft.OpenApi.Models;
    using System.Collections.Generic;

    public class OpenApiPathOperation
    {
        public KeyValuePair<string, OpenApiPathItem> OpenApiPath { get; set; }

        public KeyValuePair<OperationType, OpenApiOperation> Operation { get; set; }
    }

    public class FilteredOpenApiPath
    {
        public List<OpenApiPathOperation> Operations { get; set; }

        public List<string> ExtendTagNames { get; set; }

        public FilteredOpenApiPath()
        {
            Operations = new List<OpenApiPathOperation>();
            ExtendTagNames = new List<string>();
        }
    }
}
