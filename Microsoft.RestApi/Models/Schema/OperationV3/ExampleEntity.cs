namespace Microsoft.RestApi.Models
{
    using YamlDotNet.Serialization;

    public class ExampleEntity : ReferenceEntity
    {
        [YamlMember(Alias = "description")]
        public string Description { get; set; }

        [YamlMember(Alias = "value")]
        public string Value { get; set; }
    }
}
