namespace Microsoft.RestApi.Models
{
    using System.Collections.Generic;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    public class OperationGroupEntity : GroupEntity
    {
        [YamlMember(Alias = "groupItems")]
        public IList<string> GroupItems
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
        public List<OperationV3Entity> Operations { get; set; }
    }
}
