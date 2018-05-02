namespace Microsoft.RestApi.Models
{
    using YamlDotNet.Serialization;

    public class ResponseLinkEntity
    {
        [YamlMember(Alias = "key")]
        public string Key { get; set; }

        [YamlMember(Alias = "operation")]
        public string OperationId { get; set; }
    }
}
