namespace Microsoft.RestApi.Models
{
    using System;
    using System.Collections.Generic;

    using YamlDotNet.Serialization;

    [Serializable]
    public class ParameterEntity : PropertyEntity
    {
        [YamlMember(Alias = "in")]
        public string In { get; set; }

        /// <summary>
        /// Get this parameter from this operationId
        /// </summary>
        [YamlMember(Alias = "link")]
        public LinkEntity Link { get; set; }

        [YamlMember(Alias = "examples")]
        public List<ExampleEntity> Examples { get; set; }
    }
}
