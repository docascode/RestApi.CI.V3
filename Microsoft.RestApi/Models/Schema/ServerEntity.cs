namespace Microsoft.RestApi.Models
{
    using System.Collections.Generic;

    using YamlDotNet.Serialization;

    public class ServerEntity : NamedEntity
    {
        [YamlMember(Alias = "variables")]
        public IList<ServerVariableEntity> ServerVariables { get; set; }
    }
}
