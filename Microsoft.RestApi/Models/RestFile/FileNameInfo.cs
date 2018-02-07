namespace Microsoft.RestApi.Models
{
    using System.Collections.Generic;

    public class FileNameInfo
    {
        public string FileName { get; set; }

        public List<FileNameInfo> ChildrenFileNameInfo { get; set; }

        public string TocName { get; set; }
    }
}
