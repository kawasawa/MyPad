using Microsoft.Xaml.Behaviors;
using System.Diagnostics;
using System.Windows;

namespace MyPad.Views.Behaviors;

/// <summary>
/// 引数を指定してプロセスを開始させるためのトリガーアクションを表します。
/// </summary>
public class ProcessStartAction : TriggerAction<DependencyObject>
{
    public static readonly DependencyProperty FileNameProperty = DependencyPropertyExtensions.Register();
    public static readonly DependencyProperty ArgumentsProperty = DependencyPropertyExtensions.Register();
    public static readonly DependencyProperty CreateNoWindowProperty = DependencyPropertyExtensions.Register();
    public static readonly DependencyProperty ThrowExceptionProperty = DependencyPropertyExtensions.Register(new PropertyMetadata(true));

    /// <summary>
    /// アプリケーションのパス
    /// </summary>
    public string FileName
    {
        get => (string)this.GetValue(FileNameProperty);
        set => this.SetValue(FileNameProperty, value);
    }

    /// <summary>
    /// コマンドライン引数
    /// </summary>
    public string Arguments
    {
        get => (string)this.GetValue(ArgumentsProperty);
        set => this.SetValue(ArgumentsProperty, value);
    }

    /// <summary>
    /// 起動するアプリケーションのウィンドウを非表示にするかどうかを示す値
    /// </summary>
    public bool CreateNoWindow
    {
        get => (bool)this.GetValue(CreateNoWindowProperty);
        set => this.SetValue(CreateNoWindowProperty, value);
    }

    /// <summary>
    /// 発生した例外をスローするかどうかを示す値
    /// </summary>
    public bool ThrowException
    {
        get => (bool)this.GetValue(ThrowExceptionProperty);
        set => this.SetValue(ThrowExceptionProperty, value);
    }

    /// <summary>
    /// アクションを実行します。
    /// </summary>
    /// <param name="parameter">パラメータ</param>
    protected override void Invoke(object parameter)
    {
        try
        {
            var info = new ProcessStartInfo(this.FileName, this.Arguments) { CreateNoWindow = this.CreateNoWindow };
            Process.Start(info);
        }
        catch when (this.ThrowException == false)
        {
        }
    }
}
