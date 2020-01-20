namespace Microsoft.RestApi.Models
{
    using System.Collections.Generic;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    public class ComponentGroupEntity : GroupEntity
    {
        [YamlIgnore]
        public List<NamedEntity> Components { get; set; }

        [YamlMember(Alias = "groupItems")]
        public IList<string> GroupItems
        {
            get
            {
                var ids = new List<string>();
                foreach (var component in Components)
                {
                    ids.Add(component.Id);
                }
                return ids;
            }
        }
    }
}
