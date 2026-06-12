using Microsoft.AspNetCore.Http;
using Wasl.Application.Common.Models;

namespace Wasl.API.Extensions
{
    public static class FormFileExtensions
    {
        public static UploadedFile? ToUploadedFile(this IFormFile? file)
        {
            if (file == null || file.Length == 0) return null;

            return new UploadedFile
            {
                Content = file.OpenReadStream(),
                FileName = file.FileName,
                ContentType = file.ContentType,
                Length = file.Length
            };
        }
    }
}