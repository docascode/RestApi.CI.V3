namespace Microsoft.RestApi.Models
{
    using YamlDotNet.Serialization;

    public class SeeAlsoEntity
    {
        [YamlMember(Alias = "description")]
        public string Description { get; set; }

        [YamlMember(Alias = "url")]
        public string Url { get; set; }

    }
}
