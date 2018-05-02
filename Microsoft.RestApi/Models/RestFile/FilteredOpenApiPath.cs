namespace Microsoft.RestApi.Models
{
    using Microsoft.OpenApi.Models;
    using System.Collections.Generic;

    public class FilteredOpenApiPath
    {
        public KeyValuePair<string, OpenApiPathItem> OpenApiPath { get; set; }

        public List<KeyValuePair<OperationType, OpenApiOperation>> Operations { get; set; }

        public List<string> ExtendTagNames { get; set; }

        public FilteredOpenApiPath()
        {
            Operations = new List<KeyValuePair<OperationType, OpenApiOperation>>();
            ExtendTagNames = new List<string>();
        }
    }
}
