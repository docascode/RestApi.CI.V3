namespace Microsoft.RestApi.Models
{
    using System.Collections.Generic;

    using YamlDotNet.Serialization;

    public class FlowEntity
    {
        [YamlMember(Alias = "name")]
        public string Name { get; set; }

        [YamlMember(Alias = "authorizationUrl")]
        public string AuthorizationUrl { get; set; }

        [YamlMember(Alias = "tokenUrl")]
        public string TokenUrl { get; set; }

        [YamlMember(Alias = "scopes")]
        public IList<SecurityScopeEntity> Scopes { get; set; }
    }
}
