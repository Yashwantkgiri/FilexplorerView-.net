//using FileExplorerApplicationV1.Models;
//using FileExplorerApplicationV1.Services.Interfaces;
//using Microsoft.Extensions.Options;

//namespace FileExplorerApplicationV1.Services.Providers
//{
//    public class LocalFileProviderService : IFileProviderService
//    {
//        private readonly string _rootPath;
//        private readonly string[] _allowedExtensions;
//        private readonly long _maxFileSize;
//        private readonly ILogger<LocalFileProviderService> _logger;

//        public string ProviderName => "Local File System";

//        public LocalFileProviderService(
//            IOptions<FileExplorerSettings> settings,
//            IOptions<FileSetting> fileSettings,
//            ILogger<LocalFileProviderService> logger)
//        {
//            _rootPath = settings.Value.RootPath;
//            _allowedExtensions = fileSettings.Value.AllowedFileExtensions ?? new[] { ".txt", ".json", ".xml", ".pdf", ".doc", ".docx", ".csv" };
//            _maxFileSize = 10 * 1024 * 1024; // 10MB
//            _logger = logger;
//        }

//        public async Task<bool> TestConnectionAsync()
//        {
//            try
//            {
//                return await Task.FromResult(Directory.Exists(_rootPath));
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error testing local file system connection");
//                return false;
//            }
//        }

//        public bool IsValidPath(string path)
//        {
//            if (string.IsNullOrWhiteSpace(path))
//                return false;

//            try
//            {
//                var sanitizedPath = Path.GetFullPath(path);
//                var allowedRootPath = Path.GetFullPath(_rootPath);
//                return sanitizedPath.StartsWith(allowedRootPath, StringComparison.OrdinalIgnoreCase);
//            }
//            catch
//            {
//                return false;
//            }
//        }

//        public async Task<List<FileDetail>> GetFolderContentsAsync(string path)
//        {
//            var sanitizedPath = Path.GetFullPath(path);

//            if (!Directory.Exists(sanitizedPath))
//            {
//                throw new DirectoryNotFoundException($"Directory not found: {sanitizedPath}");
//            }

//            try
//            {
//                var files = await Task.Run(() =>
//                    Directory.GetFiles(sanitizedPath)
//                        .Where(f => _allowedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
//                        .Select(f =>
//                        {
//                            var info = new FileInfo(f);
//                            return new FileDetail
//                            {
//                                Name = info.Name,
//                                FullPath = info.FullName,
//                                Size = info.Length,
//                                Created = info.CreationTime,
//                                Modified = info.LastWriteTime,
//                                Extension = info.Extension,
//                                //FormattedSize = FormatFileSize(info.Length)
//                            };
//                        })
//                        .OrderBy(f => f.Name)
//                        .ToList()
//                );

//                return files;
//            }
//            catch (UnauthorizedAccessException)
//            {
//                throw new UnauthorizedAccessException($"Access denied to directory: {sanitizedPath}");
//            }
//        }

//        public async Task<FolderNode> GetFolderTreeAsync(string rootPath)
//        {
//            var dirInfo = new DirectoryInfo(rootPath);
//            var node = new FolderNode
//            {
//                Name = dirInfo.Name,
//                FullPath = dirInfo.FullName
//            };

//            try
//            {
//                var directories = await Task.Run(() => Directory.GetDirectories(rootPath));
//                var files = await Task.Run(() => Directory.GetFiles(rootPath));

//                node.ChildCount = directories.Length + files.Length;

//                foreach (var dir in directories)
//                {
//                    node.SubFolders.Add(await GetFolderTreeAsync(dir));
//                }

//                foreach (var file in files)
//                {
//                    var fileInfo = new FileInfo(file);
//                    if (_allowedExtensions.Contains(fileInfo.Extension.ToLowerInvariant()))
//                    {
//                        node.Files.Add(new FileDetail
//                        {
//                            Name = fileInfo.Name,
//                            FullPath = fileInfo.FullName,
//                            Size = fileInfo.Length,
//                            Created = fileInfo.CreationTime,
//                            Modified = fileInfo.LastWriteTime,
//                            Extension = fileInfo.Extension,
//                            //FormattedSize = FormatFileSize(fileInfo.Length)
//                        });
//                    }
//                }
//            }
//            catch (UnauthorizedAccessException)
//            {
//                node.ChildCount = 0;
//            }

//            return node;
//        }

//        public async Task<FileDetail> GetFileContentAsync(string path)
//        {
//            var sanitizedPath = Path.GetFullPath(path);

//            if (!File.Exists(sanitizedPath))
//            {
//                throw new FileNotFoundException($"File not found: {sanitizedPath}");
//            }

//            var fileInfo = new FileInfo(sanitizedPath);

//            if (fileInfo.Length > _maxFileSize)
//            {
//                throw new InvalidOperationException("File is too large to display.");
//            }

//            string content;
//            List<string> lines;

//            if (IsBinaryFile(sanitizedPath))
//            {
//                content = "[Binary file - cannot display content]";
//                lines = new List<string> { content };
//            }
//            else
//            {
//                content = await File.ReadAllTextAsync(sanitizedPath);
//                lines = content.Split('\n').ToList();
//            }

//            return new FileDetail
//            {
//                FullPath = sanitizedPath,
//                Content = content,
//                Lines = lines,
//                Name = fileInfo.Name,
//                Size = fileInfo.Length,
//                //FormattedSize = FormatFileSize(fileInfo.Length),
//                Created = fileInfo.CreationTime,
//                Modified = fileInfo.LastWriteTime,
//                Extension = fileInfo.Extension
//            };
//        }

//        public async Task<bool> DeleteFileAsync(string path)
//        {
//            try
//            {
//                var sanitizedPath = Path.GetFullPath(path);

//                if (!File.Exists(sanitizedPath))
//                {
//                    return false;
//                }

//                await Task.Run(() => File.Delete(sanitizedPath));
//                return true;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error deleting file: {Path}", path);
//                return false;
//            }
//        }

//        public async Task<bool> UploadFileAsync(string folderPath, string fileName, Stream fileStream)
//        {
//            try
//            {
//                var sanitizedFolderPath = Path.GetFullPath(folderPath);
//                var filePath = Path.Combine(sanitizedFolderPath, fileName);

//                if (File.Exists(filePath))
//                {
//                    return false; // File already exists
//                }

//                using var fileStreamWriter = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
//                await fileStream.CopyToAsync(fileStreamWriter);

//                return true;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error uploading file: {FileName} to {FolderPath}", fileName, folderPath);
//                return false;
//            }
//        }

//        public async Task<bool> CreateFileAsync(string folderPath, string fileName)
//        {
//            try
//            {
//                var filePath = Path.Combine(folderPath, fileName);

//                if (File.Exists(filePath))
//                {
//                    return false; // File already exists
//                }

//                await Task.Run(() =>
//                {
//                    using var fs = new FileStream(filePath, FileMode.CreateNew);
//                    // Create empty file
//                });

//                return true;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error creating file: {FileName} in {FolderPath}", fileName, folderPath);
//                return false;
//            }
//        }

//        private static string FormatFileSize(long bytes)
//        {
//            string[] sizes = { "B", "KB", "MB", "GB" };
//            double len = bytes;
//            int order = 0;
//            while (len >= 1024 && order < sizes.Length - 1)
//            {
//                order++;
//                len = len / 1024;
//            }
//            return $"{len:0.##} {sizes[order]}";
//        }

//        private static bool IsBinaryFile(string filePath)
//        {
//            var extension = Path.GetExtension(filePath).ToLowerInvariant();
//            return extension is ".pdf" or ".doc" or ".docx" or ".exe" or ".dll" or ".zip" or ".rar";
//        }
//    }
//}
using System.Text;
using FileExplorerApplicationV1.Models;
using FileExplorerApplicationV1.Services;

using System.IO.Compression;

using System.Text.Json;
using System.Xml;

namespace FileExplorerApplicationV1.Services
{
    public class LocalFileProviderService : IFileProviderService
    {
        private readonly string _basePath;
        private readonly List<string> _allowed;
        private readonly string _metadataIndexPath;

        public LocalFileProviderService(string basePath, List<string> allowedExtensions)
        {
            _basePath = basePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            _allowed = allowedExtensions.Select(e => e.StartsWith(".") ? e.ToLower() : "." + e.ToLower()).ToList();
            _metadataIndexPath = Path.Combine(_basePath, ".fileIndex.json");

            // ensure base exists
            if (!Directory.Exists(_basePath))
                Directory.CreateDirectory(_basePath);
        }

        public string GetBasePath() => _basePath;

        public List<FolderNode> GetFolderTree(string basePath = null)
        {
            var path = string.IsNullOrEmpty(basePath) ? _basePath : basePath;
            path = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            var root = BuildFolderNode(path);
            return new List<FolderNode> { root };
        }

        private FolderNode BuildFolderNode(string path)
        {
            var node = new FolderNode
            {
                Name = Path.GetFileName(path) == string.Empty ? path : Path.GetFileName(path),
                FullPath = path
            };

            try
            {
                var dirs = Directory.GetDirectories(path);
                foreach (var d in dirs)
                {
                    try
                    {
                        node.SubFolders.Add(BuildFolderNode(d));
                    }
                    catch { /* ignore inaccessible subfolders */ }
                }
            }
            catch { /* ignore permissions issues */ }

            return node;
        }

        public List<FileDetail> GetFiles(string folderPath)
        {
            var target = string.IsNullOrEmpty(folderPath) ? _basePath : folderPath;
            if (!Directory.Exists(target)) return new List<FileDetail>();

            var files = Directory.GetFiles(target)
                .Where(f => _allowed.Count == 0 || _allowed.Contains(Path.GetExtension(f).ToLower()))
                .Select(f =>
                {
                    var fi = new FileInfo(f);
                    return new FileDetail
                    {
                        Name = fi.Name,
                        FullPath = fi.FullName,
                        Size = fi.Length,
                        Created = fi.CreationTime,
                        Modified = fi.LastWriteTime,
                        Extension = fi.Extension
                    };
                })
                .OrderBy(f => f.Name)
                .ToList();

            // update local metadata index (best-effort)
            try
            {
                var meta = new
                {
                    GeneratedAt = DateTime.UtcNow,
                    Folder = target,
                    Files = files
                };
                File.WriteAllText(_metadataIndexPath, JsonSerializer.Serialize(meta, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { /* not critical */ }

            return files;
        }

        public FileDetail GetFileDetail(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath)) return null;
            var fi = new FileInfo(fullPath);
            return new FileDetail
            {
                Name = fi.Name,
                FullPath = fi.FullName,
                Size = fi.Length,
                Created = fi.CreationTime,
                Modified = fi.LastWriteTime,
                Extension = fi.Extension
            };
        }

        public string GetFileContent(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath)) return null;

            var ext = Path.GetExtension(fullPath).ToLowerInvariant();

            try
            {
                if (ext == ".json" || ext == ".xml" || ext == ".txt")
                {
                    return File.ReadAllText(fullPath);
                }
                else if (ext == ".docx")
                {
                    // attempt to extract document.xml from docx zip
                    using var stream = File.OpenRead(fullPath);
                    using var z = new ZipArchive(stream, ZipArchiveMode.Read);
                    var entry = z.GetEntry("word/document.xml");
                    if (entry != null)
                    {
                        using var entryStream = entry.Open();
                        using var sr = new StreamReader(entryStream);
                        var xml = sr.ReadToEnd();
                        // strip XML tags naive
                        return StripXmlTags(xml);
                    }
                }
                else if (ext == ".doc")
                {
                    // older binary .doc - can't reliably read; return notice
                    return "[Binary .doc file — preview not supported. You can open it locally.]";
                }
                else
                {
                    // fallback: try text read
                    return File.ReadAllText(fullPath);
                }
            }
            catch (Exception ex)
            {
                return $"[Could not read file: {ex.Message}]";
            }

            return "[No preview available]";
        }

        private string StripXmlTags(string xmlContent)
        {
            try
            {
                var sb = new StringBuilder();
                var doc = new XmlDocument();
                doc.LoadXml(xmlContent);
                sb.Append(doc.InnerText);
                return sb.ToString();
            }
            catch
            {
                // fallback to naive tag removal
                var inside = false;
                var outSb = new StringBuilder();
                foreach (var ch in xmlContent)
                {
                    if (ch == '<') inside = true;
                    else if (ch == '>') { inside = false; continue; }
                    else if (!inside) outSb.Append(ch);
                }
                return outSb.ToString();
            }
        }

        public void SaveFileContent(string fullPath, string content)
        {
            if (string.IsNullOrEmpty(fullPath)) throw new ArgumentNullException(nameof(fullPath));
            File.WriteAllText(fullPath, content);
        }

        public void CreateFile(string folderPath, string fileName, string content)
        {
            var target = string.IsNullOrEmpty(folderPath) ? _basePath : folderPath;
            if (!Directory.Exists(target)) Directory.CreateDirectory(target);
            var full = Path.Combine(target, fileName);
            File.WriteAllText(full, content);
        }

        // --- CORRECTED EXPLICIT INTERFACE IMPLEMENTATIONS ---

        string IFileProviderService.GetBasePath()
        {
            return GetBasePath();
        }

        IEnumerable<FolderNode> IFileProviderService.GetFolderTree(string path)
        {
            return GetFolderTree(path);
        }

        IEnumerable<FileDetail> IFileProviderService.GetFiles(string path)
        {
            return GetFiles(path);
        }

        string IFileProviderService.GetFileContent(string fullPath)
        {
            return GetFileContent(fullPath);
        }

        FileDetail IFileProviderService.GetFileDetail(string fullPath)
        {
            return GetFileDetail(fullPath);
        }

        void IFileProviderService.SaveFileContent(string fullPath, string content)
        {
            SaveFileContent(fullPath, content);
        }

        void IFileProviderService.CreateFile(string folderPath, string fileName, string content)
        {
            CreateFile(folderPath, fileName, content);
        }

        void IFileProviderService.DeleteFile(string fullPath)
        {
            // Add the missing implementation for DeleteFile
            if (!string.IsNullOrEmpty(fullPath) && File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
    }
}
