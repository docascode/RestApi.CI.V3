namespace Microsoft.RestApi.Models
{
    using System;

    using YamlDotNet.Serialization;

    [Serializable]
    public class IdentifiableEntity: BaseEntity
    {
        [YamlMember(Alias = "uid", Order = -10)]
        public string Id { get; set; }
    }
}
