namespace Microsoft.RestApi.Models
{
    using Microsoft.RestApi.Splitters;
    using System.Collections.Generic;

    using YamlDotNet.Serialization;

    public class TocEntity : IdentifiableEntity
    {
        [YamlMember(Alias = "name")]
        public string Name
        {
            get
            {
                if (string.IsNullOrEmpty(PascalName))
                {
                    return CustomName ?? string.Empty;
                }
                return Utility.ExtractPascalNameByRegex(PascalName);
            }
        }

        [YamlIgnore]
        public string PascalName { get; set; }

        [YamlIgnore]
        public string CustomName { get; set; }

        [YamlMember(Alias = "href")]
        public string Herf { get; set; }

        [YamlMember(Alias = "items")]
        public IList<TocEntity> Items { get; set; } 
    }
}
