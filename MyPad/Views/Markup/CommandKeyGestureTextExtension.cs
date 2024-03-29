﻿using System;
using System.Windows.Markup;

namespace MyPad.Views.Markup;

/// <summary>
/// コマンドからキージェスチャー文字列を取得するためのマークアップ拡張機能を提供します。
/// </summary>
public class CommandKeyGestureTextExtension : MarkupExtension
{
    public string CommandName { get; set; }

    public CommandKeyGestureTextExtension(string commandName)
        => this.CommandName = commandName;

    public override object ProvideValue(IServiceProvider serviceProvider)
        => AppCommands.Definitions[this.CommandName].keyGesture?.GetText();
}
