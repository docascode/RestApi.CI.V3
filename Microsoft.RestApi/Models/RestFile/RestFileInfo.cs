namespace Microsoft.RestApi.Model
{
    using System.Collections.Generic;

    public class RestFileInfo
    {
        public List<FileNameInfo> FileNameInfos { get; set; } = new List<FileNameInfo>();

        public string TocTitle { get; set; }
    }
}
