using System;

namespace FileExplorerApplicationV1.Models
{
    public class FileDetail
    {
        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public long Size { get; set; }
        public string FormattedSize { get; set; } = string.Empty;
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
        public string Extension { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public List<string> Lines { get; set; } = new();
        public bool IsDirectory { get; set; } = false;
    }
}
