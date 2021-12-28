namespace MyPad.Views.Controls.ChangeMarker
{
    /// <summary>
    /// 変更状態の種類を定義します。
    /// </summary>
    public enum ChangeKind
    {
        /// <summary>
        /// 変更されていない状態を表します。
        /// </summary>
        None,

        /// <summary>
        /// 追加された状態を表します。
        /// </summary>
        Added,

        /// <summary>
        /// 削除された状態を表します。
        /// </summary>
        Deleted,

        /// <summary>
        /// 更新された状態を表します。
        /// </summary>
        Modified,

        /// <summary>
        /// 保存されていない状態を表します。
        /// </summary>
        Unsaved,
    }
}
