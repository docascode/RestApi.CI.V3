namespace Microsoft.RestApi.Models
{
    using System.Collections.Generic;

    using YamlDotNet.Serialization;

    public class ResponseEntity: NamedEntity
    {
        [YamlMember(Alias = "bodies")]
        public List<BodyEntity> Bodies { get; set; }

        [YamlMember(Alias = "description")]
        public string Description { get; set; }

        [YamlMember(Alias = "headers")]
        public List<ResponseHeaderEntity> Headers { get; set; }

        [YamlMember(Alias = "statusCode")]
        public string StatusCode { get; set; }
    }
}
