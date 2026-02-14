using FileManagerLiteUsage.Models;

using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FileManagerLite
{
    public enum AllowExtensionsFileManager
    {
        jpg, jpeg, gif, png, webp, svg, txt, docx, pdf, zip, rar, mp3, mp4, mkv, xlsx, apk
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

    public class ResizeResponseModel
    {
        public int StatusCode { get; set; }
        public string? Message { get; set; }
        public ResizeFileResponseModel? File { get; set; }
        public ResizeResponseModel(int statusCode, string message, ResizeFileResponseModel file = null)
        {
            this.StatusCode = statusCode;
            this.Message = message;
            this.File = file;
        }

    }

    public struct ResizeParams
    {
        public bool hasParams;
        public int w;
        public int h;
        public bool autorotate;
        public int quality; // 0 - 100
        public string format; // png, jpg, jpeg
        public string? mode; // pad, max, crop, stretch
        public string type;
        public short wmtext;
        public short wmimage;
    }
}
