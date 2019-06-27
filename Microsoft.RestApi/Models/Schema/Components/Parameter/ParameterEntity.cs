namespace Microsoft.RestApi.Models
{
    using System;
    using System.Collections.Generic;

    using YamlDotNet.Serialization;

    [Serializable]
    public class ParameterEntity : PropertyEntity
    {
        [YamlMember(Alias = "in")]
        public string In { get; set; }

        [YamlMember(Alias = "link")]
        public string Link { get; set; }

        [YamlMember(Alias = "examples")]
        public List<ExampleEntity> Examples { get; set; }
    }
}
