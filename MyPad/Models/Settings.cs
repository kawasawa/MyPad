using MyBase;
using MyBase.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using Unity;

namespace MyPad.Models;

/// <summary>
/// 個々の設定クラスのインスタンスをまとめて保持するモデルを表します。
/// </summary>
public class Settings : ModelBase
{
    private static readonly Encoding FILE_ENCODING = new UTF8Encoding(true);

    [Dependency]
    [JsonIgnore]
    public ILoggerFacade Logger { get; set; }

    [Dependency]
    [JsonIgnore]
    public IProductInfo ProductInfo { get; set; }

    [JsonIgnore]
    public string FilePath => Path.Combine(this.ProductInfo.Roaming, "settings.json");

    [JsonIgnore]
    public bool IsDifferentVersion => this.Version != this.ProductInfo.Version.ToString();

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

    /// <summary>
    /// 初期化処理を行います。
    /// </summary>
    /// <param name="force"><see cref="true"/> が指定された場合、すでにインスタンスが存在していても新たに生成しなおします。</param>
    /// <returns>正常に処理されたかどうかを示す値</returns>
    public bool Initialize(bool force)
    {
        try
        {
            this.InitializeInternalSettings(force);
            this.Logger.Debug($"システム設定を初期化しました。");
            return true;
        }
        catch (Exception e)
        {
            this.Logger.Log($"システム設定の初期化に失敗しました。", Category.Warn, e);
            return false;
        }
    }

    /// <summary>
    /// 内包する設定クラスのインスタンスを生成します。
    /// </summary>
    /// <param name="force"><see cref="true"/> が指定された場合、すでにインスタンスが存在していても新たに生成しなおします。</param>
    private void InitializeInternalSettings(bool force)
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

    /// <summary>
    /// 既定の設定ファイルから設定情報を読み込みます。
    /// </summary>
    /// <returns>正常に処理されたかどうかを示す値とこのインスタンスの組</returns>
    public (bool, Settings) Load()
    {
        return this.Load(this.FilePath);
    }

    /// <summary>
    /// 指定されたパスのファイルから設定情報を読み込みます。
    /// </summary>
    /// <param name="path">ファイルパス</param>
    /// <returns>正常に処理されたかどうかを示す値とこのインスタンスの組</returns>
    public (bool, Settings) Load(string path)
    {
        try
        {
            if (File.Exists(path) == false)
            {
                this.InitializeInternalSettings(true);
                return (true, this);
            }

            var json = string.Empty;
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(stream, FILE_ENCODING))
            {
                json = reader.ReadToEnd();
            }

            JsonConvert.PopulateObject(json, this);
            this.InitializeInternalSettings(false);

            this.Logger.Debug($"設定ファイルを読み込みました。: Path={path}");
            return (true, this);
        }
        catch (Exception e)
        {
            this.Logger.Log($"設定ファイルの読み込みに失敗しました。: Path={path}", Category.Warn, e);
            return (false, this);
        }
    }

    /// <summary>
    /// 既定の設定ファイルに設定情報を出力します。
    /// </summary>
    /// <returns>正常に処理されたかどうかを示す値</returns>
    public bool Save()
    {
        return this.Save(this.FilePath);
    }

    /// <summary>
    /// 指定されたパスに設定情報を出力します。
    /// </summary>
    /// <param name="path">ファイルパス</param>
    /// <returns>正常に処理されたかどうかを示す値</returns>
    public bool Save(string path)
    {
        try
        {
            this.Version = this.ProductInfo.Version.ToString();

            for (var i = this.OtherTools.ExplorerRoots.Count - 1; 0 <= i; i--)
            {
                if (string.IsNullOrEmpty(this.OtherTools.ExplorerRoots[i].Path))
                    this.OtherTools.ExplorerRoots.RemoveAt(i);
            }

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
}
