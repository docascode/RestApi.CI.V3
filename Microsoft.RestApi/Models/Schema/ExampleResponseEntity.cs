﻿namespace Microsoft.RestApi.Models
{
    using System.Collections.Generic;

    using YamlDotNet.Serialization;

    public class ExampleResponseEntity
    {
        [YamlMember(Alias = "statusCode")]
        public string StatusCode { get; set; }

        [YamlMember(Alias = "headers")]
        public IList<ExampleResponseHeaderEntity> Headers { get; set; }

        [YamlMember(Alias = "body")]
        public string Body { get; set; }
    }
}
