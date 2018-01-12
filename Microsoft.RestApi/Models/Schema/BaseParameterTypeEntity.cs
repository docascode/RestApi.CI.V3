namespace Microsoft.RestApi.Models
{
    using System.Collections.Generic;

    using YamlDotNet.Serialization;

    public class BaseParameterTypeEntity : IdentifiableEntity
    {
        [YamlMember(Alias = "isArray")]
        public bool IsArray { get; set; } = false;

        [YamlMember(Alias = "isDictionary")]
        public bool IsDictionary { get; set; } = false;

        [YamlMember(Alias = "additionalTypes")]
        public List<IdentifiableEntity> AdditionalTypes { get; set; }
    }
}
