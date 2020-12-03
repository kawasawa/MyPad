using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using MyPad.Properties;
using Plow;
using Plow.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Unity;

namespace MyPad.Models
{
    public sealed class SyntaxService
    {
        #region インジェクション

        [Dependency]
        public ILoggerFacade Logger { get; set; }

        [Dependency]
        public IProductInfo ProductInfo { get; set; }

        #endregion

        #region プロパティ

        private static readonly Encoding FILE_ENCODING = new UTF8Encoding(true);
        public string DirectoryPath => Path.Combine(this.ProductInfo.Roaming, "xshd");

        private IDictionary<string, XshdSyntaxDefinition> _definitions;
        public IDictionary<string, XshdSyntaxDefinition> Definitions
            => this._definitions ??= this.Enumerate().Distinct(d => d.Name).ToDictionary(d => d.Name, d => d);

        #endregion

        public bool Initialize(bool force)
        {
            try
            {
                if (force == false && Directory.Exists(this.DirectoryPath))
                    return true;

                Directory.CreateDirectory(this.DirectoryPath);
                typeof(Resources).GetProperties().Where(p => p.PropertyType == typeof(byte[]))
                    .ForEach(p =>
                    {
                        using var stream = new FileStream(Path.Combine(this.DirectoryPath, $"{p.Name}.xshd"), FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
                        using var writer = new BinaryWriter(stream, FILE_ENCODING);
                        writer.Write((byte[])p.GetValue(null));
                    });

                this.Logger.Log($"シンタックス定義ファイルを初期化しました。: Path={this.DirectoryPath}", Category.Debug);
                return true;
            }
            catch (Exception e)
            {
                this.Logger.Log($"シンタックス定義ファイルの初期化に失敗しました。: Path={this.DirectoryPath}", Category.Warn, e);
                return false;
            }
        }

        public IEnumerable<XshdSyntaxDefinition> Enumerate()
        {
            if (Directory.Exists(this.DirectoryPath) == false)
                yield break;

            foreach (var path in Directory.EnumerateFiles(this.DirectoryPath))
            {
                XshdSyntaxDefinition definition;
                try
                {
                    using var reader = new XmlTextReader(path);
                    definition = HighlightingLoader.LoadXshd(reader);
                }
                catch
                {
                    continue;
                }
                yield return definition;
            }
        }
    }
}
