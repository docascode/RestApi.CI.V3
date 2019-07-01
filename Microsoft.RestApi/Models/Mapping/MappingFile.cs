namespace Microsoft.RestApi.Models
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    [Serializable]
    public class MappingFile
    {
        [JsonProperty("use_service_url_group")]
        public bool UseServiceUrlGroup { get; set; } = true;
        
        [JsonProperty("remove_tag_from_operationId")]
        public bool RemoveTagFromOperationId { get; set; }

        [JsonProperty("is_grouped_by_tag")]
        public bool IsGroupdedByTag { get; set; }

        [JsonProperty("version_list")]
        public List<string> VersionList { get; set; }

        [JsonProperty("organizations")]
        public List<OrganizationInfo> OrganizationInfos { get; set; }

        [JsonProperty("no_split_words")]
        public List<string> NoSplitWords { get; set; }
    }
}
