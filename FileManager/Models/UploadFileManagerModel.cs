using Microsoft.AspNetCore.Http;

namespace FileManagerLite.Models
{
    public class UploadFilesManagerRequestModel
    {
        public List<IFormFile> Files { get; set; }
        public string CurrentPath { get; set; }
        public bool IsRandomFileName { get; set; } = false;
    }

    public class UploadFileManagerRequestModel
    {
        public IFormFile File { get; set; }
        public string CurrentPath { get; set; }
        public bool IsRandomFileName { get; set; } = false;
    }

    public class UploadFileManagerResponseModel : FileManagerResult
    {
        public string FilePath { get; set; }
        public UploadFileManagerResponseModel(int statusCode, string message, bool isSucceed = false, string filePath = null) : base(statusCode, message, isSucceed)
        {
            FilePath = filePath;
        }
    }

    public class UploadFilesManagerResponseModel : FileManagerResult
    {
        public List<string> FilePaths { get; set; }
        public UploadFilesManagerResponseModel(int statusCode, string message, bool isSucceed = false, List<string> filePaths = null) : base(statusCode, message, isSucceed)
        {
            FilePaths = filePaths;
        }
    }
}
