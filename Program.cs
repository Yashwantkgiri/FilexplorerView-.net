
using FileExplorerApplicationV1.Models;
using FileExplorerApplicationV1.Services;
using Microsoft.Extensions.Options;

namespace FileExplorerApplicationV1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container
            builder.Services.AddControllersWithViews();

            // Add session support for provider selection
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // Add HTTP context accessor for session access
            builder.Services.AddHttpContextAccessor();

            // Configure settings from appsettings.json
            builder.Services.Configure<FileExplorerSettings>(
                builder.Configuration.GetSection("FileExplorerSettings"));
            builder.Services.Configure<FileSetting>(
                builder.Configuration.GetSection("FileSettings"));
            builder.Services.Configure<FtpSetting>(
                builder.Configuration.GetSection("FTP"));
            builder.Services.Configure<SftpSetting>(
                builder.Configuration.GetSection("SFTP"));

            // Register provider selector
            builder.Services.AddScoped<IProviderSelector, ProviderSelector>();

            // Register file provider services
            builder.Services.AddScoped<LocalFileProviderService>(serviceProvider =>
            {
                var settings = serviceProvider.GetRequiredService<IOptions<FileExplorerSettings>>().Value;
                var allowedExtensions = builder.Configuration
                    .GetSection("FileUploadSettings:AllowedUploadExtensions")
                    .Get<List<string>>() ?? new List<string> { ".txt", ".json", ".xml", ".pdf", ".doc", ".docx", ".csv" };

                return new LocalFileProviderService(settings.RootPath, allowedExtensions);
            });

            builder.Services.AddScoped<FtpFileProviderService>(serviceProvider =>
            {
                return new FtpFileProviderService(builder.Configuration);
            });

            builder.Services.AddScoped<SftpFileProviderService>(serviceProvider =>
            {
                return new SftpFileProviderService(builder.Configuration);
            });

            // Add logging
            builder.Services.AddLogging(logging =>
            {
                logging.AddConsole();
                logging.AddDebug();
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // Enable session middleware (must be before UseAuthorization)
            app.UseSession();

            app.UseAuthorization();

            // Configure routes
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.MapControllerRoute(
                name: "api",
                pattern: "api/{controller}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
