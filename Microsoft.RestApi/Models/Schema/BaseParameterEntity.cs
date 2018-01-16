namespace Microsoft.RestApi.Models
{
    using System.Collections.Generic;

    using YamlDotNet.Serialization;

    public class BaseParameterEntity : NamedEntity
    {
        [YamlMember(Alias = "isReadyOnly")]
        public bool IsReadOnly { get; set; }

        [YamlMember(Alias = "description")]
        public string Description { get; set; }

        [YamlMember(Alias = "types")]
        public IList<BaseParameterTypeEntity> Types { get; set; }

        [YamlMember(Alias = "typesTitle")]
        public string TypesTitle { get; set; }

        [YamlMember(Alias = "pattern")]
        public string Pattern { get; set; }

        [YamlMember(Alias = "format")]
        public string Format { get; set; }
    }
}
