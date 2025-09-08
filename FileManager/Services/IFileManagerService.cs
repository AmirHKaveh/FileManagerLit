using FileManager.Helpers;
using FileManager.Models;

using Microsoft.AspNetCore.Mvc;

namespace FileManager.Services
{
    public interface IFileManagerService
    {
        Task<ApiResponse> GetDirectoriesAsync(string? currentPath);
        Task<ApiResponse> CreateNewDirectoryAsync(string? currentPath, string directoryName);
        Task<ApiResponse> RenameDirectoryOrFileAsync(string sourcePath, string newName);
        Task<ApiResponse> DeleteDirectoriesOrFilesAsync(List<string> paths);
        Task<ApiResponse> CopyDirectoriesOrFilesAsync(List<string> sourcePaths, string destinationPath);
        Task<ApiResponse> MoveDirectoriesOrFilesAsync(List<string> sourcePaths, string destinationPath);
        Task<ApiResponse> UploadAsync(UploadFileManagerRequestModel request);
        Task<(ApiResponse, FileStreamResult)> DownloadAsync(string path);
        Task<ApiResponse> ZipAsync(FileZipRequestModel request);
        Task<ApiResponse> UnzipAsync(string zipPath, string extractPath);
    }
}
