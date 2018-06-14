namespace Microsoft.RestApi.Models
{
    using System.Collections.Generic;

    public class GraphAggregateResult
    {
        public GraphAggregateResult()
        {
            AggregateOperations = new List<GraphAggregateEntity>();
            Components = new List<ComponentEntity>();
            IdMappings = new Dictionary<string, string>();
        }
        public IDictionary<string, string> IdMappings { get; set; }

        public IList<GraphAggregateEntity> AggregateOperations { get; set; }

        public IList<ComponentEntity> Components { get; set; }
    }

    public class GraphAggregateEntity
    {
        public OperationEntity MainOperation { get; set; }

        public IList<OperationEntity> GroupedOperations { get; set; }

        public bool IsFunctionOrAction { get; set; }
    }
}
