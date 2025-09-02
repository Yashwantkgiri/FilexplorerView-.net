using FileExplorerApplicationV1.Models;

using System.Collections.Generic;

namespace FileExplorerApplicationV1.Services
{
    public interface IFileProviderService
    {
        string GetBasePath();
        IEnumerable<FolderNode> GetFolderTree(string path = null);
        IEnumerable<FileDetail> GetFiles(string path);
        string GetFileContent(string fullPath);
        FileDetail GetFileDetail(string fullPath);
        void SaveFileContent(string fullPath, string content);
        void CreateFile(string folderPath, string fileName, string content);
        void DeleteFile(string fullPath);
    }
}
