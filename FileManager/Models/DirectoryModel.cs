namespace FileManager.Models
{
    public class DirectoryResponseDto
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public long Size { get; set; }
        public bool IsDirectory { get; set; }
        public bool HasSubDirectories { get; set; }
        public DateTime DateModified { get; set; }
    }
}
