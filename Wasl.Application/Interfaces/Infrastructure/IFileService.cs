using Microsoft.AspNetCore.Http;

namespace Wasl.Application.Interfaces.Infrastructure
{
    public interface IFileService
    {
        Task<string> SaveFileAsync(IFormFile file, string folderName, CancellationToken cancellationToken);

        void DeleteFile(string fileUrl);
    }
}
