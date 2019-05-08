namespace Microsoft.RestApi.Models
{
    using System;
    using System.Collections.Generic;
    using YamlDotNet.Serialization;

    public class BaseEntity
    {
        [YamlMember(Alias = "apiVersion")]
        public string ApiVersion { get; set; }

        [YamlMember(Alias = "service")]
        public string Service { get; set; }
    }
}
