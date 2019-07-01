﻿namespace Microsoft.RestApi.Models
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    [Serializable]
    public class MappingFile
    {
        [JsonProperty("product")]
        public string Product { get; set; }

        [JsonProperty("apis_page_options")]
        public ApisPageOptions ApisPageOptions { get; set; }

        [JsonProperty("enable_markdown_fragment")]
        public bool EnableMarkdownFragment { get; set; }

        [JsonProperty("use_service_url_group")]
        public bool UseServiceUrlGroup { get; set; } = true;

        [JsonProperty("temp_target_api_root_dir")]
        public string TempTargetApiRootDir { get; set; }

        [JsonProperty("target_api_root_dir")]
        public string TargetApiRootDir { get; set; }

        [JsonProperty("remove_tag_from_operationId")]
        public bool RemoveTagFromOperationId { get; set; }

        [JsonProperty("is_grouped_by_tag")]
        public bool IsGroupdedByTag { get; set; }

        [JsonProperty("version_list")]
        public List<string> VersionList { get; set; }

        [JsonProperty("organizations")]
        public List<OrganizationInfo> OrganizationInfos { get; set; }

        [JsonProperty("tag_separator")]
        public string TagSeparator { get; set; }
      
        [JsonProperty("component_prefix")]
        public string ComponentPrefix { get; set; }
    }
}
