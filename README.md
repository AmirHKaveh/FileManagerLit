# FileManagerLite

FileManagerLite is a lightweight and easy-to-use .NET library that simplifies file and directory management.  
It provides a clean API for common file system operations such as:

- Getting directories and files
- Copying and moving files
- Creating directories
- Deleting files and directories
- Other essential file system utilities

Designed for developers who need a simple, reusable, and dependency-free solution for file operations across projects.

## Installation
Install via NuGet:

```dotnet add package FileManagerLite```

Or via Package Manager:

```Install-Package FileManagerLite```

## How to use it

Config path provider file for root of environment:
   ```
 public class WebHostPathProvider : IPathProvider
  {
      private readonly IWebHostEnvironment _webHostEnvironment;
      public WebHostPathProvider(IWebHostEnvironment webHostEnvironment)
      {
          _webHostEnvironment = webHostEnvironment;
      }
      public string WebRootPath => _webHostEnvironment.WebRootPath;
  }
   ```

DI config in program.cs file:

```
   services.AddScoped<IPathProvider, WebHostPathProvider>();
   services.AddScoped<IFileManagerService, FileManagerService>();
```

Call actions in contoller:

```
 public class FileManagerController : ControllerBase
 {
     private readonly IFileManagerService _fileManagerService;
     public FileManagerController(IFileManagerService fileManagerService)
     {
         _fileManagerService = fileManagerService;
     }
     [HttpGet]
     [Route("GetDirectories")]
     public async Task<IActionResult> GetDirectories([FromQuery] string? currentPath)
     {
         var response = await _fileManagerService.GetDirectoriesAsync(currentPath);
    
         return response.StatusCode switch
         {
             200 => Ok(new ApiOkResponse(response.Result)),
             400 => BadRequest(new ApiResponse(400, response.Message)),
             _ => StatusCode(500, new ApiResponse(500, response.Message))
         };
     }
}
```

## Optional 
for change root of directory , set this config in program.cs file. ( Default of root is "Files") :

``` 
builder.Services.Configure<FileManagerOptions>(options =>
{
    options.RootPath = "MyUploads";
});
```
