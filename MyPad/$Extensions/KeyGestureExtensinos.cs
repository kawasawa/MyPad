using System.Windows.Input;

namespace MyPad;

/// <summary>
/// <see cref="KeyGesture"/> クラスの拡張メソッドを提供します。
/// </summary>
public static class KeyGestureExtensinos
{
    /// <summary>
    /// キージェスチャーに対応するテキストを取得します。
    /// <see cref="KeyGesture.DisplayString"/> が設定済みであればこれを取得し、
    /// それ以外は <see cref="KeyGesture.Modifiers"/> と <see cref="KeyGesture.Key"/> を組み合わせた文字列を取得します。
    /// </summary>
    /// <param name="self"><see cref="KeyGesture"/> クラスのインスタンス</param>
    public static string GetText(this KeyGesture self)
    {
        if (string.IsNullOrEmpty(self.DisplayString) == false)
            return self.DisplayString;

        var text = self.Key.ToString();
        if (self.Modifiers.HasFlag(ModifierKeys.Alt))
            text = $"Alt+{text}";
        if (self.Modifiers.HasFlag(ModifierKeys.Shift))
            text = $"Shift+{text}";
        if (self.Modifiers.HasFlag(ModifierKeys.Control))
            text = $"Ctrl+{text}";
        return text;
    }
}
