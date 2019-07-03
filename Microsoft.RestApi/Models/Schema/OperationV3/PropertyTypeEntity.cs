namespace Microsoft.RestApi.Models
{
    using System.Collections.Generic;

    using YamlDotNet.Serialization;

    public class PropertyTypeEntity : ReferenceEntity
    {
        [YamlMember(Alias = "isArray")]
        public bool IsArray { get; set; } = false;

        /// <summary>
        ///  when then kind = "enum", then the values should be enum values.
        /// </summary>
        [YamlMember(Alias = "enumValues")]
        public List<string> Values { get; set; }


        [YamlMember(Alias = "isDictionary")]
        public bool IsDictionary { get; set; } = false;
        
        /// <summary>
        /// if is the anonymous object, then we use properties to provide values.
        /// </summary>
        [YamlMember(Alias = "typeProperties")]
        public List<PropertyEntity> Properties { get; set; }

        [YamlMember(Alias = "isPrimitiveType")]
        public bool IsPrimitiveType { get; internal set; }
    }
}
