namespace Microsoft.RestApi.Models
{
    using System.Collections.Generic;

    using YamlDotNet.Serialization;

    public class RequestBodyEntity : NamedEntity
    {
        [YamlMember(Alias = "mediaType")]
        public string MediaType { get; set; }

        [YamlMember(Alias = "description")]
        public string Description { get; set; }

        [YamlMember(Alias = "isOneOf")]
        public bool IsOneOf { get; set; }

        [YamlMember(Alias = "isAnyOf")]
        public bool IsAnyOf { get; set; }

        [YamlMember(Alias = "schemas")]
        public IList<RequestBodySchemaEntity> RequestBodySchemas { get; set; }
    }

    public class RequestBodySchemaEntity : NamedEntity
    {
        [YamlMember(Alias = "description")]
        public string Description { get; set; }

        [YamlMember(Alias = "properties")]
        public IList<PropertyEntity> Properties { get; set; }
    }
}
