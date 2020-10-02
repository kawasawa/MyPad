using Newtonsoft.Json;
using Plow;
using Prism.Logging;
using System;
using System.IO;
using System.Text;
using Unity;

namespace MyPad.Models
{
    public sealed class SettingsService : ModelBase
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

        private string _version;
        public string Version
        {
            get => this._version;
            set => this.SetProperty(ref this._version, value);
        }

        private SystemSettings _system;
        public SystemSettings System
        {
            get => this._system;
            set => this.SetProperty(ref this._system, value);
        }

        private TextEditorSettings _textEditor;
        public TextEditorSettings TextEditor
        {
            get => this._textEditor;
            set => this.SetProperty(ref this._textEditor, value);
        }

        #endregion

        public bool Initialize()
        {
            try
            {
                this.System = new SystemSettings();
                this.TextEditor = new TextEditorSettings();

                this.Logger.Log($"システム設定を初期化しました。", Category.Debug);
                return true;
            }
            catch (Exception e)
            {
                this.Logger.Log($"システム設定の初期化に失敗しました。", Category.Warn, e);
                return false;
            }
        }

        public bool Load()
            => this.Load(this.FilePath);

        public bool Load(string path)
        {
            if (File.Exists(path) == false)
            {
                this.Initialize();
                return true;
            }

            try
            {
                var json = string.Empty;
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(stream, FILE_ENCODING))
                {
                    json = reader.ReadToEnd();
                }
                JsonConvert.PopulateObject(json, this);

                this.Logger.Log($"設定ファイルを読み込みました。: Path={path}", Category.Debug);
                return true;
            }
            catch (Exception e)
            {
                this.Logger.Log($"設定ファイルの読み込みに失敗しました。: Path={path}", Category.Warn, e);
                return false;
            }
        }

        public bool Save()
            => this.Save(this.FilePath);

        public bool Save(string path)
        {
            try
            {
                this.Version = this.ProductInfo.Version.ToString();
                var json = JsonConvert.SerializeObject(this, Formatting.Indented);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                using (var stream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
                using (var writer = new StreamWriter(stream, FILE_ENCODING))
                {
                    writer.Write(json);
                }
                
                this.Logger.Log($"設定ファイルを保存しました。: Path={path}", Category.Debug);
                return true;
            }
            catch (Exception e)
            {
                this.Logger.Log($"設定ファイルの保存に失敗しました。: Path={path}", Category.Warn, e);
                return false;
            }
        }

        public bool IsDifferentVersion()
            => this.Version != this.ProductInfo.Version.ToString();
    }
}
