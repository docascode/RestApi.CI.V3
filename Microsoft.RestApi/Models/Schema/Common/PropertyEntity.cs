namespace Microsoft.RestApi.Models
{
    using System.Collections.Generic;

    using YamlDotNet.Serialization;

    public class PropertyEntity : ReferenceEntity
    {
        [YamlMember(Alias = "allowReserved")]
        public bool AllowReserved { get; set; }
        
        [YamlMember(Alias = "isRequired")]
        public bool IsRequired { get; set; }

        [YamlMember(Alias = "description")]
        public string Description { get; set; }

        [YamlMember(Alias = "isNullable")]
        public bool Nullable { get; set; }

        [YamlMember(Alias = "isDeprecated")]
        public bool IsDeprecated { get; set; }

        [YamlMember(Alias = "types")]
        public List<PropertyTypeEntity> Types { get; set; }
        
        [YamlMember(Alias = "pattern")]
        public string Pattern { get; set; }

        [YamlMember(Alias = "format")]
        public string Format { get; set; }

        [YamlMember(Alias = "isAnyOf")]
        public bool IsAnyOf { get; set; }

        [YamlMember(Alias = "isOneOf")]
        public bool IsOneOf { get; set; }
        
        [YamlMember(Alias = "isNot")]
        public bool IsNot { get; set; }

        [YamlMember(Alias = "isAllOf")]
        public bool IsAllOf { get; set; }
    }
}
