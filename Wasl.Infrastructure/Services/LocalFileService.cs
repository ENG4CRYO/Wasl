using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Wasl.Application.Common.Models;
using Wasl.Application.Interfaces.Infrastructure;

namespace Wasl.Infrastructure.Services
{
    public class LocalFileService : IFileService
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LocalFileService> _logger;

        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".pdf" };
        private const long MaxFileSizeBytes = 10 * 1024 * 1024; 

        public LocalFileService(
            IWebHostEnvironment webHostEnvironment,
            IConfiguration configuration,
            ILogger<LocalFileService> logger)
        {
            _webHostEnvironment = webHostEnvironment;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> SaveFileAsync(UploadedFile file, string folderName, CancellationToken cancellationToken = default)
        {
            if (file == null || file.Length == 0 || file.Content == null)
                throw new ArgumentException("File is empty or not provided.");

            if (file.Length > MaxFileSizeBytes)
                throw new ArgumentException("File exceeds the 10MB limit.");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
                throw new ArgumentException($"File type '{ext}' is not allowed.");

            string webRootPath = _webHostEnvironment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            string uploadsFolder = Path.Combine(webRootPath, folderName);
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            string uniqueFileName = Guid.NewGuid().ToString() + ext;
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                if (file.Content.CanSeek) file.Content.Position = 0; 
                await file.Content.CopyToAsync(fileStream, cancellationToken);
            }

            string domainUrl = _configuration["DomainUrl"] ?? "http://localhost:5040";
            return $"{domainUrl.TrimEnd('/')}/{folderName.Replace("\\", "/")}/{uniqueFileName}";
        }

        public void DeleteFile(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl)) return;

            try
            {
                var uri = new Uri(fileUrl);
                var filePath = Path.Combine(_webHostEnvironment.WebRootPath, uri.AbsolutePath.TrimStart('/'));

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _logger.LogInformation("File deleted successfully: {FilePath}", filePath);
                }
                else
                {
                    _logger.LogWarning("Attempted to delete file that does not exist: {FilePath}", filePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while deleting the file from URL: {FileUrl}", fileUrl);
            }
        }
    }
}