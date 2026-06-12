using System.IO;

namespace Wasl.Application.Common.Models
{
    public class UploadedFile
    {
        public Stream Content { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long Length { get; set; }
    }
}