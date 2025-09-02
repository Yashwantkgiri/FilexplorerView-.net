//using FileExplorerApplicationV1.Models;
//using FileExplorerApplicationV1.Services.Interfaces;
//using FileExplorerApplicationV1.Services.Providers;
//using Microsoft.Extensions.Options;

//namespace FileExplorerApplicationV1.Services
//{
//    public class FileProviderFactory
//    {
//        private readonly IServiceProvider _serviceProvider;
//        private readonly ILogger<FileProviderFactory> _logger;
//        private readonly FtpSetting _ftpSettings;
//        private readonly SftpSetting _sftpSettings;

//        public FileProviderFactory(
//            IServiceProvider serviceProvider,
//            ILogger<FileProviderFactory> logger,
//            IOptions<FtpSetting> ftpSettings,
//            IOptions<SftpSetting> sftpSettings)
//        {
//            _serviceProvider = serviceProvider;
//            _logger = logger;
//            _ftpSettings = ftpSettings.Value;
//            _sftpSettings = sftpSettings.Value;
//        }

//        public IFileProviderService CreateProvider(FileProviderType providerType)
//        {
//            try
//            {
//                return providerType switch
//                {
//                    FileProviderType.Local => _serviceProvider.GetRequiredService<LocalFileProviderService>(),
//                    FileProviderType.Ftp when _ftpSettings.Enabled =>
//                        _serviceProvider.GetRequiredService<FtpFileProviderService>(),
//                    FileProviderType.Sftp when _sftpSettings.Enabled =>
//                        _serviceProvider.GetRequiredService<SftpFileProviderService>(),
//                    FileProviderType.Ftp when !_ftpSettings.Enabled =>
//                        throw new InvalidOperationException("FTP provider is disabled in configuration"),
//                    FileProviderType.Sftp when !_sftpSettings.Enabled =>
//                        throw new InvalidOperationException("SFTP provider is disabled in configuration"),
//                    _ => throw new ArgumentException($"Unsupported provider type: {providerType}")
//                };
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Failed to create file provider of type: {ProviderType}", providerType);
//                throw;
//            }
//        }

//        public async Task<List<(FileProviderType Type, string Name, bool IsAvailable)>> GetAvailableProvidersAsync()
//        {
//            var providers = new List<(FileProviderType Type, string Name, bool IsAvailable)>();

//            // Always include Local provider
//            try
//            {
//                var localProvider = _serviceProvider.GetRequiredService<LocalFileProviderService>();
//                var isLocalAvailable = await localProvider.TestConnectionAsync();
//                providers.Add((FileProviderType.Local, localProvider.ProviderName, isLocalAvailable));
//            }
//            catch (Exception ex)
//            {
//                _logger.LogWarning(ex, "Local provider is not available");
//                providers.Add((FileProviderType.Local, "Local File System", false));
//            }

//            // Add FTP provider if enabled
//            if (_ftpSettings.Enabled)
//            {
//                try
//                {
//                    var ftpProvider = _serviceProvider.GetRequiredService<FtpFileProviderService>();
//                    var isFtpAvailable = await ftpProvider.TestConnectionAsync();
//                    providers.Add((FileProviderType.Ftp, ftpProvider.ProviderName, isFtpAvailable));
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogWarning(ex, "FTP provider is not available");
//                    providers.Add((FileProviderType.Ftp, $"FTP - {_ftpSettings.Host}", false));
//                }
//            }

//            // Add SFTP provider if enabled
//            if (_sftpSettings.Enabled)
//            {
//                try
//                {
//                    var sftpProvider = _serviceProvider.GetRequiredService<SftpFileProviderService>();
//                    var isSftpAvailable = await sftpProvider.TestConnectionAsync();
//                    providers.Add((FileProviderType.Sftp, sftpProvider.ProviderName, isSftpAvailable));
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogWarning(ex, "SFTP provider is not available");
//                    providers.Add((FileProviderType.Sftp, $"SFTP - {_sftpSettings.Host}", false));
//                }
//            }

//            return providers;
//        }

//        public bool IsProviderEnabled(FileProviderType providerType)
//        {
//            return providerType switch
//            {
//                FileProviderType.Local => true, // Always enabled
//                FileProviderType.Ftp => _ftpSettings.Enabled,
//                FileProviderType.Sftp => _sftpSettings.Enabled,
//                _ => false
//            };
//        }
//    }
//}