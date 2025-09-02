using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using FileExplorerApplicationV1.Models;
using FileExplorerApplicationV1.Services;

namespace FileExplorerApplicationV1.Controllers
{
    public class HomeController : Controller
    {
        private readonly IProviderSelector _providerSelector;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            IProviderSelector providerSelector,
            IServiceProvider serviceProvider,
            ILogger<HomeController> logger)
        {
            _providerSelector = providerSelector;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public IActionResult Index()
        {
            try
            {
                var currentProvider = _providerSelector.CurrentProvider;
                var fileService = GetFileProviderService(currentProvider);

                if (fileService == null)
                {
                    _logger.LogError("Could not create file provider for: {Provider}", currentProvider);
                    return View(CreateErrorViewModel("Failed to initialize file provider"));
                }

                var rootPath = fileService.GetBasePath();
                var rootNodes = fileService.GetFolderTree(rootPath).ToList();
                var rootNode = rootNodes.FirstOrDefault() ?? new FolderNode
                {
                    Name = "Root",
                    FullPath = rootPath,
                    ChildCount = 0
                };

                var viewModel = new FileExplorerViewModel
                {
                    RootFolder = rootNode,
                    SelectedFile = null
                };

                ViewBag.CurrentProvider = currentProvider;
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Index action");
                return View(CreateErrorViewModel($"Error loading file explorer: {ex.Message}"));
            }
        }

        [HttpPost]
        public IActionResult SetProvider(string provider)
        {
            try
            {
                if (string.IsNullOrEmpty(provider))
                {
                    return BadRequest("Provider name is required");
                }

                _providerSelector.SetProvider(provider);
                _logger.LogInformation("Provider changed to: {Provider}", provider);

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting provider to: {Provider}", provider);
                return RedirectToAction("Index");
            }
        }

        private IFileProviderService GetFileProviderService(string providerName)
        {
            return providerName?.ToLower() switch
            {
                "local" => _serviceProvider.GetService<LocalFileProviderService>(),
                "ftp" => _serviceProvider.GetService<FtpFileProviderService>(),
                "sftp" => _serviceProvider.GetService<SftpFileProviderService>(),
                _ => _serviceProvider.GetService<LocalFileProviderService>()
            };
        }

        private FileExplorerViewModel CreateErrorViewModel(string errorMessage)
        {
            return new FileExplorerViewModel
            {
                RootFolder = new FolderNode { Name = "Error", FullPath = "", ChildCount = 0 },
                SelectedFile = null,
                ErrorMessage = errorMessage
            };
        }
    }
}