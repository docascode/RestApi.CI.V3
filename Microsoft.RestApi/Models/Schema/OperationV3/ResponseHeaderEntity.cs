namespace Microsoft.RestApi.Models
{
    using System.Collections.Generic;

    using YamlDotNet.Serialization;

    public class ResponseHeaderEntity : PropertyEntity
    {
        [YamlMember(Alias = "examples")]
        public List<ExampleEntity> Examples { get; set; }
    }
}
