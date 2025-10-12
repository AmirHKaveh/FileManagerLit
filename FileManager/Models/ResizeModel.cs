using System.ComponentModel.DataAnnotations;

namespace FileManagerLiteUsage.Models
{
    public class ResizeRequestModel
    {
        [Required(ErrorMessage = "Please enter {0}")]
        public required string ImagePath { get; set; }
        public string? Mode { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string? NewFileName { get; set; }

    }

    public class ResizeFileResponseModel
    {
        public required byte[] ContentData { get; set; }
        public required string ContentType { get; set; }
        public required string FileName { get; set; }
    }
}
