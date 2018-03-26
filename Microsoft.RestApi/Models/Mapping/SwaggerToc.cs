namespace Microsoft.RestApi.Models
{
    using System;
    using System.Collections.Generic;

    public class SwaggerToc : ICloneable, IComparable
    {
        public string Title { get; }

        public string FilePath { get; }

        public string Uid { get; }

        public List<SwaggerToc> ChildrenToc { get; }

        public bool IsComponentGroup { get; }

        public SwaggerToc(string title, string filePath, string uid, List<SwaggerToc> childrenToc = null, bool isComponentGroup = false)
        {
            Title = title;
            FilePath = filePath;
            Uid = uid;
            ChildrenToc = childrenToc;
            IsComponentGroup = isComponentGroup;
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }

        public int CompareTo(object obj)
        {
            var newSwaggerToc = (SwaggerToc)obj;
            return string.Compare(this.Title, newSwaggerToc.Title);
        }
    }
}
