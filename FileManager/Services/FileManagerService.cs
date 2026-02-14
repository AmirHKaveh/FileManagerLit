using FileManagerLite.Models;

using FileManagerLiteUsage.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Web;

namespace FileManagerLite
{
    public class FileManagerService : IFileManagerService
    {
        private readonly string _rootPath;
        private readonly string _trashRootPath = "trash";
        private readonly IPathProvider _pathProvider;

        public FileManagerService(IPathProvider pathProvider, IOptions<FileManagerOptions> options)
        {
            _pathProvider = pathProvider;
            _rootPath = options.Value.RootPath;
        }

        public FileManagerResult CopyDirectoriesOrFiles(List<string> sourcePaths, string destinationPath)
        {
            if (sourcePaths == null || sourcePaths.Count == 0)
            {
                return new FileManagerResult(400, "لیست مسیرهای منبع نمی تواند خالی باشد !");
            }

            if (string.IsNullOrEmpty(destinationPath))
            {
                return new FileManagerResult(400, "مسیر مقصد معتبر نمی باشد !");
            }

            var isValidSourcePath = CheckRootPath(sourcePaths);
            if (!isValidSourcePath.Item1)
            {
                return new FileManagerResult(403, "مسیر درخواستی مبدا مجاز نمی باشد");
            }

            var isValidPath = CheckRootPath(destinationPath);
            if (!isValidPath.Item1)
            {
                return new FileManagerResult(403, "مسیر درخواستی مقصد مجاز نمی باشد");
            }

            var destinationDirectoryPath = Path.Combine(_pathProvider.WebRootPath, destinationPath);

            if (!Directory.Exists(destinationDirectoryPath))
            {
                return new FileManagerResult(400, "مسیر مقصد معتبر نمی باشد !");
            }

            try
            {
                foreach (var sourcePath in sourcePaths)
                {
                    var sourceFullPath = Path.GetFullPath(
                        Path.Combine(_pathProvider.WebRootPath, sourcePath));

                    if (!sourceFullPath.StartsWith(_pathProvider.WebRootPath))
                        return new FileManagerResult(403, "مسیر منبع غیرمجاز است");

                    if (!Directory.Exists(sourceFullPath) && !File.Exists(sourceFullPath))
                        return new FileManagerResult(400, "مسیر منبع معتبر نمی باشد");

                    var fileName = Path.GetFileName(sourceFullPath);
                    var destinationFilePath = Path.Combine(destinationDirectoryPath, fileName);

                    if (Directory.Exists(sourceFullPath))
                    {
                        CopyAll(new DirectoryInfo(sourceFullPath),
                                new DirectoryInfo(destinationFilePath));
                    }
                    else
                    {
                        File.Copy(sourceFullPath, destinationFilePath, true);
                    }
                }

                return new FileManagerResult(200, "عملیات با موفقیت انجام شد", true);
            }
            catch (Exception ex)
            {
                // log
                return new FileManagerResult(500, "خطا در انجام عملیات");
            }

        }

        public FileManagerResult CreateNewDirectory(string? currentPath, string directoryName)
        {
            string folderNamePattern = @"^[^\\/:*?""<>|]+$";

            if (!string.IsNullOrEmpty(currentPath))
            {
                var isValidPath = CheckRootPath(currentPath);
                if (!isValidPath.Item1)
                {
                    return new FileManagerResult(403, "مسیر درخواستی مجاز نمی باشد");
                }
            }
            if (string.IsNullOrEmpty(directoryName))
            {
                return new FileManagerResult(400, "لطفا نام فولدر را وارد نمایید !");
            }

            if (!Regex.IsMatch(directoryName, folderNamePattern))
            {
                return new FileManagerResult(400, "لطفا نام فولدر را بصورت صحیح وارد نمایید !");
            }

            var basePath = GetBasePath(currentPath ?? "");
            var uniqueDirectoryName = GetUniqueDirectoryName(basePath, directoryName);
            var finalPath = Path.Combine(basePath, uniqueDirectoryName);

            Directory.CreateDirectory(finalPath);
            return new FileManagerResult(200, "عملیات با موفقیت انجام شد", true);
        }

        public FileManagerResult DeleteDirectoriesOrFiles(List<string> paths, bool isPermanent = false)
        {
            if (paths == null || !paths.Any())
                return new FileManagerResult(400, "لیست مسیرها خالی است");


            if (paths.Any(x => x.TrimEnd('/').ToLower() == _rootPath.ToLower()))
            {
                return new FileManagerResult(400, "مسیر فایل معتبر نمی باشد");
            }

            var isValidSourcePath = CheckRootPath(paths);
            if (!isValidSourcePath.Item1)
            {
                return new FileManagerResult(403, "مسیرهای درخواستی مجاز نمی باشد");
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

        public (FileManagerResult, FileStreamResult) Download(string path)
        {
            var filePath = Path.Combine(_pathProvider.WebRootPath, path);
            filePath = HttpUtility.UrlDecode(filePath);
            if (!System.IO.File.Exists(filePath))
            {
                return (new FileManagerResult(400, "فایلی یافت نشد!"), null);
            }
            var extension = Path.GetExtension(filePath);

            Enum.TryParse(extension, out AllowExtensionsFileManager allowExtension);
            var contentType = GetMimeType(allowExtension);

            try
            {
                var stream = new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read);

                return (new FileManagerResult(200, "عملیات با موفقیت انجام شد", true), new FileStreamResult(stream, contentType));
            }
            catch
            {
                return (new FileManagerResult(500, "خطا در خواندن فایل"), null);
            }
        }

        public DirectoryFileManagerResponseModel GetDirectories(string? currentPath, FileManagerSearchRequest? searchRequest = null)
        {
            var filePath = Path.Combine(_pathProvider.WebRootPath, _rootPath);
            if (!string.IsNullOrEmpty(currentPath))
            {
                var isValidPath = CheckRootPath(currentPath);
                if (!isValidPath.Item1)
                {
                    return new DirectoryFileManagerResponseModel(403, "مسیر درخواستی مجاز نمی باشد");
                }

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

            var result = GenerateDirectoriesAndFiles(objDirectoryInfo, searchRequest);


            return new DirectoryFileManagerResponseModel(200, "ok", true, result);
        }

        public FileManagerResult MoveDirectoriesOrFiles(List<string> sourcePaths, string destinationPath)
        {
            if (sourcePaths == null || sourcePaths.Count == 0)
            {
                return new FileManagerResult(400, "هیچ مسیر منبعی ارائه نشده است!");
            }

            if (string.IsNullOrWhiteSpace(destinationPath))
            {
                return new FileManagerResult(400, "مسیر مقصد معتبر نمی باشد!");
            }

            var isValidSourcePath = CheckRootPath(sourcePaths);
            if (!isValidSourcePath.Item1)
            {
                return new FileManagerResult(403, "مسیر درخواستی مبدا مجاز نمی باشد");
            }

            var isValidPath = CheckRootPath(destinationPath);
            if (!isValidPath.Item1)
            {
                return new FileManagerResult(403, "مسیر درخواستی مقصد مجاز نمی باشد");
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

                if (sourceFullPath == targetPath)
                    return new FileManagerResult(400, "مبدا و مقصد یکسان هستند");


                if (targetPath.StartsWith(sourceFullPath))
                {
                    return new FileManagerResult(400, "امکان انتقال پوشه داخل خودش وجود ندارد");
                }


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

        public FileManagerResult RenameDirectoryOrFile(string sourcePath, string newName)
        {
            if (string.IsNullOrEmpty(newName))
            {
                return new FileManagerResult(400, "لطفا نام جدید را وارد نمایید !");
            }

            string pattern = @"^[^\\/:*?""<>|]+$";

            if (!Regex.IsMatch(newName, pattern))
            {
                return new FileManagerResult(400, "لطفا نام جدید را بصورت صحیح وارد نمایید !");
            }

            if (string.IsNullOrEmpty(sourcePath))
            {
                return new FileManagerResult(400, "مسیر منبع معتبر نمی باشد !");
            }

            var isValidPath = CheckRootPath(sourcePath);
            if (!isValidPath.Item1)
            {
                return new FileManagerResult(403, "مسیر درخواستی مجاز نمی باشد");
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

        public FileManagerResult Unzip(string zipPath, string extractPath)
        {
            var root = _pathProvider.WebRootPath;
            var extractFilePath = Path.GetFullPath(Path.Combine(root, extractPath));
            var zipFilePath = Path.GetFullPath(Path.Combine(root, zipPath));

            if (!zipFilePath.StartsWith(root) || !extractFilePath.StartsWith(root))
                return new FileManagerResult(403, "مسیر غیرمجاز است");

            if (!File.Exists(zipFilePath))
                return new FileManagerResult(400, "فایلی یافت نشد!");

            if (!Directory.Exists(extractFilePath))
                return new FileManagerResult(400, "مسیر انتخابی پیدا نشد!");

            var isValidZipPath = CheckRootPath(zipPath);
            var isValidExtractPath = CheckRootPath(extractPath);
            if (!isValidZipPath.Item1 || !isValidExtractPath.Item1)
                return new FileManagerResult(403, "مسیر درخواستی مجاز نمی باشد");

            const long MaxTotalUncompressedSize = 500 * 1024 * 1024; // 500 MB
            const int MaxFiles = 1000;

            long totalSize = 0;
            int fileCount = 0;

            try
            {
                using (var archive = ZipFile.OpenRead(zipFilePath))
                {
                    foreach (var entry in archive.Entries)
                    {
                        fileCount++;
                        totalSize += entry.Length;

                        if (fileCount > MaxFiles)
                            return new FileManagerResult(400, "تعداد فایل‌های داخل zip بیش از حد مجاز است");

                        if (totalSize > MaxTotalUncompressedSize)
                            return new FileManagerResult(400, "حجم کل فایل‌های داخل zip بیش از حد مجاز است");

                        var destinationPath = Path.GetFullPath(Path.Combine(extractFilePath, entry.FullName));
                        if (!destinationPath.StartsWith(extractFilePath))
                            return new FileManagerResult(403, "فایل zip شامل مسیر غیرمجاز است");

                        if (string.IsNullOrEmpty(entry.Name))
                            continue;

                        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                        entry.ExtractToFile(destinationPath, true);
                    }
                }
            }
            catch (InvalidDataException)
            {
                return new FileManagerResult(400, "فایل zip نامعتبر است");
            }
            catch (Exception ex)
            {
                return new FileManagerResult(500, $"خطا در استخراج فایل zip: {ex.Message}");
            }

            return new FileManagerResult(200, "عملیات با موفقیت انجام شد", true);
        }

        public FileManagerResult Zip(FileZipRequestModel request)
        {
            var root = _pathProvider.WebRootPath;
            var destinationZipPath = Path.GetFullPath(
                Path.Combine(root, request.DirectoryPath, request.FileZipName));

            if (!destinationZipPath.StartsWith(root))
                return new FileManagerResult(403, "مسیر غیرمجاز است");

            if (File.Exists(destinationZipPath))
                File.Delete(destinationZipPath);

            var isValidPath = CheckRootPath(request.DirectoryPath);
            if (!isValidPath.Item1)
                return new FileManagerResult(403, "مسیر درخواستی مجاز نمی باشد");

            try
            {
                using (FileStream zipToOpen = new FileStream(destinationZipPath, FileMode.Create))
                using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create))
                {
                    foreach (string path in request.FilePaths)
                    {
                        var filePath = Path.GetFullPath(Path.Combine(root, path));

                        if (!filePath.StartsWith(root))
                            continue; // skip unsafe paths

                        if (File.Exists(filePath))
                        {
                            string entryName = Path.GetFileName(path);
                            archive.CreateEntryFromFile(filePath, entryName);
                        }
                        else if (Directory.Exists(filePath))
                        {
                            AddDirectoryToZip(archive, filePath, Path.GetFileName(path));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new FileManagerResult(500, $"خطا در ساخت فایل zip: {ex.Message}");
            }

            return new FileManagerResult(200, "عملیات با موفقیت انجام شد", true);
        }


        public async Task<UploadFilesManagerResponseModel> UploadFiles(UploadFilesManagerRequestModel request)
        {
            if (!request.Files.Any())
                return new UploadFilesManagerResponseModel(400, "فایلی انتخاب نشده است !");

            var root = _pathProvider.WebRootPath;
            var currentPath = request.CurrentPath?.TrimEnd('/') ?? _rootPath;
            var fullPath = Path.GetFullPath(Path.Combine(root, currentPath));

            if (!fullPath.StartsWith(root))
                return new UploadFilesManagerResponseModel(403, "مسیر غیرمجاز است");

            if (!Directory.Exists(fullPath))
                Directory.CreateDirectory(fullPath);

            var fileAddresses = new List<string>();
            const long MaxFileSize = 50 * 1024 * 1024;

            foreach (var file in request.Files)
            {
                if (file.Length == 0) continue;
                if (file.Length > MaxFileSize)
                    return new UploadFilesManagerResponseModel(400, "حجم فایل بیش از حد مجاز است");

                var fileName = Path.GetFileName(file.FileName);
                if (!Regex.IsMatch(fileName, @"^[^\\/:*?""<>|]+$"))
                    return new UploadFilesManagerResponseModel(400, "نام فایل نامعتبر است");

                var fileExtension = Path.GetExtension(fileName).TrimStart('.');
                if (!Enum.TryParse<AllowExtensionsFileManager>(fileExtension, true, out var allowExt))
                    return new UploadFilesManagerResponseModel(400, "نوع فایل نامعتبر است");

                var newFileName = request.IsRandomFileName ? Extensions.GenerateFileName() : Path.GetFileNameWithoutExtension(fileName);
                var uniqueFileName = GetUniqueFileName(fullPath, newFileName, fileExtension);
                var filePath = Path.Combine(fullPath, uniqueFileName);

                try
                {
                    using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
                    await file.CopyToAsync(stream);
                }
                catch (Exception ex)
                {
                    return new UploadFilesManagerResponseModel(500, $"خطا در آپلود فایل: {ex.Message}");
                }

                var relativePath = Path.GetRelativePath(root, filePath).Replace("\\", "/");
                fileAddresses.Add(relativePath);
            }

            return new UploadFilesManagerResponseModel(200, "عملیات با موفقیت انجام شد", true, fileAddresses);
        }

        public async Task<UploadFileManagerResponseModel> UploadFile(UploadFileManagerRequestModel request)
        {
            if (request.File is null)
                return new UploadFileManagerResponseModel(400, "فایلی انتخاب نشده است !");

            var root = _pathProvider.WebRootPath;
            var currentPath = request.CurrentPath?.TrimEnd('/') ?? _rootPath;
            var fullPath = Path.GetFullPath(Path.Combine(root, currentPath));

            if (!fullPath.StartsWith(root))
                return new UploadFileManagerResponseModel(403, "مسیر غیرمجاز است");

            if (!Directory.Exists(fullPath))
                Directory.CreateDirectory(fullPath);

            var fileName = Path.GetFileName(request.File.FileName);
            if (!Regex.IsMatch(fileName, @"^[^\\/:*?""<>|]+$"))
                return new UploadFileManagerResponseModel(400, "نام فایل نامعتبر است");

            var fileExtension = Path.GetExtension(fileName).TrimStart('.').ToLower();

            if (!Enum.TryParse<AllowExtensionsFileManager>(fileExtension, true, out var allowExt))
                return new UploadFileManagerResponseModel(400, "نوع فایل نامعتبر است");

            var newFileName = request.IsRandomFileName ? Extensions.GenerateFileName() : Path.GetFileNameWithoutExtension(fileName);
            var uniqueFileName = GetUniqueFileName(fullPath, newFileName, fileExtension);
            var filePath = Path.Combine(fullPath, uniqueFileName);

            try
            {
                using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
                await request.File.CopyToAsync(stream);
            }
            catch (Exception ex)
            {
                return new UploadFileManagerResponseModel(500, $"خطا در آپلود فایل: {ex.Message}");
            }

            var fileAddress = Path.Combine(currentPath, uniqueFileName).Replace("\\", "/");

            return new UploadFileManagerResponseModel(200, "عملیات با موفقیت انجام شد", true, fileAddress);
        }

        public DirectoryFileManagerResponseModel GetTrashDirectories(string? currentPath, FileManagerSearchRequest? searchRequest = null)
        {
            var filePath = Path.Combine(_pathProvider.WebRootPath, _trashRootPath);
            if (!string.IsNullOrEmpty(currentPath))
                filePath = Path.Combine(_pathProvider.WebRootPath, currentPath);


            DirectoryInfo objDirectoryInfo = new DirectoryInfo(filePath);
            if (!Path.Exists(filePath))
            {
                return new DirectoryFileManagerResponseModel(400, "مسیری یافت نشد !");
            }
            if (!objDirectoryInfo.Exists)
            {
                return new DirectoryFileManagerResponseModel(400, "فولدری یافت نشد !");
            }

            var result = GenerateDirectoriesAndFiles(objDirectoryInfo, searchRequest);


            return new DirectoryFileManagerResponseModel(200, "ok", true, result);
        }
        public FileManagerResult RestoreFromTrash(string trashRelativePath, string restorePath)
        {
            try
            {
                var root = _pathProvider.WebRootPath;
                var trashPath = Path.Combine(root, trashRelativePath);
                var targetPath = Path.Combine(root, restorePath, Path.GetFileName(trashRelativePath));

                if (!File.Exists(trashPath) && !Directory.Exists(trashPath))
                    return new FileManagerResult(400, "فایل یا فولدر در Trash یافت نشد");

                var fullTargetPath = Path.GetFullPath(targetPath);
                if (!fullTargetPath.StartsWith(root))
                    return new FileManagerResult(403, "مسیر بازیابی غیرمجاز است");

                var targetDir = Path.GetDirectoryName(fullTargetPath);
                if (!Directory.Exists(targetDir))
                    Directory.CreateDirectory(targetDir);

                if (Directory.Exists(trashPath))
                {
                    Directory.Move(trashPath, fullTargetPath);
                }
                else
                {
                    File.Move(trashPath, fullTargetPath);
                }

                return new FileManagerResult(200, "فایل/فولدر با موفقیت بازیابی شد", true);
            }
            catch (Exception ex)
            {
                return new FileManagerResult(500, $"خطا در بازیابی فایل/فولدر: {ex.Message}");
            }
        }


        public ResizeResponseModel Resizer(ResizeRequestModel request)
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
        private string GetBasePath(string currentPath)
        {
            return Path.Combine(_pathProvider.WebRootPath,
                               string.IsNullOrEmpty(currentPath) ? _rootPath : currentPath);
        }
        [NonAction]
        private string GetUniqueDirectoryName(string basePath, string nameWithoutExt)
        {
            var directoryName = $"{nameWithoutExt}";
            var index = 1;

            while (Directory.Exists(Path.Combine(basePath, directoryName)))
            {
                directoryName = $"{nameWithoutExt}{string.Format("-copy({0})", index)}";
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
        public string GetRelativePath(string fullName)
        {
            string relativePath = Path.GetRelativePath(_pathProvider.WebRootPath, fullName)
              .Replace("\\", "/");

            return relativePath;
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
        private List<DirectoryResponseDto> GenerateDirectoriesAndFiles(DirectoryInfo directoryInfo, FileManagerSearchRequest? searchRequest = null)
        {
            var query = directoryInfo.EnumerateFileSystemInfos("*", SearchOption.TopDirectoryOnly).AsQueryable();

            if (searchRequest is not null)
            {
                if (searchRequest.IsRecursive)
                {
                    query = directoryInfo.EnumerateFileSystemInfos("*", SearchOption.AllDirectories).AsQueryable();
                }
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
                .ThenBy(x => x.LastWriteTime);

            var result = new List<DirectoryResponseDto>();
            foreach (var item in allEntries.Take(2000))
            {
                var isDir = (item.Attributes & FileAttributes.Directory) == FileAttributes.Directory;

                result.Add(new DirectoryResponseDto()
                {
                    DateModified = item.LastWriteTime,
                    IsDirectory = isDir,
                    HasSubDirectories = (item.Attributes & FileAttributes.Directory) == FileAttributes.Directory
                                    && Directory.EnumerateDirectories(item.FullName).Any(),
                    Name = item.Name,
                    Path = GetRelativePath(item.FullName),
                    Size = (item is FileInfo fi && !isDir ? fi.Length : 0),
                    SubscriptionsCount = (item.Attributes & FileAttributes.Directory) == FileAttributes.Directory
                    ? Directory.EnumerateFiles(item.FullName).Count() + Directory.EnumerateDirectories(item.FullName).Count()
                    : 0
                });
            }
            return result;
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
        private (bool, string) CheckRootPath(string path)
        {
            var parts = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            bool isValidPath = Array.Exists(parts, part => part.Equals(_rootPath, StringComparison.OrdinalIgnoreCase));
            if (!isValidPath)
            {
                return (false, "notAllow");
            }
            return (true, "ok");
        }
        [NonAction]
        private (bool, string) CheckRootPath(List<string> paths)
        {
            bool isValidPath = paths.All(p =>
            p.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
             .Any(part => part.Equals(_rootPath, StringComparison.OrdinalIgnoreCase)));

            if (!isValidPath)
            {
                return (false, "notAllow");
            }
            return (true, "ok");
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
                AllowExtensionsFileManager.svg => "image/svg+xml",
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
