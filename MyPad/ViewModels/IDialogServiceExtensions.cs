using MahApps.Metro.Controls.Dialogs;
using MyBase.Logging;
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
    public static class IDialogExtensions
    {
        private static T GetActiveWindow<T>()
            where T : Window
            => Application.Current.Windows.OfType<T>().FirstOrDefault(x => x.IsActive);

        private static string GetDialogName([CallerMemberName] string callerName = "")
            // 呼び出し元のメソッド名の末尾に "Dialog" を付与した文字列がダイアログの名称であると推定する
            => $"{callerName}Dialog";

        private static bool? ConvertToTernary(this ButtonResult self)
        {
            // ダイアログに表示されるボタンの形式は以下のパターンを想定している
            // ・OK              : [OK]
            // ・OKCancel        : [OK] [キャンセル]
            // ・YesNo           : [はい] [いいえ]
            // ・YesNoCancel     : [はい] [いいえ] [キャンセル]
            // ・RetryCancel     : [再試行] [キャンセル]
            // ・AbortRetryIgnore: [中止] [再試行] [無視]

            switch (self)
            {
                case ButtonResult.OK:     // OK
                case ButtonResult.Yes:    // はい
                case ButtonResult.Retry:  // 再試行
                    return true;
                case ButtonResult.No:     // いいえ
                case ButtonResult.Abort:  // 中止
                    return false;
                case ButtonResult.Cancel: // キャンセル
                case ButtonResult.Ignore: // 無視
                default:                  // 上記以外 ([×]ボタン押下など)
                    return null;
            }
        }

        private static bool? ConvertToTernary(this MessageDialogResult self)
            => self switch
            {
                MessageDialogResult.Affirmative => true,
                MessageDialogResult.Negative => false,
                _ => null,
            };

        private static MessageOptions CreateToastMessageOptions(Action<bool?> callback = null)
            => new()
            {
                // NOTE: ToastNotifications の明示的なクローズ
                // Close() を使用すると、CloseClickAction が呼び出されてしまう
                // DisplayPart.OnClose() を使用すると、通知は閉じるが実体は残留してしまう
                NotificationClickAction = n => {
                    var closeAction = typeof(NotificationBase).GetField("_closeAction", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(n) as Action<INotification>;
                    closeAction?.Invoke(n);
                    callback?.Invoke(true); 
                },
                CloseClickAction = n => callback?.Invoke(false),
            };

        private static MetroDialogSettings CreateMetroDialogSettings()
            => new()
            {
                DialogTitleFontSize = 16,
                AffirmativeButtonText = Resources.Command_OK,
                DefaultButtonFocus = MessageDialogResult.Affirmative,
            };

        private static IContainerExtension GetContainerExtension(this IDialogService self)
            => self.GetType().GetField("_containerExtension", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(self) as IContainerExtension;

        private static ILoggerFacade GetLogger(this IDialogService self)
        {
            var containerExtension = self.GetContainerExtension();
            if (containerExtension == null)
                return null;

            return containerExtension.Resolve<ILoggerFacade>();
        }

        private static bool UseInAppToastNotifications(IDialogService dialogService, out MainWindow window)
        {
            window = GetActiveWindow<MainWindow>();
            if (window == null)
                return false;

            var containerExtension = dialogService.GetContainerExtension();
            if (containerExtension == null)
                return false;

            var settingsService = containerExtension.Resolve<SettingsService>();
            return settingsService?.System?.UseInAppToastNotifications ?? false;
        }

        private static bool UseOverlayDialog(IDialogService dialogService, out MainWindow window)
        {
            window = GetActiveWindow<MainWindow>();
            if (window == null)
                return false;

            var containerExtension = dialogService.GetContainerExtension();
            if (containerExtension == null)
                return false;

            var settingsService = containerExtension.Resolve<SettingsService>();
            return settingsService?.System?.UseOverlayDialog ?? false;
        }

        [LogInterceptor]
        public static void ToastNotify(this IDialogService self, string message)
        {
            if (UseInAppToastNotifications(self, out var window))
                GetActiveWindow<MainWindow>()?.Notifier.ShowInformation(message, CreateToastMessageOptions());
        }

        [LogInterceptor]
        public static void ToastWarn(this IDialogService self, string message)
        {
            GetActiveWindow<MainWindow>()?.Notifier.ShowWarning(message, CreateToastMessageOptions());
        }

        [LogInterceptor]
        public static void Notify(this IDialogService self, string message)
        {
            if (UseOverlayDialog(self, out var window))
            {
                var style = MessageDialogStyle.Affirmative;
                var settings = CreateMetroDialogSettings();
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

        [LogInterceptor]
        public static void Warn(this IDialogService self, string message)
        {
            if (UseOverlayDialog(self, out var window))
            {
                var style = MessageDialogStyle.Affirmative;
                var settings = CreateMetroDialogSettings();
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

        [LogInterceptor]
        public static bool Confirm(this IDialogService self, string message)
        {
            if (UseOverlayDialog(self, out var window))
            {
                var style = MessageDialogStyle.AffirmativeAndNegative;
                var settings = CreateMetroDialogSettings();
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

        [LogInterceptor]
        public static bool? CancelableConfirm(this IDialogService self, string message)
        {
            if (UseOverlayDialog(self, out var window))
            {
                var style = MessageDialogStyle.AffirmativeAndNegativeAndSingleAuxiliary;
                var settings = CreateMetroDialogSettings();
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

        [LogInterceptor]
        public async static Task<(bool result, int line)> ChangeLine(this IDialogService self, TextEditorViewModel textEditor)
        {
            var parameters = new DialogParameters {
                { nameof(DialogViewModelBase.Title), Resources.Command_GoToLine },
                { nameof(ChangeLineDialogViewModel.Line), textEditor.Line },
                { nameof(ChangeLineDialogViewModel.MaxLine), textEditor.Document.LineCount },
            };

            if (UseOverlayDialog(self, out var window))
            {
                var settings = CreateMetroDialogSettings();
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

        [LogInterceptor]
        public async static Task<(bool result, Encoding encoding)> ChangeEncoding(this IDialogService self, TextEditorViewModel textEditor)
        {
            var parameters = new DialogParameters {
                { nameof(ChangeEncodingDialogViewModel.Title), Resources.Command_ChangeEncoding },
                { nameof(ChangeEncodingDialogViewModel.Encoding), textEditor.Encoding },
            };

            if (UseOverlayDialog(self, out var window))
            {
                var settings = CreateMetroDialogSettings();
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

        [LogInterceptor]
        public async static Task<(bool result, string syntax)> ChangeSyntax(this IDialogService self, TextEditorViewModel textEditor)
        {
            var parameters = new DialogParameters {
                { nameof(ChangeSyntaxDialogViewModel.Title), Resources.Command_ChangeSyntax },
                { nameof(ChangeSyntaxDialogViewModel.Syntax), textEditor.SyntaxDefinition?.Name },
            };

            if (UseOverlayDialog(self, out var window))
            {
                var settings = CreateMetroDialogSettings();
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

        [LogInterceptor]
        public async static Task<(bool result, string diffSourcePath, string diffDestinationPath)> SelectDiffFiles(this IDialogService self, IEnumerable<string> fileNames, string diffSourcePath = null, string diffDestinationPath = null)
        {
            var parameters = new DialogParameters {
                { nameof(SelectDiffFilesDialogViewModel.Title), Resources.Command_Diff },
                { nameof(SelectDiffFilesDialogViewModel.FileNames), fileNames },
                { nameof(SelectDiffFilesDialogViewModel.DiffSourcePath), diffSourcePath },
                { nameof(SelectDiffFilesDialogViewModel.DiffDestinationPath), diffDestinationPath },
            };

            if (UseOverlayDialog(self, out var window))
            {
                var settings = CreateMetroDialogSettings();
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
    }
}
