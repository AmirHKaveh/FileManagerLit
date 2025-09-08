using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManagerLite
{
    public class FileManagerOptions
    {
        /// <summary>
        /// Relative root path inside wwwroot (default = "Files")
        /// </summary>
        public string RootPath { get; set; } = "Files";
    }
}
