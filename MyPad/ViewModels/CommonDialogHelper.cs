using Microsoft.WindowsAPICodePack.Dialogs.Controls;
using MyPad.Models;
using MyPad.Properties;
using System.Linq;
using System.Text;

namespace MyPad.ViewModels;

/// <summary>
/// コモンダイアログに関連する汎用処理を提供します。
/// </summary>
public static class CommonDialogHelper
{
    /// <summary>
    /// シンタックス定義をもとにダイアログ用のファイルフィルターを構築します。
    /// </summary>
    /// <param name="syntaxService">シンタックス定義管理サービス</param>
    /// <returns>ファイルフィルター</returns>
    public static string CreateFileFilter(SyntaxService syntaxService)
    {
        return string.Join("|",
            new[] { $"{Resources.Label_AllFiles}|*.*" }
            .Concat(syntaxService.Definitions.Values.Select(d => $"{d.Name}|{string.Join(";", d.Extensions)}")));
    }

    /// <summary>
    /// 文字コード選択用のコンボボックスを構築します。
    /// </summary>
    /// <param name="defaultEncoding">既定の文字コード</param>
    /// <returns>文字コード選択用のコンボボックス</returns>
    public static CommonFileDialogComboBox CreateEncodingComboBox(Encoding defaultEncoding)
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

    /// <summary>
    /// 文字コードを格納するコンボボックスアイテムを表します。
    /// </summary>
    public class EncodingComboBoxItem : CommonFileDialogComboBoxItem
    {
        /// <summary>
        /// 文字コード
        /// </summary>
        public Encoding Encoding { get; }

        /// <summary>
        /// このクラスの新しいインスタンスを生成します。
        /// </summary>
        /// <param name="encoding">文字コード</param>
        public EncodingComboBoxItem(Encoding encoding)
            : this(encoding, encoding?.EncodingName)
        {
        }

        /// <summary>
        /// このクラスの新しいインスタンスを生成します。
        /// </summary>
        /// <param name="encoding">文字コード</param>
        /// <param name="text">テキスト</param>
        public EncodingComboBoxItem(Encoding encoding, string text)
            : base(text)
        {
            this.Encoding = encoding;
        }
    }
}
