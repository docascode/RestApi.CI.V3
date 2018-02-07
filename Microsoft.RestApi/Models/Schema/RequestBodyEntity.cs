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

        [YamlMember(Alias = "items")]
        public IList<RequestBodyItemEntity> RequestBodyItems { get; set; }
    }

    public class RequestBodyItemEntity : NamedEntity
    {
        [YamlMember(Alias = "description")]
        public string Description { get; set; }

        [YamlMember(Alias = "properties")]
        public IList<PropertyEntity> Properties { get; set; }
    }
}
