using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace MyPad;

/// <summary>
/// バルーン通知に関わる処理を提供します。
/// </summary>
public static class BalloonHelper
{
    /// <summary>
    /// バルーン通知内で発生するアクションを定義します。
    /// </summary>
    public static readonly Dictionary<string, Action<string>> ActionList = new();

    /// <summary>
    /// 初期化処理を行います。
    /// </summary>
    [ModuleInitializer]
    public static void Init()
    {
        ToastNotificationManagerCompat.OnActivated += OnActivated;
    }

    /// <summary>
    /// バルーン通知が表示されたとき、またはバルーン通知上でボタンが押されたときにに行う処理を定義します。
    /// </summary>
    /// <param name="e">イベントの情報</param>
    /// </summary>
    [LogInterceptor]
    private static void OnActivated(ToastNotificationActivatedEventArgsCompat e)
    {
        if (string.IsNullOrEmpty(e.Argument))
            return;

        var args = ToastArguments.Parse(e.Argument);
        var kvp = args.FirstOrDefault();
        if (args.TryGetValue(kvp.Key, out var argument) == false)
            return;

        ActionList[kvp.Key].Invoke(argument);
    }
}
