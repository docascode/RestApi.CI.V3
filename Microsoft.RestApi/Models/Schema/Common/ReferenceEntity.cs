namespace Microsoft.RestApi.Models
{
    using YamlDotNet.Serialization;

    public class ReferenceEntity : NamedEntity
    {
        [YamlMember(Alias = "referencedType")]
        public string ReferencedType { get; set; }
    }
}
