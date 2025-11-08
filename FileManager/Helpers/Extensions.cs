namespace FileManagerLite
{
    public static class Extensions
    {
        public static string GetSubstringToLastSlash(this string input)
        {
            // Find the position of the last slash
            int lastSlashIndex = input.LastIndexOf('\\'); // For Windows
            int lastSlashIndexForward = input.LastIndexOf('/'); // For Unix/Linux

            // Choose the one that occurs last
            int lastIndex = Math.Max(lastSlashIndex, lastSlashIndexForward);

            // If not found, return the original string or an empty string
            if (lastIndex == -1)
            {
                return string.Empty; // or return input if you prefer to keep the full string
            }

            // Extract substring from the start to the last slash
            return input.Substring(0, lastIndex + 1).TrimEnd('/');
        }
        public static string ToExtension(this AllowExtensionsFileManager ext)
        {
            return ext switch
            {
                AllowExtensionsFileManager.txt => ".txt",
                AllowExtensionsFileManager.pdf => ".pdf",
                AllowExtensionsFileManager.docx => ".docx",
                AllowExtensionsFileManager.xlsx => ".xlsx",
                AllowExtensionsFileManager.png => ".png",
                AllowExtensionsFileManager.jpg => ".jpg",
                AllowExtensionsFileManager.jpeg => ".jpeg",
                AllowExtensionsFileManager.webp => ".webp",
                AllowExtensionsFileManager.mkv => ".mkv",
                AllowExtensionsFileManager.mp4 => ".mp4",
                AllowExtensionsFileManager.mp3 => ".mp3",
                AllowExtensionsFileManager.rar => ".rar",
                AllowExtensionsFileManager.zip => ".zip",
                AllowExtensionsFileManager.gif => ".gif",
                _ => throw new ArgumentOutOfRangeException(nameof(ext), ext, null)
            };
        }

        public static string GetMimeType(AllowExtensionsFileManager extension)
        {
            return extension switch
            {
                AllowExtensionsFileManager.jpg => "image/jpeg",
                AllowExtensionsFileManager.jpeg => "image/jpeg",
                AllowExtensionsFileManager.gif => "image/gif",
                AllowExtensionsFileManager.png => "image/png",
                AllowExtensionsFileManager.webp => "image/webp",
                AllowExtensionsFileManager.txt => "text/plain",
                AllowExtensionsFileManager.docx => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                AllowExtensionsFileManager.pdf => "application/pdf",
                AllowExtensionsFileManager.zip => "application/zip",
                AllowExtensionsFileManager.rar => "application/x-rar-compressed",
                AllowExtensionsFileManager.mp3 => "audio/mpeg",
                AllowExtensionsFileManager.mp4 => "video/mp4",
                AllowExtensionsFileManager.mkv => "video/x-matroska",
                _ => "application/octet-stream"
            };
        }

        public static string GenerateFileName()
        {
            return $"{DateTime.Now.Year}{DateTime.Now.Month}{DateTime.Now.Day}{DateTime.Now.Hour}{DateTime.Now.Minute}{DateTime.Now.Second}{DateTime.Now.Millisecond}";
        }
    }
}
