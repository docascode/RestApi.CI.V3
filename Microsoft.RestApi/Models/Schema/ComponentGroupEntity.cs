namespace Microsoft.RestApi.Models
{
    using System.Collections.Generic;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    public class ComponentGroupEntity : NamedEntity
    {
        [YamlMember(Alias = "summary")]
        public string Summary { get; set; }

        [YamlIgnore]
        public List<NamedEntity> Components { get; set; }

        [YamlMember(Alias = "components")]
        public IList<string> ComponentIds
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
