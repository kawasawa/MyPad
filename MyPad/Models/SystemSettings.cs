using ControlzEx.Theming;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using Vanara.PInvoke;
using WPFLocalizeExtension.Engine;

namespace MyPad.Models;

/// <summary>
/// システム設定を管理するモデルを表します。
/// </summary>
public class SystemSettings : ModelBase
{
    private User32.WINDOWPLACEMENT? _windowPlacement;
    public User32.WINDOWPLACEMENT? WindowPlacement
    {
        get => this._windowPlacement;
        set => this.SetProperty(ref this._windowPlacement, value);
    }

    private bool _saveWindowPlacement = true;
    public bool SaveWindowPlacement
    {
        get => this._saveWindowPlacement;
        set => this.SetProperty(ref this._saveWindowPlacement, value);
    }

    private ThemeType _theme = ThemeType.Sync;
    public ThemeType Theme
    {
        get => this._theme;
        set
        {
            if (this.SetProperty(ref this._theme, value))
                this.ApplyTheme();
        }
    }

    private string _culture = "ja-JP";
    public string Culture
    {
        get => this._culture;
        set
        {
            if (this.SetProperty(ref this._culture, value))
                this.ApplyCulture();
        }
    }

    private int _encodingCodePage = Encoding.UTF8.CodePage;
    public int EncodingCodePage
    {
        get => this._encodingCodePage;
        set
        {
            if (this.SetProperty(ref this._encodingCodePage, value))
                this.RaisePropertyChanged(nameof(this.Encoding));
        }
    }

    [JsonIgnore]
    public Encoding Encoding
    {
        get => Encoding.GetEncoding(this.EncodingCodePage);
        set => this.EncodingCodePage = value.CodePage;
    }

    private bool _autoDetectEncoding = true;
    public bool AutoDetectEncoding
    {
        get => this._autoDetectEncoding;
        set => this.SetProperty(ref this._autoDetectEncoding, value);
    }

    private string _syntaxDefinitionName = string.Empty;
    public string SyntaxDefinitionName
    {
        get => this._syntaxDefinitionName;
        set => this.SetProperty(ref this._syntaxDefinitionName, value);
    }

    private bool _useInAppToastNotifications = true;
    public bool UseInAppToastNotifications
    {
        get => this._useInAppToastNotifications;
        set => this.SetProperty(ref this._useInAppToastNotifications, value);
    }

    private bool _useOverlayDialog = true;
    public bool UseOverlayDialog
    {
        get => this._useOverlayDialog;
        set => this.SetProperty(ref this._useOverlayDialog, value);
    }

    private bool _showFullName = true;
    public bool ShowFullName
    {
        get => this._showFullName;
        set => this.SetProperty(ref this._showFullName, value);
    }

    private bool _showFileIcon = true;
    public bool ShowFileIcon
    {
        get => this._showFileIcon;
        set => this.SetProperty(ref this._showFileIcon, value);
    }

    private bool _showSingleTab;
    public bool ShowSingleTab
    {
        get => this._showSingleTab;
        set => this.SetProperty(ref this._showSingleTab, value);
    }

    private bool _enableNotificationIcon = true;
    public bool EnableNotificationIcon
    {
        get => this._enableNotificationIcon;
        set => this.SetProperty(ref this._enableNotificationIcon, value);
    }

    private bool _enableResident = true;
    public bool EnableResident
    {
        get => this._enableResident;
        set => this.SetProperty(ref this._enableResident, value);
    }

    private bool _enableAutoSave = true;
    public bool EnableAutoSave
    {
        get => this._enableAutoSave;
        set => this.SetProperty(ref this._enableAutoSave, value);
    }

    private int _autoSaveInterval = 10;
    [Range(1, int.MaxValue)]
    public int AutoSaveInterval
    {
        get => this._autoSaveInterval;
        set => this.SetProperty(ref this._autoSaveInterval, value);
    }

    private bool _showMenuBar = true;
    public bool ShowMenuBar
    {
        get => this._showMenuBar;
        set => this.SetProperty(ref this._showMenuBar, value);
    }

    private bool _showToolBar = true;
    public bool ShowToolBar
    {
        get => this._showToolBar;
        set => this.SetProperty(ref this._showToolBar, value);
    }

    private bool _showStatusBar = true;
    public bool ShowStatusBar
    {
        get => this._showStatusBar;
        set => this.SetProperty(ref this._showStatusBar, value);
    }

    private bool _showSideBar = true;
    public bool ShowSideBar
    {
        get => this._showSideBar;
        set => this.SetProperty(ref this._showSideBar, value);
    }

    public SystemSettings()
    {
        this.ApplyTheme();
        this.ApplyCulture();
    }

    public void ApplyTheme()
    {
        if (this.Theme == ThemeType.Sync)
        {
            ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.SyncWithAppMode;
            ThemeManager.Current.SyncTheme();
        }
        else
        {
            ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.DoNotSync;
            ThemeManager.Current.SyncTheme();

            var theme = ThemeManager.Current.GetTheme(this.Theme switch
            {
                ThemeType.Dark => $"{ThemeManager.BaseColorDark}.Blue",
                ThemeType.Light => $"{ThemeManager.BaseColorLight}.Blue",
                _ => throw new InvalidEnumArgumentException(nameof(this.Theme), (int)this.Theme, typeof(ThemeType))
            });
            ThemeManager.Current.ChangeTheme(Application.Current, theme);
            Application.Current?.Windows.OfType<Window>().ForEach(w => ThemeManager.Current.ChangeTheme(w, theme));
        }
    }

    public void ApplyCulture()
    {
        LocalizeDictionary.Instance.Culture = CultureInfo.GetCultureInfo(this.Culture);
    }

    public enum ThemeType
    {
        Sync,
        Dark,
        Light,
    }
}
