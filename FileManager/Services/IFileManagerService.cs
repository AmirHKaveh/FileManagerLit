using FileManagerLite.Models;

using FileManagerLiteUsage.Models;

using Microsoft.AspNetCore.Mvc;

namespace FileManagerLite
{
    public interface IFileManagerService 
    {
        DirectoryFileManagerResponseModel GetDirectories(string? currentPath, FileManagerSearchRequest searchRequest = null);
        FileManagerResult CreateNewDirectory(string? currentPath, string directoryName);
        FileManagerResult RenameDirectoryOrFile(string sourcePath, string newName);
        FileManagerResult DeleteDirectoriesOrFiles(List<string> paths, bool isPermanent = false);
        FileManagerResult CopyDirectoriesOrFiles(List<string> sourcePaths, string destinationPath);
        FileManagerResult MoveDirectoriesOrFiles(List<string> sourcePaths, string destinationPath);
        Task<UploadFilesManagerResponseModel> UploadFiles(UploadFilesManagerRequestModel request);
        Task<UploadFileManagerResponseModel> UploadFile(UploadFileManagerRequestModel request);
        (FileManagerResult, FileStreamResult) Download(string path);
        FileManagerResult Zip(FileZipRequestModel request);
        FileManagerResult Unzip(string zipPath, string extractPath);
        ResizeResponseModel Resizer(ResizeRequestModel request);
    }
}
