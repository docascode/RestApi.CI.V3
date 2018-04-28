namespace Microsoft.RestApi.Models
{
    using System.Collections.Generic;

    using YamlDotNet.Serialization;

    public class OperationGroupEntity : NamedEntity
    {
        [YamlMember(Alias = "summary")]
        public string Summary { get; set; }

        [YamlMember(Alias = "apiVersion")]
        public string ApiVersion { get; set; }

        [YamlMember(Alias = "service")]
        public string Service { get; set; }

        [YamlMember(Alias = "operations")]
        public IList<string> Operations { get; set; }

        [YamlIgnore]
        public string FilePath { get; set; }
    }
}
