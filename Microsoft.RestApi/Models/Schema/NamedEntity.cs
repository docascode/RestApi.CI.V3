namespace Microsoft.RestApi.Models
{
    using System;

    using YamlDotNet.Serialization;

    [Serializable]

    public class NamedEntity : IdentifiableEntity
    {
        [YamlMember(Alias = "name", Order = -9)]
        public string Name { get; set; }
    }
}
