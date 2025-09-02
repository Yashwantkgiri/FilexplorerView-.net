//using FileExplorerApplicationV1.Models;
//using FileExplorerApplicationV1.Services.Interfaces;
//using Microsoft.Extensions.Options;
//using Renci.SshNet;
//using Renci.SshNet.Sftp;

//namespace FileExplorerApplicationV1.Services.Providers
//{
//    public class SftpFileProviderService : IFileProviderService, IDisposable
//    {
//        private readonly SftpSetting _sftpSettings;
//        private readonly string[] _allowedExtensions;
//        private readonly long _maxFileSize;
//        private readonly ILogger<SftpFileProviderService> _logger;
//        private SftpClient _sftpClient;
//        private bool _disposed = false;

//        public string ProviderName => $"SFTP - {_sftpSettings.Host}";

//        public SftpFileProviderService(
//            IOptions<SftpSetting> sftpSettings,
//            IOptions<FileSetting> fileSettings,
//            ILogger<SftpFileProviderService> logger)
//        {
//            _sftpSettings = sftpSettings.Value;
//            _allowedExtensions = fileSettings.Value.AllowedFileExtensions ?? new[] { ".txt", ".json", ".xml", ".pdf", ".doc", ".docx", ".csv" };
//            _maxFileSize = 10 * 1024 * 1024; // 10MB
//            _logger = logger;

//            InitializeSftpClient();
//        }

//        private void InitializeSftpClient()
//        {
//            try
//            {
//                _sftpClient = new SftpClient(_sftpSettings.Host, _sftpSettings.Port, _sftpSettings.Username, _sftpSettings.Password);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Failed to initialize SFTP client");
//                throw;
//            }
//        }

//        private async Task<SftpClient> GetConnectedClientAsync()
//        {
//            if (_sftpClient == null || !_sftpClient.IsConnected)
//            {
//                InitializeSftpClient();

//                await Task.Run(() =>
//                {
//                    if (!_sftpClient.IsConnected)
//                    {
//                        _sftpClient.Connect();
//                    }
//                });
//            }

//            return _sftpClient;
//        }

//        public async Task<bool> TestConnectionAsync()
//        {
//            try
//            {
//                var client = await GetConnectedClientAsync();
//                return client.IsConnected;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "SFTP connection test failed for host: {Host}", _sftpSettings.Host);
//                return false;
//            }
//        }

//        public bool IsValidPath(string path)
//        {
//            if (string.IsNullOrWhiteSpace(path))
//                return false;

//            // Basic path validation for SFTP
//            return !path.Contains("../") && !path.Contains("..\\");
//        }

//        public async Task<List<FileDetail>> GetFolderContentsAsync(string path)
//        {
//            var files = new List<FileDetail>();

//            try
//            {
//                var client = await GetConnectedClientAsync();

//                var sftpFiles = await Task.Run(() => client.ListDirectory(path));

//                foreach (var sftpFile in sftpFiles)
//                {
//                    if (sftpFile.IsRegularFile &&
//                        _allowedExtensions.Contains(Path.GetExtension(sftpFile.Name).ToLowerInvariant()))
//                    {
//                        files.Add(new FileDetail
//                        {
//                            Name = sftpFile.Name,
//                            FullPath = sftpFile.FullName,
//                            Size = sftpFile.Length,
//                            Created = sftpFile.LastWriteTime,
//                            Modified = sftpFile.LastWriteTime,
//                            Extension = Path.GetExtension(sftpFile.Name),
//                            //FormattedSize = FormatFileSize(sftpFile.Length)
//                        });
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error getting SFTP folder contents for path: {Path}", path);
//                throw new InvalidOperationException($"Failed to get SFTP folder contents: {ex.Message}");
//            }

//            return files.OrderBy(f => f.Name).ToList();
//        }

//        public async Task<FolderNode> GetFolderTreeAsync(string rootPath)
//        {
//            var node = new FolderNode
//            {
//                Name = Path.GetFileName(rootPath) ?? rootPath,
//                FullPath = rootPath
//            };

//            try
//            {
//                var client = await GetConnectedClientAsync();
//                var sftpFiles = await Task.Run(() => client.ListDirectory(rootPath));

//                var subFolders = new List<SftpFile>();
//                var fileCount = 0;

//                foreach (var sftpFile in sftpFiles)
//                {
//                    if (sftpFile.Name == "." || sftpFile.Name == "..")
//                        continue;

//                    if (sftpFile.IsDirectory)
//                    {
//                        subFolders.Add((SftpFile)sftpFile);
//                    }
//                    else if (sftpFile.IsRegularFile &&
//                             _allowedExtensions.Contains(Path.GetExtension(sftpFile.Name).ToLowerInvariant()))
//                    {
//                        node.Files.Add(new FileDetail
//                        {
//                            Name = sftpFile.Name,
//                            FullPath = sftpFile.FullName,
//                            Size = sftpFile.Length,
//                            Created = sftpFile.LastWriteTime,
//                            Modified = sftpFile.LastWriteTime,
//                            Extension = Path.GetExtension(sftpFile.Name),
//                            //FormattedSize = FormatFileSize(sftpFile.Length)
//                        });
//                        fileCount++;
//                    }
//                }

//                node.ChildCount = subFolders.Count + fileCount;

//                // Recursively get subfolders (limit depth to avoid infinite recursion)
//                foreach (var subFolder in subFolders.Take(20)) // Limit to prevent performance issues
//                {
//                    try
//                    {
//                        var subNode = await GetFolderTreeAsync(subFolder.FullName);
//                        node.SubFolders.Add(subNode);
//                    }
//                    catch (Exception ex)
//                    {
//                        _logger.LogWarning(ex, "Could not access SFTP subfolder: {SubFolder}", subFolder.FullName);
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error building SFTP folder tree for: {RootPath}", rootPath);
//                node.ChildCount = 0;
//            }

//            return node;
//        }

//        public async Task<FileDetail> GetFileContentAsync(string path)
//        {
//            try
//            {
//                var client = await GetConnectedClientAsync();

//                // Get file attributes
//                var attributes = await Task.Run(() => client.GetAttributes(path));

//                if (attributes.Size > _maxFileSize)
//                {
//                    throw new InvalidOperationException("File is too large to display.");
//                }

//                // Download file content
//                using var memoryStream = new MemoryStream();
//                await Task.Run(() => client.DownloadFile(path, memoryStream));

//                var content = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
//                var lines = content.Split('\n').ToList();

//                return new FileDetail
//                {
//                    FullPath = path,
//                    Content = content,
//                    Lines = lines,
//                    Name = Path.GetFileName(path),
//                    Size = attributes.Size,
//                    //FormattedSize = FormatFileSize(attributes.Size),
//                    Created = attributes.LastWriteTime,
//                    Modified = attributes.LastWriteTime,
//                    Extension = Path.GetExtension(path)
//                };
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error getting SFTP file content: {Path}", path);
//                throw new InvalidOperationException($"Failed to get SFTP file content: {ex.Message}");
//            }
//        }

//        public async Task<bool> DeleteFileAsync(string path)
//        {
//            try
//            {
//                var client = await GetConnectedClientAsync();
//                await Task.Run(() => client.DeleteFile(path));
//                return true;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error deleting SFTP file: {Path}", path);
//                return false;
//            }
//        }

//        public async Task<bool> UploadFileAsync(string folderPath, string fileName, Stream fileStream)
//        {
//            try
//            {
//                var client = await GetConnectedClientAsync();
//                var filePath = CombineSftpPath(folderPath, fileName);

//                // Check if file already exists
//                if (await Task.Run(() => client.Exists(filePath)))
//                {
//                    return false; // File already exists
//                }

//                await Task.Run(() => client.UploadFile(fileStream, filePath));
//                return true;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error uploading file to SFTP: {FileName} to {FolderPath}", fileName, folderPath);
//                return false;
//            }
//        }

//        public async Task<bool> CreateFileAsync(string folderPath, string fileName)
//        {
//            try
//            {
//                var client = await GetConnectedClientAsync();
//                var filePath = CombineSftpPath(folderPath, fileName);

//                // Check if file already exists
//                if (await Task.Run(() => client.Exists(filePath)))
//                {
//                    return false; // File already exists
//                }

//                // Create empty file
//                using var emptyStream = new MemoryStream();
//                await Task.Run(() => client.UploadFile(emptyStream, filePath));

//                return true;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error creating SFTP file: {FileName} in {FolderPath}", fileName, folderPath);
//                return false;
//            }
//        }

//        private static string CombineSftpPath(string basePath, string fileName)
//        {
//            var path = basePath.TrimEnd('/') + "/" + fileName.TrimStart('/');
//            return path.StartsWith("/") ? path : "/" + path;
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

//        public void Dispose()
//        {
//            Dispose(true);
//            GC.SuppressFinalize(this);
//        }

//        protected virtual void Dispose(bool disposing)
//        {
//            if (!_disposed)
//            {
//                if (disposing)
//                {
//                    _sftpClient?.Dispose();
//                }
//                _disposed = true;
//            }
//        }
//    }
//}
using FileExplorerApplicationV1.Models;
using FileExplorerApplicationV1.Services;
using Microsoft.Extensions.Configuration;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FileExplorerApplicationV1.Services
{
    public class SftpFileProviderService : IFileProviderService
    {
        private readonly SftpClient _client;
        private readonly string _remoteBase;

        public SftpFileProviderService(IConfiguration config)
        {
            var sftpConfig = config.GetSection("SFTP");
            _remoteBase = sftpConfig["RemoteDir"] ?? "/";

            _client = new SftpClient(
                sftpConfig["Host"],
                sftpConfig.GetValue<int>("Port"),
                sftpConfig["Username"],
                sftpConfig["Password"]
            );
            _client.Connect();
        }

        public string GetBasePath() => _remoteBase;

        // --- Interface implementations ---
        public IEnumerable<FolderNode> GetFolderTree(string path = null)
        {
            var target = path ?? _remoteBase;
            var node = new FolderNode
            {
                Name = Path.GetFileName(target.TrimEnd('/')) == string.Empty ? target : Path.GetFileName(target),
                FullPath = target
            };

            try
            {
                foreach (var item in _client.ListDirectory(target))
                {
                    if (item.IsDirectory && item.Name != "." && item.Name != "..")
                    {
                        node.SubFolders.AddRange(GetFolderTree(item.FullName));
                    }
                }
            }
            catch { /* ignore errors */ }

            return new List<FolderNode> { node };
        }

        public IEnumerable<FileDetail> GetFiles(string path)
        {
            var target = path ?? _remoteBase;
            var list = new List<FileDetail>();

            foreach (var item in _client.ListDirectory(target))
            {
                if (!item.IsDirectory)
                {
                    list.Add(new FileDetail
                    {
                        Name = item.Name,
                        FullPath = item.FullName,
                        Size = item.Attributes.Size,
                        Modified = item.Attributes.LastWriteTime,
                        Created = item.Attributes.LastWriteTime, // SFTP may not provide creation time
                        Extension = Path.GetExtension(item.Name)
                    });
                }
            }

            return list;
        }

        public FileDetail GetFileDetail(string fullPath)
        {
            var attr = _client.GetAttributes(fullPath);
            if (attr == null) return null;

            return new FileDetail
            {
                Name = Path.GetFileName(fullPath),
                FullPath = fullPath,
                Size = attr.Size,
                Modified = attr.LastWriteTime,
                Created = attr.LastWriteTime,
                Extension = Path.GetExtension(fullPath)
            };
        }

        public string GetFileContent(string fullPath)
        {
            using var ms = new MemoryStream();
            _client.DownloadFile(fullPath, ms);
            ms.Position = 0;
            using var reader = new StreamReader(ms);
            return reader.ReadToEnd();
        }

        public void SaveFileContent(string fullPath, string content)
        {
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(content));
            _client.UploadFile(ms, fullPath, true);
        }

        public void CreateFile(string folderPath, string fileName, string content)
        {
            var path = $"{folderPath.TrimEnd('/')}/{fileName}";
            SaveFileContent(path, content);
        }

        public void DeleteFile(string fullPath)
        {
            _client.DeleteFile(fullPath);
        }
    }
}
