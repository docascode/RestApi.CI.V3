namespace Microsoft.RestApi.Model
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    [Serializable]
    public class OrganizationInfo
    {
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
