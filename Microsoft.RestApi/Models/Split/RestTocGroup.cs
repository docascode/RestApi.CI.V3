namespace Microsoft.RestApi.Models
{
    using System.Collections.Generic;

    public class RestTocGroup
    {
        public RestTocGroup()
        {
            Groups = new Dictionary<string, RestTocGroup>();
        }

        public string Id { get; set; }

        public string Name { get; set; }

        public Dictionary<string, RestTocGroup> Groups { get; }

        public RestTocGroup this[string name]
        {
            get
            {
                if(Groups.TryGetValue(name, out var restTocGroup))
                {
                    return restTocGroup;
                }
                return null;
            }
            set
            {
                Groups[name] = value;
            }
        }

        public List<GraphAggregateEntity> RestTocLeaves { get; set; }
    }
}
