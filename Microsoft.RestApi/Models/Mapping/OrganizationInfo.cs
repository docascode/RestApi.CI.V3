namespace Microsoft.RestApi.Models
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    [Serializable]
    public class OrganizationInfo
    {
        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("name")]
        public string OrganizationName { get; set; }

        [JsonProperty("index")]
        public string OrganizationIndex { get; set; }

        [JsonProperty("default_toc_title")]
        public string DefaultTocTitle { get; set; }

        [JsonProperty("services")]
        public List<ServiceInfo> Services { get; set; }
    }
}
