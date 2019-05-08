namespace Microsoft.RestApi.Models
{
    using YamlDotNet.Serialization;

    public class ReferenceEntity : NamedEntity
    {
        [YamlMember(Alias = "ref")]
        public string ReferenceTo { get; set; }
    }
}
