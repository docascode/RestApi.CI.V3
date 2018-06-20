namespace Microsoft.RestApi.Models
{
    using System;

    using YamlDotNet.Serialization;

    [Serializable]
    public class ParameterEntity : PropertyEntity
    {
        [YamlMember(Alias = "key")]
        public string Key
        {
            get
            {
                var key = string.IsNullOrEmpty(BaseComponentName) ? Name : string.Join(".", BaseComponentName, Name);
                return key.ToLower();
            }
        }

        [YamlMember(Alias = "in")]
        public string In { get; set; }

        [YamlMember(Alias = "baseComponent")]
        public string BaseComponentName { get; set; }
    }
}
