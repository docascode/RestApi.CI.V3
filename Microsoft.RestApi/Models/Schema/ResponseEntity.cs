namespace Microsoft.RestApi.Models
{
    using System;
    using System.Collections.Generic;

    using YamlDotNet.Serialization;

    [Serializable]
    public class ResponseEntity : NamedEntity
    {
        [YamlMember(Alias = "description")]
        public string Description { get; set; }

        [YamlMember(Alias = "body")]
        public IList<ResponseContentTypeAndBodyEntity> ResponseContentTypeAndBodies { get; set; }

        [YamlMember(Alias = "headers")]
        public IList<ResponseHeaderEntity> ResponseHeades { get; set; }
    }

    public class ResponseContentTypeAndBodyEntity : IdentifiableEntity
    {
        [YamlMember(Alias = "contentType")]
        public string ContentType { get; set; }

        [YamlMember(Alias = "typesTitle")]
        public string TypesTitle { get; set; }

        [YamlMember(Alias = "types")]
        public IList<PropertyTypeEntity> Types { get; set; }
    }
}
