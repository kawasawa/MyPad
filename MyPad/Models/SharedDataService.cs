using Plow;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace MyPad.Models
{
    public sealed class SharedDataService
    {
        private readonly IProductInfo _productInfo;

        public Process Process { get; }
        public IEnumerable<string> CommandLineArgs { get; set; } = Enumerable.Empty<string>();
        public IEnumerable<string> CachedDirectories { get; set; } = Enumerable.Empty<string>();

        public string Identifier => $"__{this._productInfo.Company}:{this._productInfo.Product}:{this._productInfo.Version}__";
        public string LogDirectoryPath => Path.Combine(this._productInfo.Local, "log");
        public string TempDirectoryPath => Path.Combine(this._productInfo.Temporary, this.Process.StartTime.ToString("yyyyMMddHHmmssfff"));

        public SharedDataService(IProductInfo productInfo, Process process)
        {
            this._productInfo = productInfo;
            this.Process = process;
        }
    }
}
