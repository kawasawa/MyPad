using System;
using System.Windows.Markup;

namespace MyPad.Views.Markup;

/// <summary>
/// コマンドからヘッダー文字列のリソースキーを取得するためのマークアップ拡張機能を提供します。
/// </summary>
public class CommandResourceKeyExtension : MarkupExtension
{
    public string CommandName { get; set; }

    public CommandResourceKeyExtension(string commandName)
        => this.CommandName = commandName;

    public override object ProvideValue(IServiceProvider serviceProvider)
        => Commands.Definitions[this.CommandName].resourceKey;
}
