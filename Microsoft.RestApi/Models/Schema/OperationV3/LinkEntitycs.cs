namespace Microsoft.RestApi.Models
{
    using YamlDotNet.Serialization;
    public class LinkEntity
    {
        [YamlMember(Alias = "operationId")]
        public string OperationId { get; set; }

        [YamlMember(Alias = "linkedProperty")]
        public string LinkedProperty { get; set; }
    }
}
