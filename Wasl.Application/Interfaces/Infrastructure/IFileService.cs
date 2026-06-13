using Wasl.Application.Common.Models;

namespace Wasl.Application.Interfaces.Infrastructure
{
    public interface IFileService
    {
        Task<string> SaveFileAsync(UploadedFile file, string folderName, CancellationToken cancellationToken);

        void DeleteFile(string fileUrl);
    }
}
