using Plow;
using Newtonsoft.Json;
using Prism.Logging;
using System;
using System.Globalization;
using System.IO;
using System.Text;
using Unity;

namespace MyPad.Models
{
    public sealed class SettingsService
    {
        #region インジェクション

        [Dependency]
        [JsonIgnore]
        public ILoggerFacade Logger { get; set; }

        [Dependency]
        [JsonIgnore]
        public IProductInfo ProductInfo { get; set; }

        #endregion

        #region プロパティ

        private static readonly Encoding FILE_ENCODING = new UTF8Encoding(true);
        [JsonIgnore]
        public string FilePath => Path.Combine(this.ProductInfo.Roaming, "settings.json");

        public string AppVersion { get; set; }
        public SystemSettings System { get; set; }
        public TextEditorSettings TextEditor { get; set; }

        #endregion

        public void Load()
        {
            try
            {
                if (File.Exists(this.FilePath) == false)
                {
                    this.System = new SystemSettings { Culture = CultureInfo.CurrentCulture.Name };
                    this.TextEditor = new TextEditorSettings();
                    return;
                }

                var json = string.Empty;
                using (var reader = new StreamReader(this.FilePath, FILE_ENCODING))
                {
                    json = reader.ReadToEnd();
                }
                JsonConvert.PopulateObject(json, this);
                this.Logger.Log($"設定ファイルを読み込みました。: Path={this.FilePath}", Category.Debug);
            }
            catch (Exception e)
            {
                this.Logger.Log($"設定ファイルの読み込みに失敗しました。: Path={this.FilePath}", Category.Warn, e);
            }
            finally
            {
                this.System.ApplyTheme();
            }
        }

        public bool Save()
        {
            try
            {
                this.AppVersion = this.ProductInfo.Version.ToString();
                var json = JsonConvert.SerializeObject(this, Formatting.Indented);
                Directory.CreateDirectory(Path.GetDirectoryName(this.FilePath));
                using (var writer = new StreamWriter(this.FilePath, false, FILE_ENCODING))
                {
                    writer.Write(json);
                }
                this.Logger.Log($"設定ファイルを保存しました。: Path={this.FilePath}", Category.Debug);
                return true;
            }
            catch (Exception e)
            {
                this.Logger.Log($"設定ファイルの保存に失敗しました。: Path={this.FilePath}", Category.Warn, e);
                return false;
            }
        }

        public bool IsDifferentVersion()
            => this.AppVersion != this.ProductInfo.Version.ToString();
    }
}
