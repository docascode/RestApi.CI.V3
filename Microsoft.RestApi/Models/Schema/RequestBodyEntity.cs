namespace Microsoft.RestApi.Models
{
    using System.Collections.Generic;

    using YamlDotNet.Serialization;

    public class RequestBodyEntity
    {
        [YamlMember(Alias = "description")]
        public string Description { get; set; }

        [YamlMember(Alias = "parameters")]
        public IList<ParameterEntity> RequestBodyParameters { get; set; }
    }
}
