using FileManagerLite.Models;

using FileManagerLiteUsage.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Web;

namespace FileManagerLite
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

        public async Task<FileManagerResult> CopyDirectoriesOrFilesAsync(List<string> sourcePaths, string destinationPath)
        {
            if (sourcePaths == null || sourcePaths.Count == 0)
            {
                return new FileManagerResult(400, "لیست مسیرهای منبع نمی تواند خالی باشد !");
            }

            if (string.IsNullOrEmpty(destinationPath))
            {
                return new FileManagerResult(400, "مسیر مقصد معتبر نمی باشد !");
            }

            var destinationDirectoryPath = Path.Combine(_pathProvider.WebRootPath, destinationPath);

            // Validate the destination directory
            if (!Directory.Exists(destinationDirectoryPath))
            {
                return new FileManagerResult(400, "مسیر مقصد معتبر نمی باشد !");
            }

            foreach (var sourcePath in sourcePaths)
            {
                var sourceFullPath = Path.Combine(_pathProvider.WebRootPath, sourcePath);
                var fileName = Path.GetFileName(sourcePath);
                var destinationFilePath = Path.Combine(destinationDirectoryPath, fileName);

                if (!IsValidPathDirectoryOrFile(sourceFullPath))
                {
                    return new FileManagerResult(400, $"مسیر منبع معتبر نمی باشد !");
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

            return new FileManagerResult(200, "عملیات با موفقیت انجام شد", true);
        }

        public async Task<FileManagerResult> CreateNewDirectoryAsync(string? currentPath, string directoryName)
        {
            string folderNamePattern = @"^([\w-]+\.?)*[\w-]+$";

            if (string.IsNullOrEmpty(directoryName))
            {
                return new FileManagerResult(400, "لطفا نام فولدر را وارد نمایید !");
            }

            if (!Regex.IsMatch(directoryName, folderNamePattern))
            {
                return new FileManagerResult(400, "لطفا نام فولدر را بصورت صحیح وارد نمایید !");
            }

            var (nameWithoutExt, extension) = GetFileNameParts(directoryName);
            var basePath = GetBasePath(currentPath ?? "");
            var uniqueDirectoryName = GetUniqueDirectoryName(basePath, nameWithoutExt, extension);
            var finalPath = Path.Combine(basePath, uniqueDirectoryName);

            Directory.CreateDirectory(finalPath);
            return new FileManagerResult(200, "عملیات با موفقیت انجام شد", true);
        }

        public async Task<FileManagerResult> DeleteDirectoriesOrFilesAsync(List<string> paths, bool isPermanent = false)
        {
            if (paths.Any(x => x.TrimEnd('/').ToLower() == _rootPath.ToLower()))
            {
                return new FileManagerResult(400, "مسیر فایل معتبر نمی باشد");
            }

            var trashPath = Path.Combine(_pathProvider.WebRootPath, "Trash");
            if (!Directory.Exists(trashPath))
            {
                Directory.CreateDirectory(trashPath);
            }

            if (isPermanent)
            {
                foreach (var currentPath in paths)
                {
                    var fullPath = Path.Combine(_pathProvider.WebRootPath, currentPath);
                    if (string.IsNullOrEmpty(currentPath))
                    {
                        return new FileManagerResult(400, "مسیر فایل معتبر نمی باشد");
                    }
                    if (!System.IO.File.Exists(fullPath) && !System.IO.Directory.Exists(fullPath))
                    {
                        return new FileManagerResult(400, "مسیر فایل معتبر نمی باشد");
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
            }
            else
            {
                foreach (var currentPath in paths)
                {
                    var fullPath = Path.Combine(_pathProvider.WebRootPath, currentPath);
                    if (string.IsNullOrEmpty(currentPath))
                    {
                        return new FileManagerResult(400, "مسیر فایل معتبر نمی باشد");
                    }
                    if (!System.IO.File.Exists(fullPath) && !System.IO.Directory.Exists(fullPath))
                    {
                        return new FileManagerResult(400, "مسیر فایل معتبر نمی باشد");
                    }
                    MoveToTrash(fullPath, trashPath);

                }
            }
            return new FileManagerResult(200, "عملیات با موفقیت انجام شد", true);
        }

        public async Task<(FileManagerResult, FileStreamResult)> DownloadAsync(string path)
        {
            var filePath = Path.Combine(_pathProvider.WebRootPath, path);
            filePath = HttpUtility.UrlDecode(filePath);
            if (!System.IO.File.Exists(filePath))
            {
                return (new FileManagerResult(400, "فایلی یافت نشد!"), null);
            }
            var extension = Path.GetExtension(filePath);
            var fileName = Path.GetFileName(filePath);

            Enum.TryParse(extension, out AllowExtensionsFileManager allowExtension);
            var contentType = GetMimeType(allowExtension);

            var stream = new FileStream(filePath, FileMode.Open);

            return (new FileManagerResult(200, "عملیات با موفقیت انجام شد", true), new FileStreamResult(stream, contentType));
        }

        public async Task<DirectoryFileManagerResponseModel> GetDirectoriesAsync(string? currentPath, FileManagerSearchRequest searchRequest = null)
        {
            var filePath = Path.Combine(_pathProvider.WebRootPath, _rootPath);
            if (!string.IsNullOrEmpty(currentPath))
            {
                filePath = Path.Combine(_pathProvider.WebRootPath, currentPath);
            }

            DirectoryInfo objDirectoryInfo = new DirectoryInfo(filePath);
            if (!Path.Exists(filePath))
            {
                return new DirectoryFileManagerResponseModel(400, "مسیری یافت نشد !");
            }
            if (!objDirectoryInfo.Exists)
            {
                return new DirectoryFileManagerResponseModel(400, "فولدری یافت نشد !");
            }

            var query = searchRequest.IsRecursive ?
                objDirectoryInfo.EnumerateFileSystemInfos("*", SearchOption.AllDirectories).AsQueryable() :
                objDirectoryInfo.EnumerateFileSystemInfos("*", SearchOption.TopDirectoryOnly).AsQueryable();

            if (searchRequest is not null)
            {
                if (!string.IsNullOrEmpty(searchRequest.Keyword))
                {
                    query = query.Where(x => x.Name.Contains(searchRequest.Keyword, StringComparison.OrdinalIgnoreCase)).AsQueryable();
                }
                if (searchRequest.Extensions is not null && searchRequest.Extensions.Count > 0)
                {
                    var allowed = searchRequest.Extensions.Select(e => e.ToExtension()).ToArray();

                    query = query.Where(x =>
                       allowed.Contains(x.Extension, StringComparer.OrdinalIgnoreCase)).AsQueryable();
                }
            }

            var allEntries = query
                .OrderByDescending(x => (x.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.LastWriteTime).ToList();

            var result = allEntries.Select(x => new DirectoryResponseDto()
            {
                DateModified = x.LastWriteTime,
                IsDirectory = (x.Attributes & FileAttributes.Directory) == FileAttributes.Directory,
                HasSubDirectories = (x.Attributes & FileAttributes.Directory) == FileAttributes.Directory
                                    && Directory.EnumerateDirectories(x.FullName).Any(),
                Name = x.Name,
                Path = GetPath(x.FullName),
                Size = (x is FileInfo fi ? fi.Length : 0),
                SubscriptionsCount = (x.Attributes & FileAttributes.Directory) == FileAttributes.Directory
                    ? Directory.EnumerateFiles(x.FullName).Count() + Directory.EnumerateDirectories(x.FullName).Count()
                    : 0
            }).ToList();


            return new DirectoryFileManagerResponseModel(200, "ok", true, result);
        }

        public async Task<FileManagerResult> MoveDirectoriesOrFilesAsync(List<string> sourcePaths, string destinationPath)
        {
            if (sourcePaths == null || sourcePaths.Count == 0)
            {
                return new FileManagerResult(400, "هیچ مسیر منبعی ارائه نشده است!");
            }

            if (string.IsNullOrWhiteSpace(destinationPath))
            {
                return new FileManagerResult(400, "مسیر مقصد معتبر نمی باشد!");
            }

            var rootPath = _pathProvider.WebRootPath;
            var destinationRoot = Path.Combine(rootPath, destinationPath);

            if (!Directory.Exists(destinationRoot))
            {
                return new FileManagerResult(400, "مسیر مقصد وجود ندارد!");
            }

            foreach (var relativeSourcePath in sourcePaths)
            {
                if (string.IsNullOrWhiteSpace(relativeSourcePath)) continue;

                var sourceFullPath = Path.Combine(rootPath, relativeSourcePath);

                if (!System.IO.File.Exists(sourceFullPath) && !Directory.Exists(sourceFullPath))
                {
                    return new FileManagerResult(400, $"مسیر منبع معتبر نمی‌باشد!");
                }

                var itemName = Path.GetFileName(sourceFullPath);
                var targetPath = Path.Combine(destinationRoot, itemName);

                try
                {
                    MoveAll(sourceFullPath, targetPath);
                }
                catch (Exception ex)
                {
                    return new FileManagerResult(500, $"خطا در انتقال '{relativeSourcePath}': {ex.Message}");
                }
            }

            return new FileManagerResult(200, "عملیات با موفقیت انجام شد", true);
        }

        public async Task<FileManagerResult> RenameDirectoryOrFileAsync(string sourcePath, string newName)
        {
            if (string.IsNullOrEmpty(newName))
            {
                return new FileManagerResult(400, "لطفا نام جدید را وارد نمایید !");
            }

            string pattern = @"^(\w+\.?)*\w+$";

            if (!Regex.IsMatch(newName, pattern))
            {
                return new FileManagerResult(400, "لطفا نام جدید را بصورت صحیح وارد نمایید !");
            }

            if (string.IsNullOrEmpty(sourcePath))
            {
                return new FileManagerResult(400, "مسیر منبع معتبر نمی باشد !");
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
                    return new FileManagerResult(400, "مسیر منبع معتبر نمی باشد !");
                }


                Directory.Move(sourceFullPath, destinationPath);
            }
            else
            {
                if (!System.IO.File.Exists(sourceFullPath))
                {
                    return new FileManagerResult(400, "مسیر منبع معتبر نمی باشد !");
                }

                System.IO.File.Move(sourceFullPath, destinationPath);
            }
            return new FileManagerResult(200, "عملیات با موفقیت انجام شد", true);
        }

        public async Task<FileManagerResult> UnzipAsync(string zipPath, string extractPath)
        {
            var extractFilePath = Path.Combine(_pathProvider.WebRootPath, extractPath);
            var zipFilePath = Path.Combine(_pathProvider.WebRootPath, zipPath);

            if (!System.IO.File.Exists(zipFilePath))
            {
                return new FileManagerResult(400, "فایلی یافت نشد !");
            }

            if (!Directory.Exists(extractFilePath))
            {
                return new FileManagerResult(400, "مسیر انتخابی پیدا نشد !");
            }

            ZipFile.ExtractToDirectory(zipFilePath, extractFilePath, true);

            return new FileManagerResult(200, "عملیات با موفقیت انجام شد", true);
        }

        public async Task<UploadFilesManagerResponseModel> UploadFilesAsync(UploadFilesManagerRequestModel request)
        {
            if (!request.Files.Any())
            {
                return new UploadFilesManagerResponseModel(400, "فایلی انتخاب نشده است !");
            }

            var path = Path.Combine(_pathProvider.WebRootPath, _rootPath);
            if (!string.IsNullOrEmpty(request.CurrentPath.TrimEnd('/')))
            {
                path = Path.Combine(_pathProvider.WebRootPath, request.CurrentPath.TrimEnd('/'));
            }
            var fileAddresses = new List<string>();
            foreach (var file in request.Files)
            {
                if (file.Length > 0)
                {
                    var fileName = file.FileName;

                    var fileExtension = Path.GetExtension(fileName);
                    if (!Enum.IsDefined(typeof(AllowExtensionsFileManager), fileExtension.TrimStart('.').ToLower()))
                    {
                        return new UploadFilesManagerResponseModel(400, "نوع فایل نامعتبر است");
                    }

                    var filePath = Path.Combine(path, fileName);
                    var (nameWithoutExt, extension) = GetFileNameParts(fileName);
                    var basePath = GetBasePath(path);
                    var newFileName = request.IsRandomFileName ? Extensions.GenerateFileName() : nameWithoutExt;
                    var uniqueDirectoryName = GetUniqueFileName(basePath, newFileName, extension);
                    filePath = Path.Combine(basePath, uniqueDirectoryName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                    fileAddresses.Add(string.Format("{0}/{1}{2}", request.CurrentPath, newFileName, extension));
                }
            }

            return new UploadFilesManagerResponseModel(statusCode: 200, message: "عملیات با موفقیت انجام شد", isSucceed: true, filePaths: fileAddresses);
        }

        public async Task<UploadFileManagerResponseModel> UploadFileAsync(UploadFileManagerRequestModel request)
        {
            if (request.File is null)
            {
                return new UploadFileManagerResponseModel(400, "فایلی انتخاب نشده است !");
            }

            var path = Path.Combine(_pathProvider.WebRootPath, _rootPath);
            if (!string.IsNullOrEmpty(request.CurrentPath.TrimEnd('/')))
            {
                path = Path.Combine(_pathProvider.WebRootPath, request.CurrentPath.TrimEnd('/'));
            }

            var fileAddress = "";
            if (request.File.Length > 0)
            {
                var fileName = request.File.FileName;

                var fileExtension = Path.GetExtension(fileName);
                if (!Enum.IsDefined(typeof(AllowExtensionsFileManager), fileExtension.TrimStart('.').ToLower()))
                {
                    return new UploadFileManagerResponseModel(400, "نوع فایل نامعتبر است");
                }

                var filePath = Path.Combine(path, fileName);
                var (nameWithoutExt, extension) = GetFileNameParts(fileName);
                var basePath = GetBasePath(path);
                var newFileName = request.IsRandomFileName ? Extensions.GenerateFileName() : nameWithoutExt;
                var uniqueDirectoryName = GetUniqueFileName(basePath, newFileName, extension);
                filePath = Path.Combine(basePath, uniqueDirectoryName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await request.File.CopyToAsync(stream);
                }
                fileAddress = string.Format("{0}/{1}{2}", request.CurrentPath, newFileName, extension);
            }

            return new UploadFileManagerResponseModel(statusCode: 200, message: "عملیات با موفقیت انجام شد", isSucceed: true, filePath: fileAddress);
        }
        public async Task<FileManagerResult> ZipAsync(FileZipRequestModel request)
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

            return new FileManagerResult(200, "عملیات با موفقیت انجام شد", true);
        }

        public async Task<ResizeResponseModel> Resizer(ResizeRequestModel request)
        {
            var resizeParams = new ResizeParams()
            {
                w = request.Width,
                h = request.Height,
                mode = request?.Mode,
            };

            var filePath = Path.Combine(_pathProvider.WebRootPath, request.ImagePath);
            if (!File.Exists(filePath))
            {
                return new ResizeResponseModel(400, "فایلی یافت نشد !");

            }
            byte[] bytes = null;
            using (var outputStream = new MemoryStream())
            {
                if (string.Equals(resizeParams.mode, "crop", StringComparison.OrdinalIgnoreCase))
                {
                    bytes = FileManipulation.CropImage
                        (
                            filePath,
                            resizeParams.w,
                            resizeParams.h
                        );
                }
                else if (string.Equals(resizeParams.mode, "stretch", StringComparison.OrdinalIgnoreCase))
                {
                    bytes = FileManipulation.StretchImage
                        (
                            filePath,
                            resizeParams.w,
                            resizeParams.h
                        );
                }
                else
                {
                    bytes = File.ReadAllBytes(filePath);
                }
                var extension = Path.GetExtension(filePath);
                var fileName = Path.GetFileName(filePath);

                Enum.TryParse(extension, out AllowExtensionsFileManager allowExtension);
                var contentType = Extensions.GetMimeType(allowExtension);

                var fileResponse = new ResizeFileResponseModel() { ContentData = bytes, ContentType = contentType, FileName = fileName };

                return (new ResizeResponseModel(200, "ok", fileResponse));
            }
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
            var path = uri.AbsolutePath.Substring(absolutePath.IndexOf($"{_rootPath.ToLower()}/"));

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
        public void MoveToTrash(string sourcePath, string trashPath)
        {
            if (IsDirectory(sourcePath))
            {
                // Move the directory itself into Trash
                var dirName = Path.GetFileName(sourcePath);
                var destination = Path.Combine(trashPath, dirName);

                // Ensure destination does not exist (otherwise Directory.Move fails)
                if (Directory.Exists(destination))
                {
                    Directory.Delete(destination, true);
                }

                Directory.Move(sourcePath, destination);
            }
            else
            {
                // Move file into Trash
                var fileName = Path.GetFileName(sourcePath);
                var destination = Path.Combine(trashPath, fileName);

                if (File.Exists(destination))
                {
                    File.Delete(destination);
                }

                File.Move(sourcePath, destination);
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
