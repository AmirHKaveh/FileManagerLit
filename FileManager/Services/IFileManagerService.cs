using FileManagerLite.Models;

using Microsoft.AspNetCore.Mvc;

namespace FileManagerLite
{
    public interface IFileManagerService
    {
        Task<FileManagerResult> GetDirectoriesAsync(string? currentPath);
        Task<FileManagerResult> CreateNewDirectoryAsync(string? currentPath, string directoryName);
        Task<FileManagerResult> RenameDirectoryOrFileAsync(string sourcePath, string newName);
        Task<FileManagerResult> DeleteDirectoriesOrFilesAsync(List<string> paths);
        Task<FileManagerResult> CopyDirectoriesOrFilesAsync(List<string> sourcePaths, string destinationPath);
        Task<FileManagerResult> MoveDirectoriesOrFilesAsync(List<string> sourcePaths, string destinationPath);
        Task<FileManagerResult> UploadAsync(UploadFileManagerRequestModel request);
        Task<(FileManagerResult, FileStreamResult)> DownloadAsync(string path);
        Task<FileManagerResult> ZipAsync(FileZipRequestModel request);
        Task<FileManagerResult> UnzipAsync(string zipPath, string extractPath);
    }
}
