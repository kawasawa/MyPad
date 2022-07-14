using System;

namespace MyPad;

/// <summary>
/// トレースの対象外であることを示す属性を表します。
/// </summary>
/// <remarks>
/// このクラス自体は何も処理を行いません。トレースしないことを強調するために付与します。
/// </remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Assembly | AttributeTargets.Module)]
public class LogInterceptorIgnoreAttribute : Attribute
{
    /// <summary>
    /// このクラスの新しいインスタンスを生成します。
    /// </summary>
    /// <param name="reason">トレースしない理由を記述します</param>
    public LogInterceptorIgnoreAttribute(string reason) { }
}
