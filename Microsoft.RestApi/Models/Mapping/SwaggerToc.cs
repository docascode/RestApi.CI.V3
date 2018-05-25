namespace Microsoft.RestApi.Models
{
    using System.Collections.Generic;

    public class RestTocLeaf
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string FileName { get; set; }

        public bool IsComponent { get; set; }

        public OperationEntity OperationInfo { get; set; }
    }

    public class RestTocGroup
    {
        public RestTocGroup()
        {
            Groups = new Dictionary<string, RestTocGroup>();
        }

        public string Id { get; set; }

        public string Name { get; set; }

        public string FileName { get; set; }

        public bool IsComponentGroup { get; set; }

        public TocType TocType { get; set; } = TocType.Page;

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

        public List<RestTocLeaf> OperationOrComponents { get; set; }
    }
}
