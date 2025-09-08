using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FileManager.Helpers
{
    public enum AllowExtensionsFileManager
    {
        jpg, jpeg, gif, png, webp, txt, docx, pdf, zip, rar, mp3, mp4, mkv
    }
    public class FileManagerResult
    {
        public bool IsSucceed { get; set; }
        public int StatusCode { get; set; }
        public string Message { get; set; }

        public FileManagerResult(int statusCode, string message, bool isSucceed = false)
        {
            this.StatusCode = statusCode;
            this.Message = message;
            this.IsSucceed = isSucceed;
        }
    }
}
