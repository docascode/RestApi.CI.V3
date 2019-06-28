namespace Microsoft.RestApi.Models
{
    using System.Collections.Generic;

    using YamlDotNet.Serialization;

    public class OperationV3Entity: NamedEntity
    {
        [YamlIgnore]
        public string OriginalOperationId { get; set; }

        [YamlMember(Alias = "callbacks")]
        public List<CallbackEntity> Callbacks { get; set; }

        [YamlMember(Alias = "description")]
        public string Description { get; set; }

        [YamlMember(Alias = "groupName")]
        public string GroupName { get; set; }

        [YamlMember(Alias = "httpVerb")]
        public string HttpVerb { get; set; }

        [YamlMember(Alias = "isDeprecated")]
        public bool IsDeprecated { get; set; }

        [YamlMember(Alias = "parameters")]
        public IList<ParameterEntity> Parameters { get; set; }

        [YamlMember(Alias = "paths")]
        public List<PathEntity> Paths { get; set; }

        [YamlMember(Alias = "requestBody")]
        public RequestBodyEntity RequestBody { get; set; }
        
        [YamlMember(Alias = "responses")]
        public List<ResponseEntity> Responses { get; set; }

        [YamlMember(Alias = "security")]
        public List<SecurityEntity> Securities { get; set; }

        [YamlMember(Alias = "seeAlso")]
        public List<string> SeeAlso { get; set; }

        [YamlMember(Alias = "summary")]
        public string Summary { get; set; }

        [YamlMember(Alias = "servers")]
        public IList<ServerEntity> Servers { get; set; }
    }

    public class PathEntity
    {
        [YamlMember(Alias = "content")]
        public string Content { get; set; }

        [YamlMember(Alias = "isOptional")]
        public bool IsOptional { get; set; }
    }


    public class ServerEntity : NamedEntity
    {
        [YamlMember(Alias = "description")]
        public string Description { get; set; }

        [YamlMember(Alias = "variables")]
        public IList<ServerVariableEntity> ServerVariables { get; set; }
    }

    public class CallbackEntity : NamedEntity
    {
        [YamlMember(Alias = "callbackOperations")]
        public IList<string> CallbackOperations { get; set; }
    }

    public class ServerVariableEntity : NamedEntity
    {
        [YamlMember(Alias = "description")]
        public string Description { get; set; }

        [YamlMember(Alias = "defaultValue")]
        public string DefaultValue { get; set; }

        [YamlMember(Alias = "values")]
        public IList<string> Values { get; set; }
    }
}
