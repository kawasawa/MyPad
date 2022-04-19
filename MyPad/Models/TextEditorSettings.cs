using Newtonsoft.Json;
using System.Windows;
using System.Windows.Media;

namespace MyPad.Models;

/// <summary>
/// テキストエディタの設定を管理するモデルを表します。
/// </summary>
public class TextEditorSettings : ModelBase
{
    [JsonIgnore]
    public FontFamily FontFamily
    {
        get => new(this.FontFamilyName);
        set
        {
            if (this.SetProperty(ref this._fontFamilyName, value.Source, nameof(this.FontFamilyName)))
                this.RaisePropertyChanged(nameof(this.FontFamily));
        }
    }

    private string _fontFamilyName = SystemFonts.CaptionFontFamily.Source;
    [JsonProperty(propertyName: "FontFamily")]
    public string FontFamilyName
    {
        get => this._fontFamilyName;
        set => this.SetProperty(ref this._fontFamilyName, value);
    }

    [JsonIgnore]
    public FontWeight FontWeight
    {
        get
        {
            if (!string.IsNullOrEmpty(this._fontWeightValue))
                try { return (FontWeight)typeof(FontWeights).GetProperty(this._fontWeightValue).GetValue(null); } catch { }
            return FontWeights.Normal;
        }
        set
        {
            if (this.SetProperty(ref this._fontWeightValue, value.ToString(), nameof(this.FontWeightValue)))
                this.RaisePropertyChanged(nameof(this.FontWeight));
        }
    }

    private string _fontWeightValue = nameof(FontWeights.Normal);
    [JsonProperty(propertyName: "FontWeight")]
    public string FontWeightValue
    {
        get => this._fontWeightValue;
        set => this.SetProperty(ref this._fontWeightValue, value);
    }

    [JsonIgnore]
    public FontStyle FontStyle
    {
        get
        {
            if (!string.IsNullOrEmpty(this._fontStyleValue))
                try { return (FontStyle)typeof(FontStyles).GetProperty(this._fontStyleValue).GetValue(null); } catch { }
            return FontStyles.Normal;
        }
        set
        {
            if (this.SetProperty(ref this._fontStyleValue, value.ToString(), nameof(this.FontStyleValue)))
                this.RaisePropertyChanged(nameof(this.FontStyle));
        }
    }

    private string _fontStyleValue = nameof(FontStyles.Normal);
    [JsonProperty(propertyName: "FontStyle")]
    public string FontStyleValue
    {
        get => this._fontStyleValue;
        set => this.SetProperty(ref this._fontStyleValue, value);
    }

    private double _actualFontSize = 13.0;
    [JsonProperty(propertyName: "FontSize")]
    public double ActualFontSize
    {
        get => this._actualFontSize;
        set => this.SetProperty(ref this._actualFontSize, value);
    }

    private int _zoomIncrement = 2;
    public int ZoomIncrement
    {
        get => this._zoomIncrement;
        set => this.SetProperty(ref this._zoomIncrement, value);
    }

    private int _indentationSize = 4;
    public int IndentationSize
    {
        get => this._indentationSize;
        set => this.SetProperty(ref this._indentationSize, value);
    }

    private bool _highlightCurrentLine = true;
    public bool HighlightCurrentLine
    {
        get => this._highlightCurrentLine;
        set => this.SetProperty(ref this._highlightCurrentLine, value);
    }

    private bool _convertTabsToSpaces;
    public bool ConvertTabsToSpaces
    {
        get => this._convertTabsToSpaces;
        set => this.SetProperty(ref this._convertTabsToSpaces, value);
    }

    private bool _showTabs = true;
    public bool ShowTabs
    {
        get => this._showTabs;
        set => this.SetProperty(ref this._showTabs, value);
    }

    private bool _showSpaces;
    public bool ShowSpaces
    {
        get => this._showSpaces;
        set => this.SetProperty(ref this._showSpaces, value);
    }

    private bool _showBoxForControlCharacters = true;
    public bool ShowBoxForControlCharacters
    {
        get => this._showBoxForControlCharacters;
        set => this.SetProperty(ref this._showBoxForControlCharacters, value);
    }

    private bool _showEndOfLine = true;
    public bool ShowEndOfLine
    {
        get => this._showEndOfLine;
        set => this.SetProperty(ref this._showEndOfLine, value);
    }

    private bool _showLineNumbers = true;
    public bool ShowLineNumbers
    {
        get => this._showLineNumbers;
        set => this.SetProperty(ref this._showLineNumbers, value);
    }

    private bool _showChangeMarker = true;
    public bool ShowChangeMarker
    {
        get => this._showChangeMarker;
        set => this.SetProperty(ref this._showChangeMarker, value);
    }

    private bool _showColumnRuler;
    public bool ShowColumnRuler
    {
        get => this._showColumnRuler;
        set => this.SetProperty(ref this._showColumnRuler, value);
    }

    private int _columnRulerPosition = 80;
    public int ColumnRulerPosition
    {
        get => this._columnRulerPosition;
        set => this.SetProperty(ref this._columnRulerPosition, value);
    }

    private bool _hideCursorWhileTyping = true;
    public bool HideCursorWhileTyping
    {
        get => this._hideCursorWhileTyping;
        set => this.SetProperty(ref this._hideCursorWhileTyping, value);
    }

    private bool _cutCopyWholeLine = true;
    public bool CutCopyWholeLine
    {
        get => this._cutCopyWholeLine;
        set => this.SetProperty(ref this._cutCopyWholeLine, value);
    }

    private bool _cutCopyHtmlFormat;
    public bool CutCopyHtmlFormat
    {
        get => this._cutCopyHtmlFormat;
        set => this.SetProperty(ref this._cutCopyHtmlFormat, value);
    }

    private bool _wordWrap;
    public bool WordWrap
    {
        get => this._wordWrap;
        set => this.SetProperty(ref this._wordWrap, value);
    }

    private double _wordWrapIndentation;
    public double WordWrapIndentation
    {
        get => this._wordWrapIndentation;
        set => this.SetProperty(ref this._wordWrapIndentation, value);
    }

    private bool _inheritWordWrapIndentation;
    public bool InheritWordWrapIndentation
    {
        get => this._inheritWordWrapIndentation;
        set => this.SetProperty(ref this._inheritWordWrapIndentation, value);
    }

    private bool _enableImeSupport = true;
    public bool EnableImeSupport
    {
        get => this._enableImeSupport;
        set => this.SetProperty(ref this._enableImeSupport, value);
    }

    private bool _enableFoldings = true;
    public bool EnableFoldings
    {
        get => this._enableFoldings;
        set => this.SetProperty(ref this._enableFoldings, value);
    }

    private bool _enableAutoCompletion = true;
    public bool EnableAutoCompletion
    {
        get => this._enableAutoCompletion;
        set => this.SetProperty(ref this._enableAutoCompletion, value);
    }

    private bool _enableTextDragDrop = true;
    public bool EnableTextDragDrop
    {
        get => this._enableTextDragDrop;
        set => this.SetProperty(ref this._enableTextDragDrop, value);
    }

    private bool _enableRectangularSelection = true;
    public bool EnableRectangularSelection
    {
        get => this._enableRectangularSelection;
        set => this.SetProperty(ref this._enableRectangularSelection, value);
    }

    private bool _enableVirtualSpace;
    public bool EnableVirtualSpace
    {
        get => this._enableVirtualSpace;
        set => this.SetProperty(ref this._enableVirtualSpace, value);
    }

    private bool _enableHyperlinks = true;
    public bool EnableHyperlinks
    {
        get => this._enableHyperlinks;
        set => this.SetProperty(ref this._enableHyperlinks, value);
    }

    private bool _enableEmailHyperlinks = true;
    public bool EnableEmailHyperlinks
    {
        get => this._enableEmailHyperlinks;
        set => this.SetProperty(ref this._enableEmailHyperlinks, value);
    }

    private bool _requireControlModifierForHyperlinkClick = true;
    public bool RequireControlModifierForHyperlinkClick
    {
        get => this._requireControlModifierForHyperlinkClick;
        set => this.SetProperty(ref this._requireControlModifierForHyperlinkClick, value);
    }

    private bool _allowScrollBelowDocument = true;
    public bool AllowScrollBelowDocument
    {
        get => this._allowScrollBelowDocument;
        set => this.SetProperty(ref this._allowScrollBelowDocument, value);
    }

    private bool _allowToggleOverstrikeMode = true;
    public bool AllowToggleOverstrikeMode
    {
        get => this._allowToggleOverstrikeMode;
        set => this.SetProperty(ref this._allowToggleOverstrikeMode, value);
    }
}
