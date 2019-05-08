namespace Microsoft.RestApi.Models
{
    using System.Collections.Generic;

    using YamlDotNet.Serialization;

    public class FlowEntity : NamedEntity
    {
        [YamlMember(Alias = "authorizationUrl")]
        public string AuthorizationUrl { get; set; }

        [YamlMember(Alias = "refreshUrl")]
        public string RefreshUrl { get; set; }

        [YamlMember(Alias = "tokenUrl")]
        public string TokenUrl { get; set; }

        [YamlMember(Alias = "type")]
        public string Type { get; set; }

        [YamlMember(Alias = "scopes")]
        public IList<SecurityScopeEntity> Scopes { get; set; }
    }
}
