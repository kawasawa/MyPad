using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace MyPad.Models;

/// <summary>
/// ツールの設定を管理するモデルを表します。
/// </summary>
public class ToolSettings : ModelBase
{
    private ObservableCollection<PathInfo> _explorerRoots = new();
    public ObservableCollection<PathInfo> ExplorerRoots
    {
        get => this._explorerRoots;
        set => this.SetProperty(ref this._explorerRoots, value);
    }

    private bool _showInlineDiffViewer;
    public bool ShowInlineDiffViewer
    {
        get => this._showInlineDiffViewer;
        set => this.SetProperty(ref this._showInlineDiffViewer, value);
    }

    private int _pomodoroWorkMinutes = 25;
    public int PomodoroWorkMinutes
    {
        get => this._pomodoroWorkMinutes;
        set => this.SetProperty(ref this._pomodoroWorkMinutes, value);
    }

    private int _pomodoroBreakMinutes = 5;
    public int PomodoroBreakMinutes
    {
        get => this._pomodoroBreakMinutes;
        set => this.SetProperty(ref this._pomodoroBreakMinutes, value);
    }

    public class PathInfo : ModelBase
    {
        private string _path;
        [Required]
        public string Path
        {
            get => this._path;
            set => this.SetProperty(ref this._path, value);
        }

        private bool _isEnabled = true;
        public bool IsEnabled
        {
            get => this._isEnabled;
            set => this.SetProperty(ref this._isEnabled, value);
        }
    }
}
