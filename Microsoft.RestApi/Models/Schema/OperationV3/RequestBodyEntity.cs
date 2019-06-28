namespace Microsoft.RestApi.Models
{
    using System.Collections.Generic;

    using YamlDotNet.Serialization;

    public class RequestBodyEntity : ReferenceEntity
    {
        [YamlMember(Alias = "bodies")]
        public List<BodyEntity> Bodies { get; set; }

        /// <summary>
        /// Get this requestBody from this operationId
        /// </summary>
        [YamlMember(Alias = "link")]
        public LinkEntity Link { get; set; }

        [YamlMember(Alias = "description")]
        public string Description { get; set; }

        [YamlMember(Alias = "isRequired")]
        public bool isRequired { get; set; }
    }
}