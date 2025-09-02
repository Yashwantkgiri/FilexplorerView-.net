//using FileExplorerApplicationV1.Models;
//using FileExplorerApplicationV1.Services.Interfaces;
//using Microsoft.Extensions.Options;
//using System.Net;
//using System.Text;

//namespace FileExplorerApplicationV1.Services.Providers
//{
//    public class FtpFileProviderService : IFileProviderService
//    {
//        private readonly FtpSetting _ftpSettings;
//        private readonly string[] _allowedExtensions;
//        private readonly long _maxFileSize;
//        private readonly ILogger<FtpFileProviderService> _logger;

//        public string ProviderName => $"FTP - {_ftpSettings.Host}";

//        public FtpFileProviderService(
//            IOptions<FtpSetting> ftpSettings,
//            IOptions<FileSetting> fileSettings,
//            ILogger<FtpFileProviderService> logger)
//        {
//            _ftpSettings = ftpSettings.Value;
//            _allowedExtensions = fileSettings.Value.AllowedFileExtensions ?? new[] { ".xml", ".txt", ".pdf", ".doc", ".json" };
//            _maxFileSize = 10 * 1024 * 1024; // 10MB
//            _logger = logger;
//        }

//        public async Task<bool> TestConnectionAsync()
//        {
//            try
//            {
//                var request = CreateFtpRequest("/", WebRequestMethods.Ftp.ListDirectory);
//                using var response = (FtpWebResponse)await request.GetResponseAsync();
//                return response.StatusCode == FtpStatusCode.OpeningData || response.StatusCode == FtpStatusCode.DataAlreadyOpen;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "FTP connection test failed for host: {Host}", _ftpSettings.Host);
//                return false;
//            }
//        }

//        public bool IsValidPath(string path)
//        {
//            if (string.IsNullOrWhiteSpace(path))
//                return false;

//            // Basic path validation for FTP
//            return !path.Contains("../") && !path.Contains("..\\");
//        }

//        public async Task<List<FileDetail>> GetFolderContentsAsync(string path)
//        {
//            var files = new List<FileDetail>();

//            try
//            {
//                var request = CreateFtpRequest(path, WebRequestMethods.Ftp.ListDirectoryDetails);

//                using var response = (FtpWebResponse)await request.GetResponseAsync();
//                using var stream = response.GetResponseStream();
//                using var reader = new StreamReader(stream);

//                string line;
//                while ((line = await reader.ReadLineAsync()) != null)
//                {
//                    var fileDetail = ParseFtpListLine(line, path);
//                    if (fileDetail != null && !fileDetail.IsDirectory &&
//                        _allowedExtensions.Contains(Path.GetExtension(fileDetail.Name).ToLowerInvariant()))
//                    {
//                        files.Add(fileDetail);
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error getting FTP folder contents for path: {Path}", path);
//                throw new InvalidOperationException($"Failed to get FTP folder contents: {ex.Message}");
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
//                var request = CreateFtpRequest(rootPath, WebRequestMethods.Ftp.ListDirectoryDetails);

//                using var response = (FtpWebResponse)await request.GetResponseAsync();
//                using var stream = response.GetResponseStream();
//                using var reader = new StreamReader(stream);

//                string line;
//                var subFolders = new List<string>();
//                var fileCount = 0;

//                while ((line = await reader.ReadLineAsync()) != null)
//                {
//                    var fileDetail = ParseFtpListLine(line, rootPath);
//                    if (fileDetail != null)
//                    {
//                        if (fileDetail.IsDirectory)
//                        {
//                            subFolders.Add(fileDetail.FullPath);
//                        }
//                        else if (_allowedExtensions.Contains(Path.GetExtension(fileDetail.Name).ToLowerInvariant()))
//                        {
//                            node.Files.Add(fileDetail);
//                            fileCount++;
//                        }
//                    }
//                }

//                node.ChildCount = subFolders.Count + fileCount;

//                // Recursively get subfolders (limit depth to avoid infinite recursion)
//                foreach (var subFolder in subFolders.Take(20)) // Limit to prevent performance issues
//                {
//                    try
//                    {
//                        var subNode = await GetFolderTreeAsync(subFolder);
//                        node.SubFolders.Add(subNode);
//                    }
//                    catch (Exception ex)
//                    {
//                        _logger.LogWarning(ex, "Could not access subfolder: {SubFolder}", subFolder);
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error building FTP folder tree for: {RootPath}", rootPath);
//                node.ChildCount = 0;
//            }

//            return node;
//        }

//        public async Task<FileDetail> GetFileContentAsync(string path)
//        {
//            try
//            {
//                // Get file info first
//                var fileInfo = await GetFileInfoAsync(path);
//                if (fileInfo == null)
//                {
//                    throw new FileNotFoundException($"FTP file not found: {path}");
//                }

//                if (fileInfo.Size > _maxFileSize)
//                {
//                    throw new InvalidOperationException("File is too large to display.");
//                }

//                // Download file content
//                var request = CreateFtpRequest(path, WebRequestMethods.Ftp.DownloadFile);

//                using var response = (FtpWebResponse)await request.GetResponseAsync();
//                using var stream = response.GetResponseStream();
//                using var memoryStream = new MemoryStream();

//                await stream.CopyToAsync(memoryStream);
//                var content = Encoding.UTF8.GetString(memoryStream.ToArray());

//                var lines = content.Split('\n').ToList();

//                return new FileDetail
//                {
//                    FullPath = path,
//                    Content = content,
//                    Lines = lines,
//                    Name = Path.GetFileName(path),
//                    Size = fileInfo.Size,
//                    //FormattedSize = FormatFileSize(fileInfo.Size),
//                    Created = fileInfo.Created,
//                    Modified = fileInfo.Modified,
//                    Extension = Path.GetExtension(path)
//                };
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error getting FTP file content: {Path}", path);
//                throw new InvalidOperationException($"Failed to get FTP file content: {ex.Message}");
//            }
//        }

//        public async Task<bool> DeleteFileAsync(string path)
//        {
//            try
//            {
//                var request = CreateFtpRequest(path, WebRequestMethods.Ftp.DeleteFile);

//                using var response = (FtpWebResponse)await request.GetResponseAsync();
//                return response.StatusCode == FtpStatusCode.FileActionOK;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error deleting FTP file: {Path}", path);
//                return false;
//            }
//        }

//        public async Task<bool> UploadFileAsync(string folderPath, string fileName, Stream fileStream)
//        {
//            try
//            {
//                var filePath = CombineFtpPath(folderPath, fileName);
//                var request = CreateFtpRequest(filePath, WebRequestMethods.Ftp.UploadFile);

//                using var requestStream = await request.GetRequestStreamAsync();
//                await fileStream.CopyToAsync(requestStream);

//                using var response = (FtpWebResponse)await request.GetResponseAsync();
//                return response.StatusCode == FtpStatusCode.ClosingData;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error uploading file to FTP: {FileName} to {FolderPath}", fileName, folderPath);
//                return false;
//            }
//        }

//        public async Task<bool> CreateFileAsync(string folderPath, string fileName)
//        {
//            try
//            {
//                var filePath = CombineFtpPath(folderPath, fileName);
//                var request = CreateFtpRequest(filePath, WebRequestMethods.Ftp.UploadFile);

//                using var requestStream = await request.GetRequestStreamAsync();
//                // Create empty file by uploading empty content
//                var emptyContent = Encoding.UTF8.GetBytes("");
//                await requestStream.WriteAsync(emptyContent);

//                using var response = (FtpWebResponse)await request.GetResponseAsync();
//                return response.StatusCode == FtpStatusCode.ClosingData;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error creating FTP file: {FileName} in {FolderPath}", fileName, folderPath);
//                return false;
//            }
//        }

//        private FtpWebRequest CreateFtpRequest(string path, string method)
//        {
//            var fullUrl = $"ftp://{_ftpSettings.Host}:{_ftpSettings.Port}{path}";
//            var request = (FtpWebRequest)WebRequest.Create(fullUrl);

//            request.Method = method;
//            request.Credentials = new NetworkCredential(_ftpSettings.Username, _ftpSettings.Password);
//            request.UsePassive = true;
//            request.UseBinary = true;
//            request.KeepAlive = false;

//            return request;
//        }

//        private async Task<FileDetail> GetFileInfoAsync(string path)
//        {
//            try
//            {
//                var sizeRequest = CreateFtpRequest(path, WebRequestMethods.Ftp.GetFileSize);
//                using var sizeResponse = (FtpWebResponse)await sizeRequest.GetResponseAsync();
//                var size = sizeResponse.ContentLength;

//                var dateRequest = CreateFtpRequest(path, WebRequestMethods.Ftp.GetDateTimestamp);
//                using var dateResponse = (FtpWebResponse)await dateRequest.GetResponseAsync();
//                var lastModified = dateResponse.LastModified;

//                return new FileDetail
//                {
//                    Name = Path.GetFileName(path),
//                    FullPath = path,
//                    Size = size,
//                    Modified = lastModified,
//                    Created = lastModified, // FTP typically doesn't distinguish creation from modification
//                    Extension = Path.GetExtension(path),
//                    //FormattedSize = FormatFileSize(size)
//                };
//            }
//            catch
//            {
//                return null;
//            }
//        }

//        private FileDetail ParseFtpListLine(string line, string basePath)
//        {
//            if (string.IsNullOrWhiteSpace(line))
//                return null;

//            try
//            {
//                // Parse Unix-style listing (most common)
//                // Example: drwxr-xr-x   2 user group      4096 Jan 01 12:00 filename
//                var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

//                if (parts.Length < 9)
//                    return null;

//                var permissions = parts[0];
//                var isDirectory = permissions.StartsWith("d");
//                var sizeStr = parts[4];
//                var fileName = string.Join(" ", parts.Skip(8));

//                if (!long.TryParse(sizeStr, out var size))
//                    size = 0;

//                // Try to parse date
//                var dateStr = $"{parts[5]} {parts[6]} {parts[7]}";
//                if (!DateTime.TryParse(dateStr, out var modifiedDate))
//                    modifiedDate = DateTime.Now;

//                var fullPath = CombineFtpPath(basePath, fileName);

//                return new FileDetail
//                {
//                    Name = fileName,
//                    FullPath = fullPath,
//                    Size = size,
//                    Modified = modifiedDate,
//                    Created = modifiedDate,
//                    Extension = Path.GetExtension(fileName),
//                    //FormattedSize = FormatFileSize(size),
//                    //IsDirectory = isDirectory
//                };
//            }
//            catch (Exception ex)
//            {
//                _logger.LogWarning(ex, "Could not parse FTP list line: {Line}", line);
//                return null;
//            }
//        }

//        private static string CombineFtpPath(string basePath, string fileName)
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
//    }
//}
using FileExplorerApplicationV1.Models;
using FileExplorerApplicationV1.Services;

using FluentFTP;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FileExplorerApplicationV1.Services
{
    public class FtpFileProviderService : IFileProviderService
    {
        private readonly FtpClient _client;
        private readonly string _remoteBase;

        public FtpFileProviderService(IConfiguration config)
        {
            var ftpConfig = config.GetSection("FTP");

            _remoteBase = ftpConfig["RemoteDir"] ?? "/";

            _client = new FtpClient
            {
                Host = ftpConfig["Host"],
                Port = ftpConfig.GetValue<int>("Port"),
                Credentials = new System.Net.NetworkCredential(
                    ftpConfig["Username"],
                    ftpConfig["Password"]
                )
            };
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
                foreach (var item in _client.GetListing(target))
                {
                    if (item.Type == FtpObjectType.Directory)
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
            var list = new List<FileDetail>();
            var target = path ?? _remoteBase;

            foreach (var item in _client.GetListing(target))
            {
                if (item.Type == FtpObjectType.File)
                {
                    list.Add(new FileDetail
                    {
                        Name = item.Name,
                        FullPath = item.FullName,
                        Size = item.Size,
                        Modified = item.Modified,
                        Created = item.Modified, // FTP does not provide creation date
                        Extension = Path.GetExtension(item.Name)
                    });
                }
            }
            return list;
        }

        public FileDetail GetFileDetail(string fullPath)
        {
            var info = _client.GetObjectInfo(fullPath);
            if (info == null) return null;

            return new FileDetail
            {
                Name = info.Name,
                FullPath = info.FullName,
                Size = info.Size,
                Modified = info.Modified,
                Created = info.Modified,
                Extension = Path.GetExtension(info.Name)
            };
        }

        public string GetFileContent(string fullPath)
        {
            using var ms = new MemoryStream();
            _client.DownloadStream(ms, fullPath);
            ms.Position = 0;
            using var reader = new StreamReader(ms);
            return reader.ReadToEnd();
        }

        public void SaveFileContent(string fullPath, string content)
        {
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(content));
            _client.UploadStream(ms, fullPath, FtpRemoteExists.Overwrite, true);
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
