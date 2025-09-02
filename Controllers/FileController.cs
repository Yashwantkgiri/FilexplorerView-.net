using FileExplorerApplicationV1.Models;
using FileExplorerApplicationV1.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class FileController : ControllerBase
{
    private readonly ILogger<FileController> _logger;
    private readonly IProviderSelector _providerSelector;
    private readonly IServiceProvider _serviceProvider;
    private readonly string[] _allowedExtensions = { ".txt", ".json", ".xml", ".pdf", ".doc", ".docx", ".csv" };
    private readonly long _maxFileSize = 10 * 1024 * 1024; // 10MB

    public FileController(
        ILogger<FileController> logger,
        IProviderSelector providerSelector,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _providerSelector = providerSelector;
        _serviceProvider = serviceProvider;
    }

    private IFileProviderService GetCurrentFileProvider()
    {
        var currentProvider = _providerSelector.CurrentProvider;
        return currentProvider?.ToLower() switch
        {
            "local" => _serviceProvider.GetService<LocalFileProviderService>(),
            "ftp" => _serviceProvider.GetService<FtpFileProviderService>(),
            "sftp" => _serviceProvider.GetService<SftpFileProviderService>(),
            _ => _serviceProvider.GetService<LocalFileProviderService>()
        };
    }

    [HttpGet("folder-contents")]
    public async Task<IActionResult> GetFolderContents([FromQuery] string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            _logger.LogWarning("Empty path provided");
            return BadRequest(new { message = "Path is required." });
        }

        try
        {
            var fileProvider = GetCurrentFileProvider();
            if (fileProvider == null)
            {
                return StatusCode(500, new { message = "File provider not available." });
            }

            var files = await Task.Run(() => fileProvider.GetFiles(path));

            var fileDetails = files.Select(f => new
            {
                name = f.Name,
                fullPath = f.FullPath,
                size = f.Size,
                formattedSize = FormatFileSize(f.Size),
                created = f.Created,
                modified = f.Modified,
                extension = f.Extension,
                isDirectory = false
            }).ToList();

            return Ok(fileDetails);
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogWarning("Unauthorized access to directory: {Path}", path);
            return StatusCode(403, new { message = "Access denied to this directory." });
        }
        catch (DirectoryNotFoundException)
        {
            _logger.LogWarning("Directory not found: {Path}", path);
            return NotFound(new { message = "Directory not found." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading directory contents: {Path}", path);
            return StatusCode(500, new { message = "An error occurred while reading directory contents." });
        }
    }

    [HttpGet("view-file")]
    public async Task<IActionResult> ViewFile([FromQuery] string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            _logger.LogWarning("Empty file path provided");
            return BadRequest(new { message = "File path is required." });
        }

        try
        {
            var fileProvider = GetCurrentFileProvider();
            if (fileProvider == null)
            {
                return StatusCode(500, new { message = "File provider not available." });
            }

            var fileDetail = await Task.Run(() => fileProvider.GetFileDetail(path));
            if (fileDetail == null)
            {
                _logger.LogWarning("File not found: {Path}", path);
                return NotFound(new { message = "File not found." });
            }

            // Check file size limit
            if (fileDetail.Size > _maxFileSize)
            {
                return BadRequest(new { message = "File is too large to display." });
            }

            var content = await Task.Run(() => fileProvider.GetFileContent(path));
            var lines = content?.Split('\n').ToList() ?? new List<string>();

            var viewModel = new
            {
                fullPath = fileDetail.FullPath,
                content = content,
                lines = lines,
                name = fileDetail.Name,
                size = fileDetail.Size,
                formattedSize = FormatFileSize(fileDetail.Size),
                created = fileDetail.Created,
                modified = fileDetail.Modified,
                extension = fileDetail.Extension
            };

            return Ok(viewModel);
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogWarning("Unauthorized access to file: {Path}", path);
            return StatusCode(403, new { message = "Access denied to this file." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading file: {Path}", path);
            return StatusCode(500, new { message = "An error occurred while reading the file." });
        }
    }

    [HttpDelete("delete-file")]
    public async Task<IActionResult> DeleteFile([FromQuery] string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            _logger.LogWarning("Empty file path for deletion");
            return BadRequest(new { message = "File path is required." });
        }

        try
        {
            var fileProvider = GetCurrentFileProvider();
            if (fileProvider == null)
            {
                return StatusCode(500, new { message = "File provider not available." });
            }

            var fileDetail = fileProvider.GetFileDetail(path);
            if (fileDetail == null)
            {
                _logger.LogWarning("File not found for deletion: {Path}", path);
                return NotFound(new { message = "File not found." });
            }

            await Task.Run(() => fileProvider.DeleteFile(path));
            _logger.LogInformation("File deleted successfully: {Path}", path);
            return Ok(new { message = "File deleted successfully." });
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogWarning("Unauthorized access for file deletion: {Path}", path);
            return StatusCode(403, new { message = "Access denied. Cannot delete this file." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {Path}", path);
            return StatusCode(500, new { message = "An error occurred while deleting the file." });
        }
    }

    [HttpPost("upload-file")]
    public async Task<IActionResult> UploadFile([FromForm] IFormFile file, [FromForm] string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            return BadRequest(new { message = "Folder path is required." });
        }

        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file provided or file is empty." });
        }

        if (file.Length > _maxFileSize)
        {
            return BadRequest(new { message = $"File size exceeds maximum limit of {FormatFileSize(_maxFileSize)}." });
        }

        var fileName = Path.GetFileName(file.FileName);
        if (string.IsNullOrWhiteSpace(fileName) || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            return BadRequest(new { message = "Invalid file name." });
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!_allowedExtensions.Contains(extension))
        {
            return BadRequest(new { message = $"File type not allowed. Allowed types: {string.Join(", ", _allowedExtensions)}" });
        }

        try
        {
            var fileProvider = GetCurrentFileProvider();
            if (fileProvider == null)
            {
                return StatusCode(500, new { message = "File provider not available." });
            }

            // For local files, we can check existence. For remote providers, we might need different logic
            var currentProvider = _providerSelector.CurrentProvider;
            if (currentProvider?.ToLower() == "local")
            {
                var filePath = Path.Combine(folderPath, fileName);
                if (System.IO.File.Exists(filePath))
                {
                    return Conflict(new { message = "A file with the same name already exists." });
                }
            }

            string content;
            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                stream.Position = 0;
                using (var reader = new StreamReader(stream))
                {
                    content = await reader.ReadToEndAsync();
                }
            }

            await Task.Run(() => fileProvider.CreateFile(folderPath, fileName, content));

            _logger.LogInformation("File uploaded successfully: {FileName} to {FolderPath}", fileName, folderPath);
            return Ok(new { message = "File uploaded successfully.", fileName = fileName });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file: {FileName}", fileName);
            return StatusCode(500, new { message = "An error occurred while uploading the file." });
        }
    }

    [HttpPost("create-file")]
    public async Task<IActionResult> CreateFile([FromForm] string folderPath, [FromForm] string fileName)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            return BadRequest(new { message = "Folder path is required." });
        }

        if (string.IsNullOrWhiteSpace(fileName) || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            return BadRequest(new { message = "Invalid file name." });
        }

        var allowedExtensions = new[] { ".txt", ".json", ".xml" };
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            return BadRequest(new { message = $"File type not allowed. Allowed: {string.Join(", ", allowedExtensions)}" });
        }

        try
        {
            var fileProvider = GetCurrentFileProvider();
            if (fileProvider == null)
            {
                return StatusCode(500, new { message = "File provider not available." });
            }

            await Task.Run(() => fileProvider.CreateFile(folderPath, fileName, ""));

            _logger.LogInformation("File created successfully: {FileName} in {FolderPath}", fileName, folderPath);
            return Ok(new { message = "File created successfully.", fileName });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating file: {FileName}", fileName);
            return StatusCode(500, new { message = "An error occurred while creating the file." });
        }
    }

    [HttpPost("set-provider")]
    public IActionResult SetProvider([FromForm] string provider)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(provider))
            {
                return BadRequest(new { message = "Provider name is required." });
            }

            _providerSelector.SetProvider(provider);
            _logger.LogInformation("Provider changed to: {Provider}", provider);

            return Ok(new { message = $"Provider changed to {provider}", provider });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting provider to: {Provider}", provider);
            return StatusCode(500, new { message = "An error occurred while changing provider." });
        }
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes == 0) return "0 B";
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}