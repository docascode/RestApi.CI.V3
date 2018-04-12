namespace Microsoft.RestApi.Models
{
    using System.Collections.Generic;
    using System.Linq;

    using YamlDotNet.Serialization;

    public class OperationEntity : NamedEntity
    {
        [YamlMember(Alias = "service", Order = -8)]
        public string Service { get; set; }

        [YamlMember(Alias = "groupName", Order = -7)]
        public string GroupName { get; set; }

        [YamlMember(Alias = "apiVersion", Order = -6)]
        public string ApiVersion { get; set; }

        [YamlMember(Alias = "summary")]
        public string Summary { get; set; }

        [YamlMember(Alias = "description")]
        public string Description { get; set; }

        [YamlMember(Alias = "remarks")]
        public string Remarks { get; set; }

        [YamlMember(Alias = "isDeprecated")]
        public bool IsDeprecated { get; set; } = false;

        [YamlMember(Alias = "isPreview")]
        public bool IsPreview { get; set; } = false;

        [YamlMember(Alias = "httpVerb")]
        public string HttpVerb { get; set; }

        [YamlMember(Alias = "servers")]
        public IList<ServerEntity> Servers { get; set; }

        [YamlMember(Alias = "paths")]
        public IList<string> Paths { get; set; }

        [YamlIgnore]
        public IList<OptionalParameter> OptionalParameters { get; set; }

        [YamlMember(Alias = "optionalParameters")]
        public string StringOptionalParameters
        {
            get
            {
                if (OptionalParameters != null && OptionalParameters.Count > 0)
                {
                    return string.Join("&", OptionalParameters.Select(prop => $"{prop.Name}={prop.Value}"));
                }
                return null;
            }
        }

        [YamlMember(Alias = "requestParameters")]
        public IList<ParameterEntity> RequestParameters { get; set; }

        [YamlMember(Alias = "requestBodies")]
        public IList<RequestBodyEntity> RequestBodies { get; set; }

        [YamlMember(Alias = "responses")]
        public IList<ResponseEntity> Responses { get; set; }

        [YamlMember(Alias = "securities")]
        public IList<SecurityEntity> Securities { get; set; }

        [YamlMember(Alias = "seeAlso")]
        public IList<SeeAlsoEntity> SeeAlsos { get; set; }

        [YamlIgnore]
        public string FilePath { get; set; }
    }
}
