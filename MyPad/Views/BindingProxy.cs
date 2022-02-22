using System.Windows;

namespace MyPad.Views;

/// <summary>
/// データプロキシーオブジェクトを表します。
/// </summary>
public class BindingProxy : Freezable
{
    /// <summary>
    /// 保持されるデータの依存関係プロパティ
    /// </summary>
    public static readonly DependencyProperty DataProperty = DependencyPropertyExtensions.Register();

    /// <summary>
    /// 保持されるデータの CLR ラッパープロパティ
    /// </summary>
    public object Data
    {
        get => (object)this.GetValue(DataProperty);
        set => this.SetValue(DataProperty, value);
    }

    /// <summary>
    /// 新しいインスタンスを生成します。
    /// </summary>
    /// <returns>新しいインスタンス</returns>
    protected override Freezable CreateInstanceCore()
        => new BindingProxy();
}
