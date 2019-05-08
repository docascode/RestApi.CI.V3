using System.Collections.Generic;

namespace Microsoft.RestApi.Models
{
    public class SplitSwaggerResult
    {
        public List<OperationGroupEntity> OperationGroups { get; set; }

        public List<ComponentGroupEntity> ComponentGroups { get; set; }
    }
}
