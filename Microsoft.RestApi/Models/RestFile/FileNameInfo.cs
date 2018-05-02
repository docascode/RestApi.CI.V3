namespace Microsoft.RestApi.Models
{
    using System.Collections.Generic;

    public class FileNameInfo
    {
        public string FileId { get; set; }

        public string FileName { get; set; }

        public List<FileNameInfo> ChildrenFileNameInfo { get; set; }

        public string TocName { get; set; }

        public TocType TocType { get; set; }

        public bool IsComponentGroup { get; set; }

        public OperationEntity OperationInfo { get; set; }
    }
}
