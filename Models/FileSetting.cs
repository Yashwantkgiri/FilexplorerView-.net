// File: Models/FileSettings.cs
namespace FileExplorerApplicationV1.Models
{
    public class FileSetting
    {
        public string[] AllowedFileExtensions { get; set; } = { ".txt", ".json", ".xml", ".pdf", ".doc", ".docx", ".csv" };
    }

}