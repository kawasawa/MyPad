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
    [LogInterceptorIgnore]
    public ViewModelBase()
    {
        this.ValidateProperties();
    }
}
