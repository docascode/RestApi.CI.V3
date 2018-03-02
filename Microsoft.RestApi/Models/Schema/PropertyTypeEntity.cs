namespace Microsoft.RestApi.Models
{
    using System.Collections.Generic;

    using YamlDotNet.Serialization;

    public class PropertyTypeEntity : IdentifiableEntity
    {
        [YamlMember(Alias = "isArray")]
        public bool IsArray { get; set; } = false;

        [YamlMember(Alias = "kind")]
        public string Kind { get; set; }

        /// <summary>
        ///  when then kind = "enum", then the values should be enum values.
        /// </summary>
        [YamlMember(Alias = "enumValues")]
        public IList<string> Values { get; set; }


        [YamlMember(Alias = "isDictionary")]
        public bool IsDictionary { get; set; } = false;

        /// <summary>
        /// when isDictionary = true, the AdditionalTypes's id should the dictionary type.
        /// </summary>
        [YamlMember(Alias = "additionalTypes")]
        public List<IdentifiableEntity> AdditionalTypes { get; set; }

        /// <summary>
        /// if is the anonymous object, then we use properties to provide values.
        /// </summary>
        [YamlMember(Alias = "properties")]
        public List<PropertyEntity> AnonymousChildren { get; set; }
    }
}
