//using FileExplorerApplicationV1.Models;
//using FluentFTP;
//using Microsoft.Extensions.Options;
//using System.IO;

//public class FileService
//{
//    private readonly FileExplorerSettings _fileSettings;
//    private readonly FtpSetting _ftpSettings;

//    public FileService(IOptions<FileExplorerSettings> fileOptions, IOptions<FtpSettings> ftpOptions)
//    {
//        _fileSettings = fileOptions.Value;
//        _ftpSettings = ftpOptions.Value;
//    }

//    /// <summary>
//    /// Upload a file to local folder or FTP
//    /// </summary>
//    public async Task UploadFileAsync(string fileName, Stream fileStream)
//    {
//        if (_ftpSettings.Enabled)
//        {
//            var tempFile = Path.GetTempFileName();
//            try
//            {
//                // Save stream to temporary file
//                using (var fs = File.Create(tempFile))
//                {
//                    await fileStream.CopyToAsync(fs);
//                }

//                using var client = new FtpClient(_ftpSettings.Host, _ftpSettings.Username, _ftpSettings.Password);
//                client.Connect();

//                // Upload temp file to FTP using FtpRemoteExists enum
//                client.UploadFile(tempFile, $"{_ftpSettings.RemoteDir}/{fileName}", FtpRemoteExists.Overwrite);

//                client.Disconnect();
//            }
//            finally
//            {
//                if (File.Exists(tempFile))
//                    File.Delete(tempFile);
//            }
//        }
//        else
//        {
//            var localPath = Path.Combine(_fileSettings.RootPath, fileName);
//            Directory.CreateDirectory(_fileSettings.RootPath);

//            using var file = File.Create(localPath);
//            await fileStream.CopyToAsync(file);
//        }
//    }

//    /// <summary>
//    /// List files from local folder or FTP
//    /// </summary>
//    public Task<List<string>> GetFilesAsync()
//    {
//        if (_ftpSettings.Enabled)
//        {
//            using var client = new FtpClient(_ftpSettings.Host, _ftpSettings.Username, _ftpSettings.Password);
//            client.Connect();

//            var items = client.GetNameListing(_ftpSettings.RemoteDir);
//            client.Disconnect();

//            return Task.FromResult(items.Select(Path.GetFileName).ToList());
//        }
//        else
//        {
//            if (!Directory.Exists(_fileSettings.RootPath))
//                return Task.FromResult(new List<string>());

//            var files = Directory.GetFiles(_fileSettings.RootPath)
//                                 .Select(Path.GetFileName)
//                                 .ToList();

//            return Task.FromResult(files);
//        }
//    }

//    /// <summary>
//    /// Download a file from local folder or FTP as Stream
//    /// </summary>
//    public async Task<Stream> DownloadFileAsync(string fileName)
//    {
//        if (_ftpSettings.Enabled)
//        {
//            var tempFile = Path.GetTempFileName();
//            try
//            {
//                using var client = new FtpClient(_ftpSettings.Host, _ftpSettings.Username, _ftpSettings.Password);
//                client.Connect();

//                client.DownloadFile(tempFile, $"{_ftpSettings.RemoteDir}/{fileName}");
//                client.Disconnect();

//                var memoryStream = new MemoryStream();
//                using (var fs = File.OpenRead(tempFile))
//                {
//                    await fs.CopyToAsync(memoryStream);
//                }
//                memoryStream.Position = 0;
//                return memoryStream;
//            }
//            finally
//            {
//                if (File.Exists(tempFile))
//                    File.Delete(tempFile);
//            }
//        }
//        else
//        {
//            var localPath = Path.Combine(_fileSettings.RootPath, fileName);
//            if (!File.Exists(localPath)) return null;

//            var memoryStream = new MemoryStream();
//            using (var fs = File.OpenRead(localPath))
//            {
//                await fs.CopyToAsync(memoryStream);
//            }
//            memoryStream.Position = 0;
//            return memoryStream;
//        }
//    }

//    /// <summary>
//    /// Delete a file from local folder or FTP
//    /// </summary>
//    public Task DeleteFileAsync(string fileName)
//    {
//        if (_ftpSettings.Enabled)
//        {
//            using var client = new FtpClient(_ftpSettings.Host, _ftpSettings.Username, _ftpSettings.Password);
//            client.Connect();
//            client.DeleteFile($"{_ftpSettings.RemoteDir}/{fileName}");
//            client.Disconnect();
//            return Task.CompletedTask;
//        }
//        else
//        {
//            var localPath = Path.Combine(_fileSettings.RootPath, fileName);
//            if (File.Exists(localPath))
//                File.Delete(localPath);
//            return Task.CompletedTask;
//        }
//    }
//}
