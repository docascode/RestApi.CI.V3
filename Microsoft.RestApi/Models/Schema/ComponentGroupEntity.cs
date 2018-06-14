namespace Microsoft.RestApi.Models
{
    using System.Collections.Generic;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    public class ComponentGroupEntity : NamedEntity
    {
        [YamlMember(Alias = "summary")]
        public string Summary { get; set; }

        [YamlMember(Alias = "apiVersion")]
        public string ApiVersion { get; set; }

        [YamlMember(Alias = "service")]
        public string Service { get; set; }

        [YamlIgnore]
        public IList<ComponentEntity> Components { get; set; }

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
