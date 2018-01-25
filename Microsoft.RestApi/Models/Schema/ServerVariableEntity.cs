namespace Microsoft.RestApi.Models
{
    using System.Collections.Generic;

    using YamlDotNet.Serialization;

    public class ServerVariableEntity : NamedEntity
    {
        [YamlMember(Alias = "description")]
        public string Description { get; set; }

        [YamlMember(Alias = "defaultValue")]
        public string DefaultValue { get; set; }

        [YamlMember(Alias = "values")]
        public IList<string> Values { get; set; }
    }
}
