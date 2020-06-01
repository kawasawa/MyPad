using Microsoft.WindowsAPICodePack.Dialogs.Controls;
using MyPad.Models;
using MyPad.Properties;
using System.Linq;
using System.Text;

namespace MyPad.ViewModels
{
    public static class CommonDialogHelper
    {
        public static string CreateFileFilter(SyntaxService syntaxService)
        {
            return string.Join("|",
                new[] { $"{Resources.Label_AllFiles}|*.*" }
                .Concat(syntaxService.Definitions.Values.Select(d => $"{d.Name}|{string.Join(";", d.Extensions)}")));
        }

        public static CommonFileDialogComboBox ConvertToComboBox(Encoding defaultEncoding)
        {
            var comboBox = new CommonFileDialogComboBox();
            var encodings = Constants.ENCODINGS;
            for (var i = 0; i < encodings.Count(); i++)
            {
                comboBox.Items.Add(new EncodingComboBoxItem(encodings.ElementAt(i)));
                if (encodings.ElementAt(i) == defaultEncoding)
                    comboBox.SelectedIndex = i;
            }
            return comboBox;
        }

        public class EncodingComboBoxItem : CommonFileDialogComboBoxItem
        {
            public Encoding Encoding { get; }

            public EncodingComboBoxItem(Encoding encoding)
                : this(encoding, encoding?.EncodingName)
            {
            }

            public EncodingComboBoxItem(Encoding encoding, string text)
                : base(text)
            {
                this.Encoding = encoding;
            }
        }
    }
}
