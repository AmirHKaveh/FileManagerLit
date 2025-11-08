using FileManagerLite.Models;

using FileManagerLiteUsage.Models;

using Microsoft.AspNetCore.Mvc;

namespace FileManagerLite
{
    public interface IFileManagerService 
    {
        Task<DirectoryFileManagerResponseModel> GetDirectoriesAsync(string? currentPath, FileManagerSearchRequest searchRequest = null);
        Task<FileManagerResult> CreateNewDirectoryAsync(string? currentPath, string directoryName);
        Task<FileManagerResult> RenameDirectoryOrFileAsync(string sourcePath, string newName);
        Task<FileManagerResult> DeleteDirectoriesOrFilesAsync(List<string> paths,bool isPermanent=false);
        Task<FileManagerResult> CopyDirectoriesOrFilesAsync(List<string> sourcePaths, string destinationPath);
        Task<FileManagerResult> MoveDirectoriesOrFilesAsync(List<string> sourcePaths, string destinationPath);
        Task<UploadFilesManagerResponseModel> UploadFilesAsync(UploadFilesManagerRequestModel request);
        Task<UploadFileManagerResponseModel> UploadFileAsync(UploadFileManagerRequestModel request);
        Task<(FileManagerResult, FileStreamResult)> DownloadAsync(string path);
        Task<FileManagerResult> ZipAsync(FileZipRequestModel request);
        Task<FileManagerResult> UnzipAsync(string zipPath, string extractPath);

        Task<ResizeResponseModel> Resizer(ResizeRequestModel request);
    }
}
