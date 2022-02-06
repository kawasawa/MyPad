using MahApps.Metro.Controls.Dialogs;
using MyPad.Models;
using MyPad.Properties;
using MyPad.ViewModels.Dialogs;
using MyPad.Views;
using MyPad.Views.Dialogs;
using Prism.Ioc;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ToastNotifications.Core;
using ToastNotifications.Messages;

namespace MyPad.ViewModels
{
    /// <summary>
    /// <see cref="IDialogService"/> インターフェースに対して拡張メソッドを定義します。
    /// </summary>
    public static class IDialogServiceExtensions
    {
        #region トースト

        /// <summary>
        /// 通知用のトーストを表示します。
        /// </summary>
        /// <param name="self"><see cref="IDialogService"/> を実装するインスタンス</param>
        /// <param name="message">メッセージ</param>
        [LogInterceptor]
        public static void ToastNotify(this IDialogService self, string message)
        {
            if (self.UseInAppToastNotifications(out var window))
                GetActiveMainWindow()?.Notifier.ShowInformation(message, CreateToastSettings());
        }

        /// <summary>
        /// 警告用のトーストを表示します。
        /// </summary>
        /// <param name="self"><see cref="IDialogService"/> を実装するインスタンス</param>
        /// <param name="message">メッセージ</param>
        [LogInterceptor]
        public static void ToastWarn(this IDialogService self, string message)
        {
            GetActiveMainWindow()?.Notifier.ShowWarning(message, CreateToastSettings());
        }

        #endregion

        #region メッセージボックス

        /// <summary>
        /// 通知用のメッセージボックスを表示します。
        /// </summary>
        /// <param name="self"><see cref="IDialogService"/> を実装するインスタンス</param>
        /// <param name="message">メッセージ</param>
        [LogInterceptor]
        public static void Notify(this IDialogService self, string message)
        {
            if (self.UseOverlayDialog(out var window))
            {
                var style = MessageDialogStyle.Affirmative;
                var settings = CreateDialogSettings();
                settings.DialogTitleFontSize = 0;
                window.ShowModalMessageExternal(string.Empty, message, style, settings);
            }
            else
            {
                var parameters = new DialogParameters {
                    { nameof(DialogViewModelBase.Title), Resources.Label_Notify },
                    { nameof(MessageBoxViewModelBase.Message), message },
                };
                self.ShowDialog(GetDialogName(), parameters, null);
            }
        }

        /// <summary>
        /// 警告用のメッセージボックスを表示します。
        /// </summary>
        /// <param name="self"><see cref="IDialogService"/> を実装するインスタンス</param>
        /// <param name="message">メッセージ</param>
        [LogInterceptor]
        public static void Warn(this IDialogService self, string message)
        {
            if (self.UseOverlayDialog(out var window))
            {
                var style = MessageDialogStyle.Affirmative;
                var settings = CreateDialogSettings();
                settings.DialogTitleFontSize = 0;
                settings.ColorScheme = MetroDialogColorScheme.Accented;
                window.ShowModalMessageExternal(string.Empty, message, style, settings);
            }
            else
            {
                var parameters = new DialogParameters {
                    { nameof(DialogViewModelBase.Title), Resources.Label_Warn },
                    { nameof(MessageBoxViewModelBase.Message), message },
                };
                self.ShowDialog(GetDialogName(), parameters, null);
            }
        }

        /// <summary>
        /// 確認用のメッセージボックスを表示します。
        /// </summary>
        /// <param name="self"><see cref="IDialogService"/> を実装するインスタンス</param>
        /// <param name="message">メッセージ</param>
        [LogInterceptor]
        public static bool Confirm(this IDialogService self, string message)
        {
            if (self.UseOverlayDialog(out var window))
            {
                var style = MessageDialogStyle.AffirmativeAndNegative;
                var settings = CreateDialogSettings();
                settings.DialogTitleFontSize = 0;
                settings.NegativeButtonText = Resources.Command_Cancel;
                settings.DialogResultOnCancel = MessageDialogResult.Canceled;
                var result = window.ShowModalMessageExternal(string.Empty, message, style, settings);
                return result.ConvertToTernary() ?? false;
            }
            else
            {
                var parameters = new DialogParameters {
                    { nameof(DialogViewModelBase.Title), Resources.Label_Confirm },
                    { nameof(MessageBoxViewModelBase.Message), message },
                };
                var result = ButtonResult.None;
                self.ShowDialog(GetDialogName(), parameters, r => result = r.Result);
                return result.ConvertToTernary() ?? false;
            }
        }

        /// <summary>
        /// "はい" "いいえ" のほかに "キャンセル" を選択肢に持つ確認用のメッセージボックスを表示します。
        /// </summary>
        /// <param name="self"><see cref="IDialogService"/> を実装するインスタンス</param>
        /// <param name="message">メッセージ</param>
        [LogInterceptor]
        public static bool? CancelableConfirm(this IDialogService self, string message)
        {
            if (self.UseOverlayDialog(out var window))
            {
                var style = MessageDialogStyle.AffirmativeAndNegativeAndSingleAuxiliary;
                var settings = CreateDialogSettings();
                settings.DialogTitleFontSize = 0;
                settings.AffirmativeButtonText = Resources.Command_Yes;
                settings.NegativeButtonText = Resources.Command_No;
                settings.FirstAuxiliaryButtonText = Resources.Command_Cancel;
                settings.DialogResultOnCancel = MessageDialogResult.Canceled;
                var result = window.ShowModalMessageExternal(string.Empty, message, style, settings);
                return result.ConvertToTernary();
            }
            else
            {
                var parameters = new DialogParameters {
                    { nameof(DialogViewModelBase.Title), Resources.Label_Confirm },
                    { nameof(MessageBoxViewModelBase.Message), message },
                };
                var result = ButtonResult.None;
                self.ShowDialog(GetDialogName(), parameters, r => result = r.Result);
                return result.ConvertToTernary();
            }
        }

        #endregion

        #region 専用ダイアログ

        /// <summary>
        /// 行番号を選択させるためのダイアログを表示します。
        /// </summary>
        /// <param name="self"><see cref="IDialogService"/> を実装するインスタンス</param>
        /// <param name="textEditor"><see cref="TextEditorViewModel"/> クラスのインスタンス</param>
        /// <returns>処理結果を示す値と行番号</returns>
        [LogInterceptor]
        public async static Task<(bool result, int line)> ChangeLine(this IDialogService self, TextEditorViewModel textEditor)
        {
            var parameters = new DialogParameters {
                { nameof(DialogViewModelBase.Title), Resources.Command_GoToLine },
                { nameof(ChangeLineDialogViewModel.Line), textEditor.Line },
                { nameof(ChangeLineDialogViewModel.MaxLine), textEditor.Document.LineCount },
            };

            if (self.UseOverlayDialog(out var window))
            {
                var settings = CreateDialogSettings();
                var dialog = new CustomDialog(window, settings)
                {
                    Title = Resources.Command_GoToLine,
                    Content = new ChangeLineDialog(),
                };
                var isOpened = true;
                var result = (false, textEditor.Line);
                if (((FrameworkElement)dialog.Content).DataContext is ChangeLineDialogViewModel viewModel)
                {
                    viewModel.OnDialogOpened(parameters);
                    viewModel.RequestClose += async r =>
                    {
                        if (ConvertToTernary(r.Result) == true)
                            result = (true, r.Parameters.GetValue<int>(nameof(ChangeLineDialogViewModel.Line)));
                        try { await window.HideMetroDialogAsync(dialog, settings); } catch { }
                        isOpened = false;
                    };
                }
                await window.ShowMetroDialogAsync(dialog);
                while (isOpened)
                    await Task.Delay(100);
                return result;
            }
            else
            {
                var result = (false, textEditor.Line);
                self.ShowDialog(GetDialogName(), parameters, r =>
                {
                    if (ConvertToTernary(r.Result) == true)
                        result = (true, r.Parameters.GetValue<int>(nameof(ChangeLineDialogViewModel.Line)));
                });
                return result;
            }
        }

        /// <summary>
        /// 文字コードを選択させるためのダイアログを表示します。
        /// </summary>
        /// <param name="self"><see cref="IDialogService"/> を実装するインスタンス</param>
        /// <param name="textEditor"><see cref="TextEditorViewModel"/> クラスのインスタンス</param>
        /// <returns>処理結果を示す値と文字コード</returns>
        [LogInterceptor]
        public async static Task<(bool result, Encoding encoding)> ChangeEncoding(this IDialogService self, TextEditorViewModel textEditor)
        {
            var parameters = new DialogParameters {
                { nameof(ChangeEncodingDialogViewModel.Title), Resources.Command_ChangeEncoding },
                { nameof(ChangeEncodingDialogViewModel.Encoding), textEditor.Encoding },
            };

            if (self.UseOverlayDialog(out var window))
            {
                var settings = CreateDialogSettings();
                var dialog = new CustomDialog(window, settings)
                {
                    Title = Resources.Command_ChangeEncoding,
                    Content = new ChangeEncodingDialog(),
                };
                var isOpened = true;
                var result = (false, textEditor.Encoding);
                if (((FrameworkElement)dialog.Content).DataContext is ChangeEncodingDialogViewModel viewModel)
                {
                    viewModel.OnDialogOpened(parameters);
                    viewModel.RequestClose += async r =>
                    {
                        if (ConvertToTernary(r.Result) == true)
                            result = (true, r.Parameters.GetValue<Encoding>(nameof(ChangeEncodingDialogViewModel.Encoding)));
                        try { await window.HideMetroDialogAsync(dialog, settings); } catch { }
                        isOpened = false;
                    };
                }
                await window.ShowMetroDialogAsync(dialog);
                while (isOpened)
                    await Task.Delay(100);
                return result;
            }
            else
            {
                var result = (false, textEditor.Encoding);
                self.ShowDialog(GetDialogName(), parameters, r =>
                {
                    if (ConvertToTernary(r.Result) == true)
                        result = (true, r.Parameters.GetValue<Encoding>(nameof(ChangeEncodingDialogViewModel.Encoding)));
                });
                return result;
            }
        }

        /// <summary>
        /// シンタックスを選択させるためのダイアログを表示します。
        /// </summary>
        /// <param name="self"><see cref="IDialogService"/> を実装するインスタンス</param>
        /// <param name="textEditor"><see cref="TextEditorViewModel"/> クラスのインスタンス</param>
        /// <returns>処理結果を示す値とシンタックス</returns>
        [LogInterceptor]
        public async static Task<(bool result, string syntax)> ChangeSyntax(this IDialogService self, TextEditorViewModel textEditor)
        {
            var parameters = new DialogParameters {
                { nameof(ChangeSyntaxDialogViewModel.Title), Resources.Command_ChangeSyntax },
                { nameof(ChangeSyntaxDialogViewModel.Syntax), textEditor.SyntaxDefinition?.Name },
            };

            if (self.UseOverlayDialog(out var window))
            {
                var settings = CreateDialogSettings();
                var dialog = new CustomDialog(window, settings)
                {
                    Title = Resources.Command_ChangeSyntax,
                    Content = new ChangeSyntaxDialog(),
                };
                var isOpened = true;
                var result = (false, textEditor.SyntaxDefinition?.Name);
                if (((FrameworkElement)dialog.Content).DataContext is ChangeSyntaxDialogViewModel viewModel)
                {
                    viewModel.OnDialogOpened(parameters);
                    viewModel.RequestClose += async r =>
                    {
                        if (ConvertToTernary(r.Result) == true)
                            result = (true, r.Parameters.GetValue<string>(nameof(ChangeSyntaxDialogViewModel.Syntax)));
                        try { await window.HideMetroDialogAsync(dialog, settings); } catch { }
                        isOpened = false;
                    };
                }
                await window.ShowMetroDialogAsync(dialog);
                while (isOpened)
                    await Task.Delay(100);
                return result;
            }
            else
            {
                var result = (false, textEditor.SyntaxDefinition?.Name);
                self.ShowDialog(GetDialogName(), parameters, r =>
                {
                    if (ConvertToTernary(r.Result) == true)
                        result = (true, r.Parameters.GetValue<string>(nameof(ChangeSyntaxDialogViewModel.Syntax)));
                });
                return result;
            }
        }

        /// <summary>
        /// 差分比較を行うファイルパスを選択させるダイアログを表示します。
        /// </summary>
        /// <param name="self"><see cref="IDialogService"/> を実装するインスタンス</param>
        /// <param name="fileNames">選択肢として表示するファイルパスのリスト</param>
        /// <param name="defaultSourcePath">比較元のファイルパス</param>
        /// <param name="defaultDestinationPath">比較先のファイルパス</param>
        /// <returns>処理結果を示す値と比較元、比較先のファイルパス</returns>
        [LogInterceptor]
        public async static Task<(bool result, string diffSourcePath, string diffDestinationPath)> SelectDiffFiles(this IDialogService self, IEnumerable<string> fileNames, string defaultSourcePath = null, string defaultDestinationPath = null)
        {
            var parameters = new DialogParameters {
                { nameof(SelectDiffFilesDialogViewModel.Title), Resources.Command_Diff },
                { nameof(SelectDiffFilesDialogViewModel.FileNames), fileNames },
                { nameof(SelectDiffFilesDialogViewModel.DiffSourcePath), defaultSourcePath },
                { nameof(SelectDiffFilesDialogViewModel.DiffDestinationPath), defaultDestinationPath },
            };

            if (self.UseOverlayDialog(out var window))
            {
                var settings = CreateDialogSettings();
                var dialog = new CustomDialog(window, settings)
                {
                    Title = Resources.Command_Diff,
                    Content = new SelectDiffFilesDialog(),
                };
                var isOpened = true;
                var result = (false, string.Empty, string.Empty);
                if (((FrameworkElement)dialog.Content).DataContext is SelectDiffFilesDialogViewModel viewModel)
                {
                    viewModel.OnDialogOpened(parameters);
                    viewModel.RequestClose += async r =>
                    {
                        if (ConvertToTernary(r.Result) == true)
                            result = (true,
                                r.Parameters.GetValue<string>(nameof(SelectDiffFilesDialogViewModel.DiffSourcePath)),
                                r.Parameters.GetValue<string>(nameof(SelectDiffFilesDialogViewModel.DiffDestinationPath)));
                        try { await window.HideMetroDialogAsync(dialog, settings); } catch { }
                        isOpened = false;
                    };
                }
                await window.ShowMetroDialogAsync(dialog);
                while (isOpened)
                    await Task.Delay(100);
                return result;
            }
            else
            {
                var result = (false, string.Empty, string.Empty);
                self.ShowDialog(GetDialogName(), parameters, r =>
                {
                    if (ConvertToTernary(r.Result) == true)
                        result = (true,
                            r.Parameters.GetValue<string>(nameof(SelectDiffFilesDialogViewModel.DiffSourcePath)),
                            r.Parameters.GetValue<string>(nameof(SelectDiffFilesDialogViewModel.DiffDestinationPath)));
                });
                return result;
            }
        }

        #endregion

        #region ヘルパーメソッド

        /// <summary>
        /// アクティブな <see cref="MainWindow"/> を取得します。
        /// </summary>
        /// <returns><see cref="MainWindow"/> のインスタンス</returns>
        private static MainWindow GetActiveMainWindow()
            => MvvmHelper.GetMainWindows().FirstOrDefault(x => x.IsActive);

        /// <summary>
        /// ダイアログ名を取得します。
        /// </summary>
        /// <param name="callerName">呼び出し元のメソッド名</param>
        /// <returns>ダイアログ名</returns>
        /// <remarks>
        /// このプログラムでは呼び出し元のメソッド名の末尾に "Dialog" を付与した文字列がダイアログの名称であると推定します。
        /// </remarks>
        private static string GetDialogName([CallerMemberName] string callerName = "")
            => $"{callerName}Dialog";

        /// <summary>
        /// <see cref="IDialogService"/> が内部に保持する <see cref="IContainerExtension"/> のインスタンス取得します。
        /// </summary>
        /// <param name="self"><see cref="IDialogService"/> を実装するインスタンス</param>
        /// <returns><see cref="IContainerExtension"/> を実装するインスタンス</returns>
        private static IContainerExtension GetContainerExtension(this IDialogService self)
            => self.GetType().GetField("_containerExtension", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(self) as IContainerExtension;

        /// <summary>
        /// <see cref="SystemSettings.UseInAppToastNotifications"/> プロパティの値を取得します。
        /// </summary>
        /// <param name="self"><see cref="IDialogService"/> を実装するインスタンス</param>
        /// <param name="window">[戻り値] アクティブな <see cref="MainWindow"/> のインスタンス</param>
        /// <returns>
        /// <see cref="SystemSettings.UseInAppToastNotifications"/> プロパティの値を返却します。
        /// アクティブな <see cref="MainWindow"/> が取得できない場合は常に <see cref="false"/> を返します。
        /// </returns>
        private static bool UseInAppToastNotifications(this IDialogService self, out MainWindow window)
        {
            window = GetActiveMainWindow();
            if (window == null)
                return false;

            var containerExtension = self.GetContainerExtension();
            if (containerExtension == null)
                return false;

            var settings = containerExtension.Resolve<Settings>();
            return settings?.System?.UseInAppToastNotifications ?? false;
        }

        /// <summary>
        /// <see cref="SystemSettings.UseOverlayDialog"/> プロパティの値を取得します。
        /// </summary>
        /// <param name="self"><see cref="IDialogService"/> を実装するインスタンス</param>
        /// <param name="window">[戻り値] アクティブな <see cref="MainWindow"/> のインスタンス</param>
        /// <returns>
        /// <see cref="SystemSettings.UseOverlayDialog"/> プロパティの値を返却します。
        /// アクティブな <see cref="MainWindow"/> が取得できない場合は常に <see cref="false"/> を返します。
        /// </returns>
        private static bool UseOverlayDialog(this IDialogService self, out MainWindow window)
        {
            window = GetActiveMainWindow();
            if (window == null)
                return false;

            var containerExtension = self.GetContainerExtension();
            if (containerExtension == null)
                return false;

            var settings = containerExtension.Resolve<Settings>();
            return settings?.System?.UseOverlayDialog ?? false;
        }

        /// <summary>
        /// <see cref="ButtonResult"/> の値を <see cref="true"/> <see cref="false"/> <see cref="null"/> のいずれかに変換します。
        /// </summary>
        /// <param name="self"><see cref="ButtonResult"/> の値</param>
        /// <returns><see cref="true"/> <see cref="false"/> <see cref="null"/> のいずれかの値</returns>
        /// <remarks>
        /// ダイアログに表示されるボタンの形式は以下のパターンが存在します。
        /// ・OK              : [OK]
        /// ・OKCancel        : [OK] [キャンセル]
        /// ・YesNo           : [はい] [いいえ]
        /// ・YesNoCancel     : [はい] [いいえ] [キャンセル]
        /// ・RetryCancel     : [再試行] [キャンセル]
        /// ・AbortRetryIgnore: [中止] [再試行] [無視]
        /// 
        /// これらを
        /// ・[OK] [はい] [再試行] => true
        /// ・[いいえ] [中止] => false
        /// ・[キャンセル] [無視] ※上記以外(×ボタン押下など) => null
        /// と変換し、値を返却します。
        /// </remarks>
        private static bool? ConvertToTernary(this ButtonResult self)
            => self switch
            {
                ButtonResult.OK or ButtonResult.Yes or ButtonResult.Retry => true,
                ButtonResult.No or ButtonResult.Abort => false,
                _ => null,
            };

        /// <summary>
        /// <see cref="MessageDialogResult"/> の値を <see cref="true"/> <see cref="false"/> <see cref="null"/> のいずれかに変換します。
        /// </summary>
        /// <param name="self"><see cref="MessageDialogResult"/> の値</param>
        /// <returns><see cref="true"/> <see cref="false"/> <see cref="null"/> のいずれかの値</returns>
        private static bool? ConvertToTernary(this MessageDialogResult self)
            => self switch
            {
                MessageDialogResult.Affirmative => true,
                MessageDialogResult.Negative => false,
                _ => null,
            };

        /// <summary>
        /// トーストの設定値を構築します。
        /// </summary>
        /// <param name="callback">トーストがクローズされたときに呼び出されるコールバックメソッド</param>
        /// <returns>トーストの設定値</returns>
        private static MessageOptions CreateToastSettings(Action<bool?> callback = null)
            => new()
            {
                // INFO: ToastNotifications の明示的なクローズができない問題への対応
                // トースト本体のクリックでトーストをクローズしたい。この際に閉じるボタンによるクローズと処理を分けたい。
                // Close() では最終的に CloseClickAction が呼び出されるため、閉じるボタンを押した際と区別がつかない。
                // DisplayPart.OnClose() では画面から見えない状態になるが、実体が残留してしまっている。
                // private メソッドの _closeAction を呼び出すことで、正規の終了ルートを再現する。
                NotificationClickAction = n =>
                {
                    var closeAction = typeof(NotificationBase).GetField("_closeAction", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(n) as Action<INotification>;
                    closeAction?.Invoke(n);
                    callback?.Invoke(true);
                },
                CloseClickAction = n => callback?.Invoke(false),
            };

        /// <summary>
        /// ダイアログの設定値を構築します。
        /// </summary>
        /// <returns>ダイアログの設定値</returns>
        private static MetroDialogSettings CreateDialogSettings()
            => new()
            {
                DialogTitleFontSize = 16,
                AffirmativeButtonText = Resources.Command_OK,
                DefaultButtonFocus = MessageDialogResult.Affirmative,
            };

        #endregion
    }
}
