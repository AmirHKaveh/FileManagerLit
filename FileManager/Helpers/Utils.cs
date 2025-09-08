using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FileManagerLite
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
        public object Result { get; set; }

        public FileManagerResult(int statusCode, string message, bool isSucceed = false, object result = null)
        {
            this.StatusCode = statusCode;
            this.Message = message;
            this.IsSucceed = isSucceed;
            this.Result = result;
        }
    }
}
