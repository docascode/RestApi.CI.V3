using System.Collections.Generic;

namespace Microsoft.RestApi.Models
{
    public class SplitSwaggerResult
    {
        public IList<OperationGroupEntity> OperationGroups { get; set; }

        public ComponentGroupEntity ComponentGroup { get; set; }
    }
}
