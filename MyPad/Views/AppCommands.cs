using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Search;
using MyPad.Properties;
using MyPad.ViewModels;
using MyPad.Views.Controls;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Documents;
using System.Windows.Input;

namespace MyPad.Views;

/// <summary>
/// アプリケーションで使用されるコマンドを管理します。
/// </summary>
public static class AppCommands
{
    /// <summary>
    /// コマンドの名称とそれに対応するヘッダー文字列のリソースキー、キージェスチャーのマップ
    /// </summary>
    public static IReadOnlyDictionary<string, (string resourceKey, KeyGesture keyGesture)> Definitions { get; }

    /// <summary>
    /// ショートカットキーが割り当てられているコマンドのリソースとキージェスチャー
    /// </summary>
    public static IEnumerable<(string resourceKey, KeyGesture keyGesture)> ShortcutKeys => Definitions.Values.Where(tuple => tuple.keyGesture?.Key != Key.None);

    static AppCommands()
    {
        Definitions = new Dictionary<string, (string, KeyGesture)>
        {
            // ファイル
            { nameof(MainWindowViewModel.NewCommand), (nameof(Resources.Command_New), new KeyGesture(Key.N, ModifierKeys.Control))},
            { nameof(MainWindowViewModel.NewWindowCommand), (nameof(Resources.Command_NewWindow), new KeyGesture(Key.N, ModifierKeys.Control | ModifierKeys.Shift))},
            { nameof(MainWindowViewModel.OpenCommand), (nameof(Resources.Command_Open), new KeyGesture(Key.O, ModifierKeys.Control))},
            { nameof(MainWindowViewModel.SaveCommand), (nameof(Resources.Command_Save), new KeyGesture(Key.S, ModifierKeys.Control))},
            { nameof(MainWindowViewModel.SaveAsCommand), (nameof(Resources.Command_SaveAs), new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift))},
            { nameof(MainWindowViewModel.SaveAllCommand), (nameof(Resources.Command_SaveAll), new KeyGesture(Key.K, ModifierKeys.Control | ModifierKeys.Shift))},
            { nameof(MainWindowViewModel.PrintDirectCommand), (nameof(Resources.Command_Print), new KeyGesture(Key.None))},
            { nameof(MainWindowViewModel.PrintPreviewCommand), (nameof(Resources.Command_PrintPreview), new KeyGesture(Key.None))},
            { nameof(MainWindowViewModel.CloseCommand), (nameof(Resources.Command_Close), new KeyGesture(Key.F4, ModifierKeys.Control, "Ctrl+F4 / Ctrl+W"))},
            { nameof(MainWindowViewModel.CloseAllCommand), (nameof(Resources.Command_CloseAll), new KeyGesture(Key.None))},
            { nameof(MainWindowViewModel.CloseOtherCommand), (nameof(Resources.Command_CloseOther), new KeyGesture(Key.None))},
            { ApplicationCommands.Close.Name, (nameof(Resources.Command_Exit), new KeyGesture(Key.F4, ModifierKeys.Alt, "Alt+F4 / Ctrl+Shift+W"))},
            { nameof(MainWindowViewModel.ExitApplicationCommand), (nameof(Resources.Command_ExitApplication), new KeyGesture(Key.F4, ModifierKeys.Control | ModifierKeys.Alt, "Ctrl+Alt+F4 / Ctrl+Q"))},

            // 編集
            { ApplicationCommands.Undo.Name, (nameof(Resources.Command_Undo), new KeyGesture(Key.Z, ModifierKeys.Control))},
            { ApplicationCommands.Redo.Name, (nameof(Resources.Command_Redo), new KeyGesture(Key.Y, ModifierKeys.Control, "Ctrl+Y / Ctrl+Shift+Z"))},
            { ApplicationCommands.Cut.Name, (nameof(Resources.Command_Cut), new KeyGesture(Key.X, ModifierKeys.Control))},
            { ApplicationCommands.Copy.Name, (nameof(Resources.Command_Copy), new KeyGesture(Key.C, ModifierKeys.Control))},
            { ApplicationCommands.Paste.Name, (nameof(Resources.Command_Paste), new KeyGesture(Key.V, ModifierKeys.Control))},
            { EditingCommands.Delete.Name, (nameof(Resources.Command_Delete), new KeyGesture(Key.Delete, ModifierKeys.None, "Del"))},
            { ApplicationCommands.SelectAll.Name, (nameof(Resources.Command_SelectAll), new KeyGesture(Key.A, ModifierKeys.Control))},
            { EditingCommands.MoveLeftByWord.Name, (nameof(Resources.Command_MoveLeftByWord), new KeyGesture(Key.Left, ModifierKeys.Control))},
            { EditingCommands.MoveRightByWord.Name, (nameof(Resources.Command_MoveRightByWord), new KeyGesture(Key.Right, ModifierKeys.Control))},
            { EditingCommands.MoveToLineStart.Name, (nameof(Resources.Command_MoveToLineStart), new KeyGesture(Key.Home))},
            { EditingCommands.MoveToLineEnd.Name, (nameof(Resources.Command_MoveToLineEnd), new KeyGesture(Key.End))},
            { EditingCommands.MoveUpByPage.Name, (nameof(Resources.Command_MoveUpByPage), new KeyGesture(Key.PageUp))},
            { EditingCommands.MoveDownByPage.Name, (nameof(Resources.Command_MoveDownByPage), new KeyGesture(Key.PageDown, ModifierKeys.None, "PageDown"))},
            { EditingCommands.MoveToDocumentStart.Name, (nameof(Resources.Command_MoveToDocumentStart), new KeyGesture(Key.Home, ModifierKeys.Control))},
            { EditingCommands.MoveToDocumentEnd.Name, (nameof(Resources.Command_MoveToDocumentEnd), new KeyGesture(Key.End, ModifierKeys.Control))},
            { EditingCommands.SelectLeftByWord.Name, (nameof(Resources.Command_SelectLeftByWord), new KeyGesture(Key.Left, ModifierKeys.Control | ModifierKeys.Shift))},
            { EditingCommands.SelectRightByWord.Name, (nameof(Resources.Command_SelectRightByWord), new KeyGesture(Key.Right, ModifierKeys.Control | ModifierKeys.Shift))},
            { EditingCommands.SelectToLineStart.Name, (nameof(Resources.Command_SelectToLineStart), new KeyGesture(Key.Home, ModifierKeys.Shift))},
            { EditingCommands.SelectToLineEnd.Name, (nameof(Resources.Command_SelectToLineEnd), new KeyGesture(Key.End, ModifierKeys.Shift))},
            { EditingCommands.SelectUpByPage.Name, (nameof(Resources.Command_SelectUpByPage), new KeyGesture(Key.PageUp, ModifierKeys.Shift))},
            { EditingCommands.SelectDownByPage.Name, (nameof(Resources.Command_SelectDownByPage), new KeyGesture(Key.PageDown, ModifierKeys.Shift, "Shift+PageDown"))},
            { EditingCommands.SelectToDocumentStart.Name, (nameof(Resources.Command_SelectToDocumentStart), new KeyGesture(Key.Home, ModifierKeys.Control | ModifierKeys.Shift))},
            { EditingCommands.SelectToDocumentEnd.Name, (nameof(Resources.Command_SelectToDocumentEnd), new KeyGesture(Key.End, ModifierKeys.Control | ModifierKeys.Shift))},
            { TextArea.Commands.ConvertToNarrow.Name, (nameof(Resources.Command_ConvertToNarrow), new KeyGesture(Key.F10))},
            { TextArea.Commands.ConvertToWide.Name, (nameof(Resources.Command_ConvertToWide), new KeyGesture(Key.F9))},
            { AvalonEditCommands.ConvertToLowercase.Name, (nameof(Resources.Command_ConvertToLowerCase), new KeyGesture(Key.U, ModifierKeys.Control | ModifierKeys.Shift))},
            { AvalonEditCommands.ConvertToUppercase.Name, (nameof(Resources.Command_ConvertToUpperCase), new KeyGesture(Key.U, ModifierKeys.Control))},
            { AvalonEditCommands.ConvertToTitleCase.Name, (nameof(Resources.Command_ConvertToTitleCase), new KeyGesture(Key.U, ModifierKeys.Control | ModifierKeys.Alt))},
            { AvalonEditCommands.ConvertTabsToSpaces.Name, (nameof(Resources.Command_ConvertTabsToSpaces), new KeyGesture(Key.T, ModifierKeys.Control | ModifierKeys.Shift))},
            { AvalonEditCommands.ConvertSpacesToTabs.Name, (nameof(Resources.Command_ConvertSpacesToTabs), new KeyGesture(Key.T, ModifierKeys.Control))},
            { TextArea.Commands.Completion.Name, (nameof(Resources.Command_Completion), new KeyGesture(Key.Space, ModifierKeys.Control))},
            { TextArea.Commands.Folding.Name, (nameof(Resources.Command_Folding), new KeyGesture(Key.OemOpenBrackets, ModifierKeys.Control | ModifierKeys.Shift, "Ctrl+Shift+["))},
            { TextArea.Commands.Unfolding.Name, (nameof(Resources.Command_Unfolding), new KeyGesture(Key.OemCloseBrackets, ModifierKeys.Control | ModifierKeys.Shift, "Ctrl+Shift+]"))},

            // 検索
            { ApplicationCommands.Find.Name, (nameof(Resources.Command_Find), new KeyGesture(Key.F, ModifierKeys.Control))},
            { SearchCommands.FindNext.Name, (nameof(Resources.Command_FindNext), new KeyGesture(Key.F3))},
            { SearchCommands.FindPrevious.Name, (nameof(Resources.Command_FindPrev), new KeyGesture(Key.F3, ModifierKeys.Shift))},
            { ApplicationCommands.Replace.Name, (nameof(Resources.Command_Replace), new KeyGesture(Key.H, ModifierKeys.Control))},
            { nameof(MainWindowViewModel.GoToLineCommand), (nameof(Resources.Command_GoToLine), new KeyGesture(Key.G, ModifierKeys.Control))},

            // 表示
            { TextArea.Commands.ZoomIn.Name, (nameof(Resources.Command_ZoomIn), new KeyGesture(Key.OemPlus, ModifierKeys.Control, "Ctrl+Plus"))},
            { TextArea.Commands.ZoomOut.Name, (nameof(Resources.Command_ZoomOut), new KeyGesture(Key.OemMinus, ModifierKeys.Control, "Ctrl+Minus"))},
            { TextArea.Commands.ZoomReset.Name, (nameof(Resources.Command_ZoomReset), new KeyGesture(Key.D0, ModifierKeys.Control, "Ctrl+0"))},
            { nameof(MainWindow.ActivateExplorerCommand), (nameof(Resources.Command_Explorer), new KeyGesture(Key.E, ModifierKeys.Control | ModifierKeys.Shift))},
            { nameof(MainWindow.ActivateGrepCommand), (nameof(Resources.Command_Grep), new KeyGesture(Key.F, ModifierKeys.Control | ModifierKeys.Shift))},
            { nameof(MainWindow.ActivatePropertyCommand), (nameof(Resources.Command_Property), new KeyGesture(Key.Enter, ModifierKeys.Alt, "Alt+Enter"))},
            { nameof(MainWindow.ActivateTerminalCommand), (nameof(Resources.Command_Terminal), new KeyGesture(Key.OemTilde, ModifierKeys.Control, "Ctrl+@"))},
            { nameof(MainWindow.ActivateScriptRunnerCommand), (nameof(Resources.Command_ScriptRunner), new KeyGesture(Key.OemTilde, ModifierKeys.Control | ModifierKeys.Shift, "Ctrl+Shift+@"))},
            { nameof(MainWindow.ActivateTextEditorCommand), (nameof(Resources.Command_TextEditor), new KeyGesture(Key.F6, ModifierKeys.Control))},
            { nameof(MainWindow.SwitchFullScreenModeCommand), (nameof(Resources.Command_SwitchFullScreenMode), new KeyGesture(Key.F11))},

            // ツール
            { nameof(MainWindowViewModel.OptionCommand), (nameof(Resources.Command_Option), new KeyGesture(Key.OemComma, ModifierKeys.Control, "Ctrl+,"))},
            { nameof(MainWindowViewModel.MaintenanceCommand), (nameof(Resources.Command_Maintenance), new KeyGesture(Key.OemComma, ModifierKeys.Control | ModifierKeys.Shift, "Ctrl+Shift+,"))},
            { nameof(MainWindowViewModel.ChangeEncodingCommand), (nameof(Resources.Command_ChangeEncoding), new KeyGesture(Key.E, ModifierKeys.Control))},
            { nameof(MainWindowViewModel.ChangeSyntaxCommand), (nameof(Resources.Command_ChangeSyntax), new KeyGesture(Key.M, ModifierKeys.Control))},
            { nameof(MainWindowViewModel.DiffCommand), (nameof(Resources.Command_Diff), new KeyGesture(Key.D, ModifierKeys.Control))},
            { nameof(MainWindowViewModel.DiffUnmodifiedCommand), (nameof(Resources.Command_DiffUnmodified), new KeyGesture(Key.D, ModifierKeys.Control | ModifierKeys.Shift))},
            { nameof(MainWindowViewModel.SwitchPomodoroTimerCommand), (nameof(Resources.Command_PomodoroTimer), new KeyGesture(Key.D, ModifierKeys.Control | ModifierKeys.Alt))},

            // ヘルプ
            { nameof(MainWindowViewModel.AboutCommand), (nameof(Resources.Command_About), new KeyGesture(Key.F1, ModifierKeys.Control | ModifierKeys.Shift))},
            { nameof(MainWindowViewModel.ShortcutKeysCommand), (nameof(Resources.Command_ShortcutKeys), new KeyGesture(Key.F1, ModifierKeys.Shift))},
        };
    }

    [ModuleInitializer]
    public static void ConfigureCommonCommands()
    {
        SetKeyGesture(ApplicationCommands.Close);
        SetKeyGesture(ApplicationCommands.Undo);
        SetKeyGesture(ApplicationCommands.Redo);
        SetKeyGesture(ApplicationCommands.Cut);
        SetKeyGesture(ApplicationCommands.Copy);
        SetKeyGesture(ApplicationCommands.Paste);
        SetKeyGesture(ApplicationCommands.SelectAll);
        SetKeyGesture(ApplicationCommands.Find);
        SetKeyGesture(ApplicationCommands.Replace);
        SetKeyGesture(EditingCommands.Delete);
        SetKeyGesture(EditingCommands.MoveLeftByWord);
        SetKeyGesture(EditingCommands.MoveRightByWord);
        SetKeyGesture(EditingCommands.MoveToLineStart);
        SetKeyGesture(EditingCommands.MoveToLineEnd);
        SetKeyGesture(EditingCommands.MoveUpByPage);
        SetKeyGesture(EditingCommands.MoveDownByPage);
        SetKeyGesture(EditingCommands.MoveToDocumentStart);
        SetKeyGesture(EditingCommands.MoveToDocumentEnd);
        SetKeyGesture(EditingCommands.SelectLeftByWord);
        SetKeyGesture(EditingCommands.SelectRightByWord);
        SetKeyGesture(EditingCommands.SelectToLineStart);
        SetKeyGesture(EditingCommands.SelectToLineEnd);
        SetKeyGesture(EditingCommands.SelectUpByPage);
        SetKeyGesture(EditingCommands.SelectDownByPage);
        SetKeyGesture(EditingCommands.SelectToDocumentStart);
        SetKeyGesture(EditingCommands.SelectToDocumentEnd);

        // Mac のショートカットキーを再現する
        ApplicationCommands.Redo.InputGestures.Add(new KeyGesture(Key.Z, ModifierKeys.Control | ModifierKeys.Shift));
    }

    [ModuleInitializer]
    public static void ConfigureTextEditorCommands()
    {
        SetKeyGesture(SearchCommands.FindNext);
        SetKeyGesture(SearchCommands.FindPrevious);
        SetKeyGesture(AvalonEditCommands.ConvertToLowercase);
        SetKeyGesture(AvalonEditCommands.ConvertToUppercase);
        SetKeyGesture(AvalonEditCommands.ConvertToTitleCase);
        SetKeyGesture(AvalonEditCommands.ConvertTabsToSpaces);
        SetKeyGesture(AvalonEditCommands.ConvertSpacesToTabs);
        SetKeyGesture(TextArea.Commands.ConvertToNarrow);
        SetKeyGesture(TextArea.Commands.ConvertToWide);
        SetKeyGesture(TextArea.Commands.Completion);
        SetKeyGesture(TextArea.Commands.Folding);
        SetKeyGesture(TextArea.Commands.Unfolding);
        SetKeyGesture(TextArea.Commands.ZoomIn);
        SetKeyGesture(TextArea.Commands.ZoomOut);
        SetKeyGesture(TextArea.Commands.ZoomReset);

        // Ctrl+D が衝突するため割り当てを解除する
        AvalonEditCommands.DeleteLine.InputGestures.Clear();
    }

    private static void SetKeyGesture(RoutedCommand command)
    {
        command.InputGestures.Clear();
        command.InputGestures.Add(Definitions[command.Name].keyGesture);

        // テンキーに対応する
        var numpadKey = Key.None;
        switch (Definitions[command.Name].keyGesture.Key)
        {
            case Key.D0:
                numpadKey = Key.NumPad0;
                break;
            case Key.D1:
                numpadKey = Key.NumPad1;
                break;
            case Key.D2:
                numpadKey = Key.NumPad2;
                break;
            case Key.D3:
                numpadKey = Key.NumPad3;
                break;
            case Key.D4:
                numpadKey = Key.NumPad4;
                break;
            case Key.D5:
                numpadKey = Key.NumPad5;
                break;
            case Key.D6:
                numpadKey = Key.NumPad6;
                break;
            case Key.D7:
                numpadKey = Key.NumPad7;
                break;
            case Key.D8:
                numpadKey = Key.NumPad8;
                break;
            case Key.D9:
                numpadKey = Key.NumPad9;
                break;
            case Key.OemPlus: // "+" キー
                numpadKey = Key.Add;
                break;
            case Key.OemMinus: // "-" キー
                numpadKey = Key.Subtract;
                break;
            case Key.OemPeriod: // "." キー
                numpadKey = Key.Decimal;
                break;
            case Key.Oem2: // "/" キー
                numpadKey = Key.Divide;
                break;
        }
        if (numpadKey != Key.None)
            command.InputGestures.Add(new KeyGesture(numpadKey, Definitions[command.Name].keyGesture.Modifiers));
    }
}
