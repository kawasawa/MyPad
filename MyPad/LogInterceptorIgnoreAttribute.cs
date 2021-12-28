using System;

[module: MyPad.LogInterceptorIgnore]
namespace MyPad
{
    /// <summary>
    /// トレースの対象外であることを示す属性を表します。
    /// </summary>
    /// <remarks>
    /// 頻繁に呼び出されるメソッドなどトーレスから除外したいターゲットに付与します。
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Assembly | AttributeTargets.Module)]
    public class LogInterceptorIgnoreAttribute : Attribute
    {
    }
}