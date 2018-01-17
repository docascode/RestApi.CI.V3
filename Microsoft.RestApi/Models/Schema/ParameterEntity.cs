namespace Microsoft.RestApi.Models
{
    using System;

    using YamlDotNet.Serialization;

    [Serializable]
    public class ParameterEntity : PropertyEntity
    {
        [YamlMember(Alias = "in")]
        public string In { get; set; }

        [YamlMember(Alias = "isRequired")]
        public bool IsRequired { get; set; }
    }
}
