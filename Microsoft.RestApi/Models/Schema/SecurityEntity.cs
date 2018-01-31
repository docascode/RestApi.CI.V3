namespace Microsoft.RestApi.Models
{
    using System.Collections.Generic;

    using YamlDotNet.Serialization;

    public class SecurityEntity
    {
        [YamlMember(Alias = "type")]
        public string Type { get; set; }

        [YamlMember(Alias = "description")]
        public string Description { get; set; }

        [YamlMember(Alias = "in")]
        public string In { get; set; }

        [YamlMember(Alias = "flows")]
        public IList<FlowEntity> Flows { get; set; }
    }
}
