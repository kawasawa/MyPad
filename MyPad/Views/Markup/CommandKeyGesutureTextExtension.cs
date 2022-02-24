using System;
using System.Windows.Input;
using System.Windows.Markup;

namespace MyPad.Views.Markup;

/// <summary>
/// コマンドからキージェスチャー文字列を取得するためのマークアップ拡張機能を提供します。
/// </summary>
public class CommandKeyGesutureTextExtension : MarkupExtension
{
    public string CommandName { get; set; }

    public CommandKeyGesutureTextExtension(string commandName)
        => this.CommandName = commandName;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var keyGesture = Commands.Definitions[this.CommandName].keyGesture;
        if (string.IsNullOrEmpty(keyGesture.DisplayString) == false)
            return keyGesture.DisplayString;

        var text = keyGesture.Key.ToString();
        if (keyGesture.Modifiers.HasFlag(ModifierKeys.Alt))
            text = $"Alt+{text}";
        if (keyGesture.Modifiers.HasFlag(ModifierKeys.Shift))
            text = $"Shift+{text}";
        if (keyGesture.Modifiers.HasFlag(ModifierKeys.Control))
            text = $"Ctrl+{text}";
        return text;
    }
}
