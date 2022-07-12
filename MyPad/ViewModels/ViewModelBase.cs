using MyBase;

namespace MyPad.ViewModels;

/// <summary>
/// ViewModel の基底クラスを表します。
/// </summary>
public abstract class ViewModelBase : ValidatableBase
{
    /// <summary>
    /// このクラスの新しいインスタンスを生成します。
    /// </summary>
    [LogInterceptorIgnore("基底クラスのコンストラクタであり、多くのクラスから呼び出されるため")]
    public ViewModelBase()
    {
        this.ValidateProperties();
    }
}
