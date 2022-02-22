using Markdig;
using Markdig.Wpf;
using System.Diagnostics;
using System.Windows.Input;

namespace MyPad.Views.Controls;

/// <summary>
/// マークダウン形式のテキストをプレビューするためのコントロールを表します。
/// </summary>
public class MarkdownViewer : Markdig.Wpf.MarkdownViewer
{
    /// <summary>
    /// このクラスの新しいインスタンスを生成します。
    /// </summary>
    public MarkdownViewer()
    {
        this.Pipeline = new MarkdownPipelineBuilder().UseSupportedExtensions().Build();
        this.CommandBindings.Add(new CommandBinding(
            Commands.Hyperlink,
            (sender, e) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo("cmd", $"/c start \"\" \"{e.Parameter}\"") { CreateNoWindow = true });
                }
                catch
                {
                }
            }
        ));
    }
}
