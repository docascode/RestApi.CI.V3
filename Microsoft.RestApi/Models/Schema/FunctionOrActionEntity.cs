namespace Microsoft.RestApi.Models
{
    using System.Collections.Generic;

    using YamlDotNet.Serialization;

    public class FunctionOrActionEntity : NamedEntity
    {
        [YamlMember(Alias = "service", Order = -8)]
        public string Service { get; set; }

        [YamlMember(Alias = "groupName", Order = -7)]
        public string GroupName { get; set; }

        [YamlMember(Alias = "apiVersion", Order = -6)]
        public string ApiVersion { get; set; }

        [YamlMember(Alias = "operations")]
        public IList<OperationEntity> Operations { get; set; }
    }
}
