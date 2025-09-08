using Microsoft.AspNetCore.Http;

namespace FileManagerLite.Models
{
    public class UploadFileManagerRequestModel
    {
        public List<IFormFile> Files { get; set; }
        public string CurrentPath { get; set; }
    }
}
