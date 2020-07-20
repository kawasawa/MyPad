using Markdig.Wpf;

namespace MyPad.Views.Controls
{
    public class MarkdownViewer : Markdig.Wpf.MarkdownViewer
    {
        public MarkdownViewer()
        {
            this.Pipeline = new Markdig.MarkdownPipelineBuilder().UseSupportedExtensions().Build();
        }
    }
}
