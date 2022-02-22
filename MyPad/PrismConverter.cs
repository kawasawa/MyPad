using System;

namespace MyPad;

/// <summary>
/// Prism の命名規則に従い、情報を変換するためのコンバーターを表します。
/// </summary>
public static class PrismConverter
{
    /// <summary>
    /// 指定された型情報をもとに MVVM の命名規則に該当する箇所を除去した固有名称を推定します。
    /// </summary>
    /// <param name="objectType">オブジェクトの型</param>
    /// <returns>固有名称</returns>
    /// <remarks>
    /// 以下の順に変換を行います。
    ///   1. 型の名前が "Model" で終わる場合は、それを取り除く。
    ///      上記以外の場合は、何もしない。
    ///   2. [1] の結果が "View" で終わる場合は、それを取り除く。
    ///      上記以外の場合は、何もしない。
    ///   -> [2] の結果を戻り値として返す。
    /// </remarks>
    public static string ConvertToCoreName<T>()
        => ConvertToCoreName(typeof(T));

    /// <summary>
    /// 指定された型情報をもとに MVVM の命名規則に該当する箇所を除去した固有名称を推定します。
    /// </summary>
    /// <param name="objectType">オブジェクトの型</param>
    /// <returns>固有名称</returns>
    /// <remarks>
    /// 以下の順に変換を行います。
    ///   1. 型の名前が "Model" で終わる場合は、それを取り除く。
    ///      上記以外の場合は、何もしない。
    ///   2. [1] の結果が "View" で終わる場合は、それを取り除く。
    ///      上記以外の場合は、何もしない。
    ///   -> [2] の結果を戻り値として返す。
    /// </remarks>
    public static string ConvertToCoreName(Type objectType)
    {
        var coreName = objectType.Name;
        if (coreName.EndsWith("Model"))
            coreName = coreName[..^"Model".Length];
        if (coreName.EndsWith("View"))
            coreName = coreName[..^"View".Length];
        return coreName;
    }

    /// <summary>
    /// 指定された型情報をもとにリージョンの名称を推定します。
    /// </summary>
    /// <<typeparam name="T">オブジェクトの型</typeparam>
    /// <returns>リージョンの名称</returns>
    /// <remarks>
    /// 以下の順に変換を行います。
    ///   1. オブジェクトの固有名称を取得する。
    ///   2. [1] の結果の末尾に "Region" を付与する。
    ///   -> [2] の結果を戻り値として返す。
    /// </remarks>
    public static string ConvertToRegionName<T>()
        => ConvertToRegionName(typeof(T));

    /// <summary>
    /// 指定された型情報をもとにリージョンの名称を推定します。
    /// </summary>
    /// <param name="objectType">オブジェクトの型</param>
    /// <returns>リージョンの名称</returns>
    /// <remarks>
    /// 以下の順に変換を行います。
    ///   1. オブジェクトの固有名称を取得する。
    ///   2. [1] の結果の末尾に "Region" を付与する。
    ///   -> [2] の結果を戻り値として返す。
    /// </remarks>
    public static string ConvertToRegionName(Type objectType)
    {
        var coreName = ConvertToCoreName(objectType);
        return $"{coreName}Region";
    }

    /// <summary>
    /// ViewModel の型情報をもとに View の型情報を推定します。
    /// </summary>
    /// <typeparam name="T">ViewModel の型情報</typeparam>
    /// <returns>View の型情報</returns>
    /// <remarks>
    /// 前提として以下の条件を満たす必要があります。
    ///   ・ViewModel のクラス名は "ViewModel" で終わる。
    /// 以下の順に変換を行います。
    ///   1. 型 <typeparamref name="T"/> の完全修飾名を取得する。
    ///   2. [1] の結果に対して、".ViewsModels." を ".Views." に置換する。
    ///   3. [2] の結果の末尾から "Model" を取り除き、これを View のクラス名と仮定する。
    ///   -> [3] の名称でクラスの型情報を取得できた場合は、それを戻り値として返す。
    ///   4. [3] の結果の末尾から "View" を取り除く。
    ///   -> [4] の名称でクラスの型情報を取得できた場合は、それを戻り値として返す。
    ///</remarks>
    public static Type ViewModelTypeToViewType<T>()
        => ViewModelTypeToViewType(typeof(T));

    /// <summary>
    /// ViewModel の型情報をもとに View の型情報を推定します。
    /// </summary>
    /// <param name="viewModelType">ViewModel の型情報</param>
    /// <returns>View の型情報</returns>
    /// <remarks>
    /// 前提として以下の条件を満たす必要があります。
    ///   ・ViewModel のクラス名は "ViewModel" で終わる。
    /// 以下の順に変換を行います。
    ///   1. 型 <typeparamref name="T"/> の完全修飾名を取得する。
    ///   2. [1] の結果に対して、".ViewsModels." を ".Views." に置換する。
    ///   3. [2] の結果の末尾から "Model" を取り除き、これを View のクラス名と仮定する。
    ///   -> [3] の名称でクラスの型情報を取得できた場合は、それを戻り値として返す。
    ///   4. [3] の結果の末尾から "View" を取り除く。
    ///   -> [4] の名称でクラスの型情報を取得できた場合は、それを戻り値として返す。
    ///</remarks>
    public static Type ViewModelTypeToViewType(Type viewModelType)
    {
        var assemblyName = viewModelType.Assembly.FullName;
        var viewModelName = viewModelType.FullName;
        if (viewModelName.EndsWith("ViewModel") == false)
            throw new ArgumentException("ViewModel のクラス名は \"ViewModel\" で終わるように定義してください。");

        var viewName = viewModelName.Replace(".ViewModels.", ".Views.");
        viewName = viewName[..^"Model".Length];
        var viewType = Type.GetType($"{viewName}, {assemblyName}");
        if (viewType == null)
        {
            viewName = viewName[..^"View".Length];
            viewType = Type.GetType($"{viewName}, {assemblyName}");
        }
        return viewType;
    }

    /// <summary>
    /// View の型情報をもとに ViewModel の型情報を推定します。
    /// </summary>
    /// <typeparam name="T">View の型情報</typeparam>
    /// <returns>ViewModel の型情報</returns>
    /// <remarks>
    /// 以下の順に変換を行います。
    ///   1. 型 <typeparamref name="T"/> の完全修飾名を取得する。
    ///   2. [1] の結果に対して、".Views." を ".ViewModels." に置換する。
    ///   3. [2] の結果の末尾を "ViewModel" となるように整形し、これを ViewModel のクラス名と仮定する。
    ///   -> [3] の名称でクラスの型情報を取得できた場合は、それを戻り値として返す。
    /// </remarks>
    public static Type ViewTypeToViewModelType<T>()
        => ViewTypeToViewModelType(typeof(T));

    /// <summary>
    /// View の型情報をもとに ViewModel の型情報を推定します。
    /// </summary>
    /// <param name="viewType">View の型情報</param>
    /// <returns>ViewModel の型情報</returns>
    /// <remarks>
    /// 以下の順に変換を行います。
    ///   1. 型 <typeparamref name="T"/> の完全修飾名を取得する。
    ///   2. [1] の結果に対して、".Views." を ".ViewModels." に置換する。
    ///   3. [2] の結果の末尾を "ViewModel" となるように整形し、これを ViewModel のクラス名と仮定する。
    ///   -> [3] の名称でクラスの型情報を取得できた場合は、それを戻り値として返す。
    /// </remarks>
    public static Type ViewTypeToViewModelType(Type viewType)
    {
        var assemblyName = viewType.Assembly.FullName;
        var viewName = viewType.FullName;
        var viewModelName = viewName.Replace(".Views.", ".ViewModels.");
        viewModelName += viewModelName.EndsWith("View") ? "Model" : "ViewModel";
        var viewModelType = Type.GetType($"{viewModelName}, {assemblyName}");
        return viewModelType;
    }
}
