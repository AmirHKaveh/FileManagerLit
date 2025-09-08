using System.ComponentModel.DataAnnotations;

namespace FileManager.Models
{
    public class FileZipRequestModel
    {
        [Display(Name = "مسیر فایل ها")]
        public List<string> FilePaths { get; set; }
        [Display(Name = "مسیر فولدر")]
        public string DirectoryPath { get; set; }
        [Display(Name = "نام فایل zip")]
        [Required(ErrorMessage = "لطفا {0} را وارد نمایید")]
        public string FileZipName { get; set; }
    }
}
