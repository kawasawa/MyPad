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

    private string _accentColor = "Blue";
    public string AccentColor
    {
        get => this._accentColor;
        set
        {
            if (this.SetProperty(ref this._accentColor, value))
                this.ApplyAccentColor();
        }
    }

    private UISizeType _uiSize = UISizeType.Small;
    public UISizeType UISize
    {
        get => this._uiSize;
        set
        {
            if (this.SetProperty(ref this._uiSize, value))
                this.ApplyUISize();
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

    private bool _showFullName;
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

    private bool _enableNotifyIcon = true;
    public bool EnableNotifyIcon
    {
        get => this._enableNotifyIcon;
        set => this.SetProperty(ref this._enableNotifyIcon, value);
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
        this.ApplyAccentColor();
        this.ApplyUISize();
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

            var theme = this.Theme switch
            {
                ThemeType.Dark => ThemeManager.BaseColorDark,
                ThemeType.Light => ThemeManager.BaseColorLight,
                _ => throw new InvalidEnumArgumentException()
            };
            ThemeManager.Current.ChangeThemeBaseColor(Application.Current, theme);
            Application.Current?.Windows.OfType<Window>().ForEach(w => ThemeManager.Current.ChangeThemeBaseColor(w, theme));
        }
    }

    public void ApplyAccentColor()
    {
        ThemeManager.Current.ChangeThemeColorScheme(Application.Current, this.AccentColor);
        Application.Current?.Windows.OfType<Window>().ForEach(w => ThemeManager.Current.ChangeThemeColorScheme(w, this.AccentColor));
    }

    public void ApplyUISize()
    {
        var dictionaries = Application.Current.Resources.MergedDictionaries;
        var resourcePath = Enum.GetNames(typeof(UISizeType)).Select(t => $"./Views/Styles/Size/{t}.xaml").ToArray();
        var currentResource = dictionaries.FirstOrDefault(dd => resourcePath.Contains(dd.Source?.OriginalString));
        if (currentResource != null)
            dictionaries.Remove(currentResource);
        dictionaries.Add(new ResourceDictionary { Source = new Uri($"./Views/Styles/Size/{this.UISize}.xaml", UriKind.Relative) });
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

    public enum UISizeType
    {
        Small,
        Medium,
        Large,
    }
}
