namespace Microsoft.RestApi.Models
{
    using System.Collections.Generic;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    public class GroupEntity : NamedEntity
    {
        [YamlMember(Alias = "summary")]
        public string Summary { get; set; }

        [YamlMember(Alias = "groupType")]
        public string GroupType { get; set; }
    }
}
