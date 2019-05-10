namespace Microsoft.RestApi.Models
{
    using System;
    using System.Collections.Generic;
    using YamlDotNet.Serialization;

    public class BodyEntity
    {
        [YamlMember(Alias = "description")]
        public string Description { get; set; }

        [YamlMember(Alias = "examples")]
        public List<ExampleEntity> Examples { get; set; }

        [YamlMember(Alias = "mediaType")]
        public string MediaType { get; set; }

        [YamlMember(Alias = "type")]
        public PropertyTypeEntity Type { get; set; }
    }
}
