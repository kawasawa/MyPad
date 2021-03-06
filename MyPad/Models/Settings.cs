﻿using MyBase;
using MyBase.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using Unity;

namespace MyPad.Models
{
    public class Settings : ModelBase
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

        private ToolSettings _otherTools;
        public ToolSettings OtherTools
        {
            get => this._otherTools;
            set => this.SetProperty(ref this._otherTools, value);
        }

        #endregion

        private void InitializeInternal(bool force)
        {
            if (force)
            {
                this.System = new();
                this.TextEditor = new();
                this.OtherTools = new();
            }
            else
            {
                this.System ??= new();
                this.TextEditor ??= new();
                this.OtherTools ??= new();
            }
        }

        public bool Initialize(bool force)
        {
            try
            {
                this.InitializeInternal(force);
                this.Logger.Debug($"システム設定を初期化しました。");
                return true;
            }
            catch (Exception e)
            {
                this.Logger.Log($"システム設定の初期化に失敗しました。", Category.Warn, e);
                return false;
            }
        }

        public (bool, Settings) Load()
            => this.Load(this.FilePath);

        public (bool, Settings) Load(string path)
        {
            try
            {
                if (File.Exists(path) == false)
                {
                    this.InitializeInternal(true);
                    return (true, this);
                }

                var json = string.Empty;
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(stream, FILE_ENCODING))
                {
                    json = reader.ReadToEnd();
                }

                JsonConvert.PopulateObject(json, this);
                this.InitializeInternal(false);

                this.Logger.Debug($"設定ファイルを読み込みました。: Path={path}");
                return (true, this);
            }
            catch (Exception e)
            {
                this.Logger.Log($"設定ファイルの読み込みに失敗しました。: Path={path}", Category.Warn, e);
                return (false, this);
            }
        }

        public bool Save()
            => this.Save(this.FilePath);

        public bool Save(string path)
        {
            try
            {
                this.CleanUp();

                var json = JsonConvert.SerializeObject(this, Formatting.Indented);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                using (var stream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
                using (var writer = new StreamWriter(stream, FILE_ENCODING))
                {
                    writer.Write(json);
                }
                
                this.Logger.Debug($"設定ファイルを保存しました。: Path={path}");
                return true;
            }
            catch (Exception e)
            {
                this.Logger.Log($"設定ファイルの保存に失敗しました。: Path={path}", Category.Warn, e);
                return false;
            }
        }

        private void CleanUp()
        {
            this.Version = this.ProductInfo.Version.ToString();
            for (var i = this.OtherTools.ExplorerRoots.Count - 1; 0 <= i; i--)
            {
                if (string.IsNullOrEmpty(this.OtherTools.ExplorerRoots[i].Path))
                    this.OtherTools.ExplorerRoots.RemoveAt(i);
            }
        }

        public bool IsDifferentVersion()
            => this.Version != this.ProductInfo.Version.ToString();
    }
}
