using System;

namespace MyPad.Views.Controls.ChangeMarker
{
    // NOTE: FoldingManager と作りを合わせるため用意
    public class ChangeMarkerManager
    {
        private TextArea _textArea;
        private ChangeMarkerMargin _margin;

        private ChangeMarkerManager(TextArea textArea)
        {
            this._textArea = textArea;
            this._margin = new ChangeMarkerMargin();
            this._textArea.LeftMargins.Add(this._margin);
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
