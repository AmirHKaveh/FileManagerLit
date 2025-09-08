using FileManager.Helpers;
using FileManager.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Web;

namespace FileManager.Services
{
    public class FileManagerService : IFileManagerService
    {
        private readonly string _rootPath;
        private readonly IPathProvider _pathProvider;

        public FileManagerService(IPathProvider pathProvider, IOptions<FileManagerOptions> options)
        {
            _pathProvider = pathProvider;
            _rootPath = options.Value.RootPath;
        }

        public async Task<ApiResponse> CopyDirectoriesOrFilesAsync(List<string> sourcePaths, string destinationPath)
        {
            if (sourcePaths == null || sourcePaths.Count == 0)
            {
                return new ApiResponse(400, "لیست مسیرهای منبع نمی تواند خالی باشد !");
            }

            if (string.IsNullOrEmpty(destinationPath))
            {
                return new ApiResponse(400, "مسیر مقصد معتبر نمی باشد !");
            }

            var destinationDirectoryPath = Path.Combine(_pathProvider.WebRootPath, destinationPath);

            // Validate the destination directory
            if (!Directory.Exists(destinationDirectoryPath))
            {
                return new ApiResponse(400, "مسیر مقصد معتبر نمی باشد !");
            }

            foreach (var sourcePath in sourcePaths)
            {
                var sourceFullPath = Path.Combine(_pathProvider.WebRootPath, sourcePath);
                var fileName = Path.GetFileName(sourcePath);
                var destinationFilePath = Path.Combine(destinationDirectoryPath, fileName);

                if (!IsValidPathDirectoryOrFile(sourceFullPath))
                {
                    return new ApiResponse(400, $"مسیر منبع معتبر نمی باشد !");
                }

                var sourceInfo = new DirectoryInfo(sourceFullPath);
                if (IsDirectory(sourceFullPath))
                {
                    var destinationInfo = new DirectoryInfo(destinationFilePath);
                    CopyAll(sourceInfo, destinationInfo); // Recursively copy the directory
                }
                else
                {
                    System.IO.File.Copy(sourceFullPath, destinationFilePath, overwrite: true); // Copy the file
                }
            }

            return new ApiOkResponse("ok");
        }

        public async Task<ApiResponse> CreateNewDirectoryAsync(string? currentPath, string directoryName)
        {
            string folderNamePattern = @"^([\w-]+\.?)*[\w-]+$";

            if (string.IsNullOrEmpty(directoryName))
            {
                return new ApiResponse(400, "لطفا نام فولدر را وارد نمایید !");
            }

            if (!Regex.IsMatch(directoryName, folderNamePattern))
            {
                return new ApiResponse(400, "لطفا نام فولدر را بصورت صحیح وارد نمایید !");
            }

            var (nameWithoutExt, extension) = GetFileNameParts(directoryName);
            var basePath = GetBasePath(currentPath ?? "");
            var uniqueDirectoryName = GetUniqueDirectoryName(basePath, nameWithoutExt, extension);
            var finalPath = Path.Combine(basePath, uniqueDirectoryName);

            Directory.CreateDirectory(finalPath);
            return new ApiOkResponse("ok");
        }

        public async Task<ApiResponse> DeleteDirectoriesOrFilesAsync(List<string> paths)
        {
            foreach (var currentPath in paths)
            {
                var fullPath = Path.Combine(_pathProvider.WebRootPath, currentPath);
                if (string.IsNullOrEmpty(currentPath))
                {
                    return new ApiResponse(400, "مسیر فایل معتبر نمی باشد");
                }
                var path = currentPath.GetSubstringToLastSlash();
                if (!System.IO.File.Exists(fullPath) && !System.IO.Directory.Exists(fullPath))
                {
                    return new ApiResponse(400, "مسیر فایل معتبر نمی باشد");
                }
                FileAttributes attr = System.IO.File.GetAttributes(fullPath);


                if (attr.HasFlag(FileAttributes.Directory))
                {
                    Directory.Delete(fullPath, true);
                }
                else
                {
                    System.IO.File.Delete(fullPath);
                }
            }
            return new ApiOkResponse("ok");
        }

        public async Task<(ApiResponse, FileStreamResult)> DownloadAsync(string path)
        {
            var filePath = Path.Combine(_pathProvider.WebRootPath, path);
            filePath = HttpUtility.UrlDecode(filePath);
            if (!System.IO.File.Exists(filePath))
            {
                return (new ApiResponse(404, "فایلی یافت نشد!"), null);
            }
            var extension = Path.GetExtension(filePath);
            var fileName = Path.GetFileName(filePath);

            Enum.TryParse(extension, out AllowExtensionsFileManager allowExtension);
            var contentType = GetMimeType(allowExtension);

            var stream = new FileStream(filePath, FileMode.Open);

            return (new ApiOkResponse("ok"), new FileStreamResult(stream, contentType));
        }

        public async Task<ApiResponse> GetDirectoriesAsync(string? currentPath)
        {
            var filePath = Path.Combine(_pathProvider.WebRootPath, _rootPath);
            if (!string.IsNullOrEmpty(currentPath))
            {
                filePath = Path.Combine(_pathProvider.WebRootPath, currentPath);
            }

            DirectoryInfo objDirectoryInfo = new DirectoryInfo(filePath);
            if (!Path.Exists(filePath))
            {
                return new ApiResponse(404, "مسیری یافت نشد !");
            }
            if (!objDirectoryInfo.Exists)
            {
                return new ApiResponse(404, "فولدری یافت نشد !");
            }
            DirectoryInfo[] directories = objDirectoryInfo.GetDirectories();
            var files = objDirectoryInfo.GetFiles();

            var result = directories.Select(x => new DirectoryResponseDto()
            {
                DateModified = x.LastWriteTime,
                IsDirectory = (x.Attributes == FileAttributes.Directory ? true : false),
                HasSubDirectories = (Directory.EnumerateDirectories(x.FullName).Any() ? true : false),
                Name = x.Name,
                Path = GetPath(x.FullName),
                Size = 0,
            }).ToList();

            result.AddRange(files.Select(x => new DirectoryResponseDto()
            {
                DateModified = x.LastWriteTime,
                IsDirectory = (x.Attributes == FileAttributes.Directory ? true : false),
                HasSubDirectories = false,
                Name = x.Name,
                Path = GetPath(x.FullName),
                Size = x.Length,
            }));

            return new ApiOkResponse(result);
        }

        public async Task<ApiResponse> MoveDirectoriesOrFilesAsync(List<string> sourcePaths, string destinationPath)
        {
            if (sourcePaths == null || sourcePaths.Count == 0)
            {
                return new ApiResponse(400, "هیچ مسیر منبعی ارائه نشده است!");
            }

            if (string.IsNullOrWhiteSpace(destinationPath))
            {
                return new ApiResponse(400, "مسیر مقصد معتبر نمی باشد!");
            }

            var rootPath = _pathProvider.WebRootPath;
            var destinationRoot = Path.Combine(rootPath, destinationPath);

            if (!Directory.Exists(destinationRoot))
            {
                return new ApiResponse(400, "مسیر مقصد وجود ندارد!");
            }

            foreach (var relativeSourcePath in sourcePaths)
            {
                if (string.IsNullOrWhiteSpace(relativeSourcePath)) continue;

                var sourceFullPath = Path.Combine(rootPath, relativeSourcePath);

                if (!System.IO.File.Exists(sourceFullPath) && !Directory.Exists(sourceFullPath))
                {
                    return new ApiResponse(400, $"مسیر منبع معتبر نمی‌باشد!");
                }

                var itemName = Path.GetFileName(sourceFullPath);
                var targetPath = Path.Combine(destinationRoot, itemName);

                try
                {
                    MoveAll(sourceFullPath, targetPath);
                }
                catch (Exception ex)
                {
                    return new ApiResponse(500, $"خطا در انتقال '{relativeSourcePath}': {ex.Message}");
                }
            }

            return new ApiOkResponse("ok");
        }

        public async Task<ApiResponse> RenameDirectoryOrFileAsync(string sourcePath, string newName)
        {
            if (string.IsNullOrEmpty(newName))
            {
                return new ApiResponse(400, "لطفا نام جدید را وارد نمایید !");
            }

            string pattern = @"^(\w+\.?)*\w+$";

            if (!Regex.IsMatch(newName, pattern))
            {
                return new ApiResponse(400, "لطفا نام جدید را بصورت صحیح وارد نمایید !");
            }

            if (string.IsNullOrEmpty(sourcePath))
            {
                return new ApiResponse(400, "مسیر منبع معتبر نمی باشد !");
            }

            var sourceFullPath = Path.Combine(_pathProvider.WebRootPath, sourcePath);
            var path = sourcePath.GetSubstringToLastSlash();
            var newPath = Path.Combine(path, newName);

            var destinationPath = Path.Combine(_pathProvider.WebRootPath, newPath);


            FileAttributes attr = System.IO.File.GetAttributes(sourceFullPath);

            if (attr.HasFlag(FileAttributes.Directory))
            {
                if (!System.IO.Directory.Exists(sourceFullPath))
                {
                    return new ApiResponse(400, "مسیر منبع معتبر نمی باشد !");
                }


                Directory.Move(sourceFullPath, destinationPath);
            }
            else
            {
                if (!System.IO.File.Exists(sourceFullPath))
                {
                    return new ApiResponse(400, "مسیر منبع معتبر نمی باشد !");
                }

                System.IO.File.Move(sourceFullPath, destinationPath);
            }
            return new ApiOkResponse("ok");
        }

        public async Task<ApiResponse> UnzipAsync(string zipPath, string extractPath)
        {
            var extractFilePath = Path.Combine(_pathProvider.WebRootPath, extractPath);
            var zipFilePath = Path.Combine(_pathProvider.WebRootPath, zipPath);

            if (!System.IO.File.Exists(zipFilePath))
            {
                return new ApiResponse(400, "فایلی یافت نشد !");
            }

            if (!Directory.Exists(extractFilePath))
            {
                return new ApiResponse(400, "مسیر انتخابی پیدا نشد !");
            }

            ZipFile.ExtractToDirectory(zipFilePath, extractFilePath, true);

            return new ApiOkResponse("ok");
        }

        public async Task<ApiResponse> UploadAsync(UploadFileManagerRequestModel request)
        {
            if (!request.Files.Any())
            {
                return new ApiResponse(200, "ok");
            }

            var path = Path.Combine(_pathProvider.WebRootPath, _rootPath);
            if (!string.IsNullOrEmpty(request.CurrentPath.TrimEnd('/')))
            {
                path = Path.Combine(_pathProvider.WebRootPath, request.CurrentPath.TrimEnd('/'));
            }

            foreach (var file in request.Files)
            {
                if (file.Length > 0)
                {
                    var fileName = file.FileName;

                    var fileExtension = Path.GetExtension(fileName);
                    if (!Enum.IsDefined(typeof(AllowExtensionsFileManager), fileExtension.TrimStart('.')))
                    {
                        return new ApiResponse(400, "نوع فایل نامعتبر است");
                    }

                    var filePath = Path.Combine(path, fileName);
                    var (nameWithoutExt, extension) = GetFileNameParts(fileName);
                    var basePath = GetBasePath(path);
                    var uniqueDirectoryName = GetUniqueFileName(basePath, nameWithoutExt, extension);
                    filePath = Path.Combine(basePath, uniqueDirectoryName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                }
            }

            return new ApiOkResponse("ok");
        }

        public async Task<ApiResponse> ZipAsync(FileZipRequestModel request)
        {
            var destinationZipPath = Path.Combine(_pathProvider.WebRootPath, request.DirectoryPath, request.FileZipName);

            if (System.IO.File.Exists(destinationZipPath))
            {
                System.IO.File.Delete(destinationZipPath);
            }

            using (FileStream zipToOpen = new FileStream(destinationZipPath, FileMode.Create))
            using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create))
            {
                foreach (string path in request.FilePaths)
                {
                    var filePath = Path.Combine(_pathProvider.WebRootPath, path);
                    if (System.IO.File.Exists(filePath))
                    {
                        // It's a file
                        string entryName = Path.GetFileName(path); // Just the file name
                        archive.CreateEntryFromFile(filePath, entryName);
                    }
                    else if (Directory.Exists(filePath))
                    {
                        // It's a directory
                        AddDirectoryToZip(archive, filePath, Path.GetFileName(path));
                    }
                }
            }

            return new ApiOkResponse("ok");
        }

        [NonAction]
        private bool IsValidPathDirectoryOrFile(string path)
        {
            return System.IO.File.Exists(path) || Directory.Exists(path);
        }
        [NonAction]
        private bool IsDirectory(string path)
        {
            return (System.IO.File.GetAttributes(path) & FileAttributes.Directory) == FileAttributes.Directory;
        }

        [NonAction]
        public static void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            Directory.CreateDirectory(target.FullName);

            // Copy each file into the new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
                fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir =
                    target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir, nextTargetSubDir);
            }
        }
        [NonAction]
        private (string nameWithoutExt, string extension) GetFileNameParts(string fileName)
        {
            return (Path.GetFileNameWithoutExtension(fileName), Path.GetExtension(fileName));
        }

        [NonAction]
        private string GetBasePath(string currentPath)
        {
            return Path.Combine(_pathProvider.WebRootPath,
                               string.IsNullOrEmpty(currentPath) ? _rootPath : currentPath);
        }
        [NonAction]
        private string GetUniqueDirectoryName(string basePath, string nameWithoutExt, string extension)
        {
            var directoryName = $"{nameWithoutExt}{extension}";
            var index = 1;

            while (Directory.Exists(Path.Combine(basePath, directoryName)))
            {
                directoryName = $"{nameWithoutExt}{string.Format("-copy({0})", index)}{extension}";
                index++;
            }

            return directoryName;
        }

        [NonAction]
        private string GetUniqueFileName(string basePath, string nameWithoutExt, string extension)
        {
            var directoryName = $"{nameWithoutExt}{extension}";
            var index = 1;

            while (System.IO.File.Exists(Path.Combine(basePath, directoryName)))
            {
                directoryName = $"{nameWithoutExt}{string.Format("-copy({0})", index)}{extension}";
                index++;
            }

            return directoryName;
        }

        [NonAction]
        public string GetPath(string fullName)
        {
            var uri = new Uri(fullName);
            var absolutePath = uri.AbsolutePath.ToLower();
            var path = uri.AbsolutePath.Substring(absolutePath.IndexOf("files/"));

            return path;
        }

        [NonAction]
        public void MoveAll(string sourcePath, string destinationPath)
        {
            if (IsDirectory(sourcePath))
            {
                if (Directory.Exists(destinationPath))
                {
                    Directory.Delete(destinationPath, true);
                }
                Directory.Move(sourcePath, destinationPath);
            }
            else
            {
                System.IO.File.Move(sourcePath, destinationPath, overwrite: true);
            }
        }

        [NonAction]
        private void AddDirectoryToZip(ZipArchive archive, string sourceDir, string entryName)
        {
            foreach (string filePath in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                string relativePath = Path.GetRelativePath(sourceDir, filePath);
                string entryPath = Path.Combine(entryName, relativePath).Replace("\\", "/");
                archive.CreateEntryFromFile(filePath, entryPath);
            }
        }

        [NonAction]
        private string GetMimeType(AllowExtensionsFileManager extension)
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
    }
}
