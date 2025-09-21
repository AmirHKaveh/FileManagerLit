namespace FileManagerLite.Models
{
    public class DirectoryResponseDto
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public long Size { get; set; }
        public int SubscriptionsCount { get; set; }
        public bool IsDirectory { get; set; }
        public bool HasSubDirectories { get; set; }
        public DateTime DateModified { get; set; }
    }

    public class FileManagerSearchRequest
    {
        public string? Keyword { get; set; }
        public List<AllowExtensionsFileManager>? Extensions { get; set; }
        public bool IsRecursive { get; set; } = true;
    }
}
