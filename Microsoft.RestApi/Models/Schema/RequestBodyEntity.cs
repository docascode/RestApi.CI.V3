namespace Microsoft.RestApi.Models
{
    using System.Collections.Generic;

    using YamlDotNet.Serialization;

    public class RequestBodyEntity : NamedEntity
    {
        [YamlMember(Alias = "contentType")]
        public string ContentType { get; set; }

        [YamlMember(Alias = "description")]
        public string Description { get; set; }

        [YamlMember(Alias = "items")]
        public IList<RequestBodyItemEntity> RequestBodyItems { get; set; }
    }

    public class RequestBodyItemEntity : NamedEntity
    {
        [YamlMember(Alias = "description")]
        public string Description { get; set; }

        [YamlMember(Alias = "parameters")]
        public IList<PropertyEntity> Parameters { get; set; }
    }
}
