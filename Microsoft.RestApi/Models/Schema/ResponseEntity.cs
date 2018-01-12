namespace Microsoft.RestApi.Models
{
    using System;
    using System.Collections.Generic;

    using YamlDotNet.Serialization;

    [Serializable]
    public class ResponseEntity : BaseParameterEntity
    {
        [YamlMember(Alias = "headers")]
        public IList<ResponseHeaderEntity> ResponseHeaders { get; set; }
    }
}
