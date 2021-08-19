using Markdig;
using Markdig.Wpf;
using System.Diagnostics;
using System.Windows.Input;

namespace MyPad.Views.Controls
{
    public class MarkdownViewer : Markdig.Wpf.MarkdownViewer
    {
        public MarkdownViewer()
        {
            this.Pipeline = new MarkdownPipelineBuilder().UseSupportedExtensions().Build();
            this.CommandBindings.Add(new CommandBinding(
                Commands.Hyperlink,
                (sender, e) => { 
                    try { Process.Start(new ProcessStartInfo("cmd", $"/c start \"\" \"{e.Parameter}\"") { CreateNoWindow = true }); } catch { }
                }
            ));
        }
    }
}
