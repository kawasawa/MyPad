using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using System;
using System.Windows.Media;

namespace MyPad.Views.Controls.Completion
{
    /// <summary>
    /// 入力補完候補の情報を格納するモデルを表します。
    /// </summary>
    public class CompletionData : ICompletionData
    {
        public ImageSource Image { get; set; }
        public string Text { get; set; }
        public object Content { get; set; }
        public object Description { get; set; }
        public double Priority { get; set; }

        void ICompletionData.Complete(ICSharpCode.AvalonEdit.Editing.TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
            => textArea.Document.Replace(completionSegment, this.Text);
    }
}
