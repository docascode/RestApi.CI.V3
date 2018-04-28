namespace Microsoft.RestApi.Models
{
    using System.Collections.Generic;

    using YamlDotNet.Serialization;

    public class ComponentEntity : NamedEntity
    {
        [YamlMember(Alias = "apiVersion")]
        public string ApiVersion { get; set; }

        [YamlMember(Alias = "service")]
        public string Service { get; set; }

        [YamlMember(Alias = "description")]
        public string Description { get; set; }

        [YamlMember(Alias = "kind")]
        public string Kind { get; set; }

        [YamlMember(Alias = "properties")]
        public List<PropertyEntity> PropertyItems { get; set; }

        [YamlMember(Alias = "operations")]
        public List<string> Operations { get; set; }
    }
}
