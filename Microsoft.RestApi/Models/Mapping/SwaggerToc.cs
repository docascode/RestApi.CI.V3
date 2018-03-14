namespace Microsoft.RestApi.Models
{
    using System.Collections.Generic;

    public class SwaggerToc
    {
        public string Title { get; }

        public string FilePath { get; }

        public string Uid { get; }

        public List<SwaggerToc> ChildrenToc { get; }

        public SwaggerToc(string title, string filePath, string uid, List<SwaggerToc> childrenToc = null)
        {
            Title = title;
            FilePath = filePath;
            Uid = uid;
            ChildrenToc = childrenToc;
        }
    }
}
