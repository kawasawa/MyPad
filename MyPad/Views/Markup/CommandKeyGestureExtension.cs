using System;
using System.Windows.Markup;

namespace MyPad.Views.Markup;

/// <summary>
/// コマンドからキージェスチャーを取得するためのマークアップ拡張機能を提供します。
/// </summary>
public class CommandKeyGestureExtension : MarkupExtension
{
    public string CommandName { get; set; }

    public CommandKeyGestureExtension(string commandName)
        => this.CommandName = commandName;

    public override object ProvideValue(IServiceProvider serviceProvider)
        => AppCommands.Definitions[this.CommandName].keyGesture;
}
