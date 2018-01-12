namespace Microsoft.RestApi.Models
{
    using YamlDotNet.Serialization;

    public abstract class HeaderEntity : NamedEntity
    {
        [YamlMember(Alias = "value")]
        public string Value { get; set; }
    }
}
