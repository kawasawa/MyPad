using ICSharpCode.AvalonEdit.Editing;
using System;
using System.Linq;

namespace MyPad.Views.Controls.ChangeMarker
{
    // FoldingManager と作りを合わせるため用意する
    public class ChangeMarkerManager
    {
        private TextArea _textArea;
        private ChangeMarkerMargin _margin;

        private ChangeMarkerManager(TextArea textArea)
        {
            this._textArea = textArea;
            this._margin = new ChangeMarkerMargin();
            // 行番号と罫線の右隣に描画されるように調整する
            var index = this._textArea.LeftMargins.FirstOrDefault() is LineNumberMargin ? 2 : 0;
            this._textArea.LeftMargins.Insert(index, this._margin);
            this._textArea.TextView.Services.AddService(typeof(ChangeMarkerManager), this);
        }

        public static ChangeMarkerManager Install(TextArea textArea)
        {
            if (textArea == null)
                throw new ArgumentNullException(nameof(textArea));
            return new ChangeMarkerManager(textArea);
        }

        public static void Uninstall(ChangeMarkerManager manager)
        {
            if (manager == null)
                throw new ArgumentNullException(nameof(manager));

            manager._textArea.TextView.Services.RemoveService(typeof(ChangeMarkerManager));
            manager._textArea.LeftMargins.Remove(manager._margin);
            manager._margin.Dispose();
            manager._margin = null;
            manager._textArea = null;
        }
    }
}
