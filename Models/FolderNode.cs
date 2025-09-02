using System;
using System.Collections.Generic;

namespace FileExplorerApplicationV1.Models
{
    public class FolderNode
    {
        public string Name { get; set; }
        public string FullPath { get; set; }
        public int ChildCount { get; set; }

        // Separate lists for folders and files
        public List<FolderNode> SubFolders { get; set; } = new();
        public List<FileDetail> Files { get; set; } = new();
    }
}
