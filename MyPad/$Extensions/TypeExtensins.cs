using System;
using System.Reflection;

namespace MyPad;

/// <summary>
/// <see cref="Type"/> クラスの拡張メソッドを提供します。
/// </summary>
public static class TypeExtensins
{
    /// <summary>
    /// 継承元をさかのぼり、指定されたフィールド名とバインディングフラグに合致するフィールドを取得します。
    /// </summary>
    /// <param name="self">型情報</param>
    /// <param name="name">フィールド名</param>
    /// <param name="bindingAttr">バインディングフラグ/param>
    /// <returns>フィールドの情報</returns>
    public static FieldInfo GetFieldRecursive(this Type self, string name, BindingFlags bindingAttr)
        => self.GetField(name, bindingAttr) ?? self.BaseType?.GetFieldRecursive(name, bindingAttr);

    /// <summary>
    /// 継承元をさかのぼり、指定されたプロパティ名とバインディングフラグに合致するプロパティを取得します。
    /// </summary>
    /// <param name="self">型情報</param>
    /// <param name="name">プロパティ名</param>
    /// <param name="bindingAttr">バインディングフラグ/param>
    /// <returns>プロパティの情報</returns>
    public static PropertyInfo GetPropertyRecursive(this Type self, string name, BindingFlags bindingAttr)
        => self.GetProperty(name, bindingAttr) ?? self.BaseType?.GetPropertyRecursive(name, bindingAttr);

    /// <summary>
    /// 継承元をさかのぼり、指定されたメソッド名とバインディングフラグに合致するメソッドを取得します。
    /// </summary>
    /// <param name="self">型情報</param>
    /// <param name="name">メソッド名</param>
    /// <param name="bindingAttr">バインディングフラグ/param>
    /// <returns>メソッドの情報</returns>
    public static MethodInfo GetMethodRecursive(this Type self, string name, BindingFlags bindingAttr)
        => self.GetMethod(name, bindingAttr) ?? self.BaseType?.GetMethodRecursive(name, bindingAttr);
}
