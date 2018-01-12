namespace Microsoft.RestApi.Models
{

    using YamlDotNet.Serialization;
    public class SecurityScopeEntity : NamedEntity
    {
        [YamlMember(Alias = "description")]
        public string Description { get; set; }
    }
}
