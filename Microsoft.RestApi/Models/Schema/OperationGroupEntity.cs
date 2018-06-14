namespace Microsoft.RestApi.Models
{
    using System.Collections.Generic;

    using Newtonsoft.Json;
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
        public IList<string> OperationIds
        {
            get
            {
                var ids = new List<string>();
                foreach (var operations in Operations)
                {
                    ids.Add(operations.Id);
                }
                return ids;
            }
        }

        [YamlIgnore]
        [JsonIgnore]
        public IList<OperationEntity> Operations { get; set; }
    }
}
