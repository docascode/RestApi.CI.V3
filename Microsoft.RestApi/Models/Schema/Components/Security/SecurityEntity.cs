namespace Microsoft.RestApi.Models
{
    using System.Collections.Generic;

    using YamlDotNet.Serialization;

    public class SecurityEntity: NamedEntity
    {
        [YamlMember(Alias = "type")]
        public string Type { get; set; }

        [YamlMember(Alias = "apiKeyName")]
        public string ApiKeyName { get; set; }

        [YamlMember(Alias = "bearerFormat")]
        public string BearerFormat { get; set; }

        [YamlMember(Alias = "description")]
        public string Description { get; set; }

        [YamlMember(Alias = "scheme")]
        public string Scheme { get; set; }

        [YamlMember(Alias = "openIdConnectUrl")]
        public string OpenIdConnectUrl { get; set; }

        [YamlMember(Alias = "in")]
        public string In { get; set; }

        [YamlMember(Alias = "flows")]
        public IList<FlowEntity> Flows { get; set; }
    }
}
