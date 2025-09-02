//using FileExplorerApplicationV1.Services;

namespace FileExplorerApplicationV1.Models
{
    public class FileExplorerViewModel
    {
        public FolderNode? RootFolder { get; set; }
        public FileDetail? SelectedFile { get; set; }
        //public FileProviderType CurrentProviderType { get; set; } = FileProviderType.Local;
        //public List<ProviderInfo> AvailableProviders { get; set; } = new();
        public string? ErrorMessage { get; set; }
        public bool IsLoading { get; set; }
    }
}