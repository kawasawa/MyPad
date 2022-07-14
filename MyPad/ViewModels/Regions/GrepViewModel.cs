using MyBase.Logging;
using MyBase.Wpf.CommonDialogs;
using MyPad.Models;
using MyPad.Properties;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using Unity;

namespace MyPad.ViewModels.Regions;

/// <summary>
/// Grep パネルに対応する ViewModel を表します。
/// </summary>
public class GrepViewModel : RegionViewModelBase
{
    // Constructor Injection
    public SettingsModel Settings { get; set; }

    // Dependency Injection
    [Dependency]
    public ILoggerFacade Logger { get; set; }
    [Dependency]
    public ICommonDialogService DialogService { get; set; }

    public ReactiveCollection<object> Results { get; }

    [Required]
    public ReactiveProperty<string> SearchText { get; }
    [Required]
    public ReactiveProperty<string> Directory { get; }
    [Required]
    public ReactiveProperty<string> SearchPattern { get; }
    [Required]
    public ReactiveProperty<Encoding> Encoding { get; }
    public ReactiveProperty<bool> AutoDetectEncoding { get; }
    public ReactiveProperty<bool> AllDirectories { get; }
    public ReactiveProperty<bool> IgnoreCase { get; }
    public ReactiveProperty<bool> UseRegex { get; }

    public ReactiveProperty<bool> IsPending { get; }
    public ReactiveProperty<bool> CanGrep { get; }

    public ReactiveCommand SelectDirectoryCommand { get; }
    public ReactiveCommand GrepCommand { get; }
    public ReactiveCommand GrepCancelCommand { get; }

    private CancellationTokenSource _grepCancellationTokenSource;

    /// <summary>
    /// このクラスの新しいインスタンスを生成します。
    /// </summary>
    /// <param name="settings">システム設定</param>
    [InjectionConstructor]
    [LogInterceptor]
    public GrepViewModel(SettingsModel settings)
    {
        this.Settings = settings;

        this.Results = new ReactiveCollection<object>().AddTo(this.CompositeDisposable);
        BindingOperations.EnableCollectionSynchronization(this.Results, new object());

        this.SearchText = new ReactiveProperty<string>().SetValidateAttribute(() => this.SearchText).AddTo(this.CompositeDisposable);
        this.Directory = new ReactiveProperty<string>().SetValidateAttribute(() => this.Directory).AddTo(this.CompositeDisposable);
        this.SearchPattern = new ReactiveProperty<string>("*").SetValidateAttribute(() => this.SearchPattern).AddTo(this.CompositeDisposable);
        this.Encoding = new ReactiveProperty<Encoding>(settings.System.Encoding).SetValidateAttribute(() => this.Encoding).AddTo(this.CompositeDisposable);
        this.AutoDetectEncoding = new ReactiveProperty<bool>(settings.System.AutoDetectEncoding).AddTo(this.CompositeDisposable);
        this.AllDirectories = new ReactiveProperty<bool>(true).AddTo(this.CompositeDisposable);
        this.IgnoreCase = new ReactiveProperty<bool>(true).AddTo(this.CompositeDisposable);
        this.UseRegex = new ReactiveProperty<bool>().AddTo(this.CompositeDisposable);

        this.IsPending = new ReactiveProperty<bool>().AddTo(this.CompositeDisposable);
        this.CanGrep = new[] {
                this.IsPending.Inverse(),
                this.SearchText.ObserveHasErrors.Inverse(),
                this.Directory.ObserveHasErrors.Inverse(),
                this.SearchPattern.ObserveHasErrors.Inverse(),
                this.Encoding.ObserveHasErrors.Inverse(),
            }
            .CombineLatestValuesAreAllTrue()
            .ToReactiveProperty()
            .AddTo(this.CompositeDisposable);

        this.SelectDirectoryCommand = this.IsPending.Inverse().ToReactiveCommand()
            .WithSubscribe(() => this.SelectDirectory())
            .AddTo(this.CompositeDisposable);

        this.GrepCommand = this.CanGrep.ToReactiveCommand()
            .WithSubscribe(async () =>
            {
                this._grepCancellationTokenSource ??= new();
                try
                {
                    await this.Grep(this._grepCancellationTokenSource.Token);
                }
                catch
                {
                }

                if (!this._grepCancellationTokenSource.TryReset())
                {
                    this._grepCancellationTokenSource.Dispose();
                    this._grepCancellationTokenSource = null;
                }
            })
            .AddTo(this.CompositeDisposable);

        this.GrepCancelCommand = this.IsPending.ToReactiveCommand()
            .WithSubscribe(() => this._grepCancellationTokenSource?.Cancel())
            .AddTo(this.CompositeDisposable);
    }

    /// <summary>
    /// このインスタンスが保持するリソースを解放します。
    /// </summary>
    /// <param name="disposing">マネージリソースを破棄するかどうかを示す値</param>
    protected override void Dispose(bool disposing)
    {
        this._grepCancellationTokenSource?.Dispose();
        base.Dispose(disposing);
    }

    /// <summary>
    /// ディレクトリを選択します。
    /// </summary>
    [LogInterceptor]
    private void SelectDirectory()
    {
        var parameter = new FolderBrowserDialogParameters();
        var ready = this.DialogService.ShowDialog(parameter);
        if (ready == false)
            return;

        this.Directory.Value = parameter.FileName;
    }

    /// <summary>
    /// Grep を行います。
    /// </summary>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>非同期タスク</returns>
    [LogInterceptor]
    private async Task Grep(CancellationToken cancellationToken = default)
    {
        if (System.IO.Directory.Exists(this.Directory.Value) == false)
        {
            this.DialogService.Notify(Resources.Message_NotifyDirectoryNotFound);
            return;
        }

        try
        {
            this.IsPending.Value = true;

            this.Results.Clear();
            var count = 0;
            await foreach (var (path, line, encoding) in IOHelper.Grep(
                this.SearchText.Value,
                this.Directory.Value,
                this.Encoding.Value,
                this.AutoDetectEncoding.Value,
                this.SearchPattern.Value,
                this.AllDirectories.Value,
                this.IgnoreCase.Value,
                this.UseRegex.Value))
            {
                cancellationToken.ThrowIfCancellationRequested();
                this.Results.AddOnScheduler(new { path, line, encoding });
                count++;
            }

            this.Logger.Log($"Grep を実行しました。: MatchedLines={count}", Category.Info);
            this.DialogService.ToastNotify(Resources.Message_NotifySearchCompleted);
        }
        catch (OperationCanceledException)
        {
            this.Logger.Log($"Grep はキャンセルされました。", Category.Info);
            throw;
        }
        catch (Exception e)
        {
            this.Logger.Log($"Grep に失敗しました。: {nameof(SearchText)}={this.SearchText.Value}, {nameof(Directory)}={this.Directory.Value}, {nameof(Encoding)}={this.Encoding.Value.EncodingName}, {nameof(AutoDetectEncoding)}={this.AutoDetectEncoding.Value}, {nameof(SearchPattern)}={this.SearchPattern.Value}, {nameof(AllDirectories)}={this.AllDirectories.Value}, {nameof(IgnoreCase)}={this.IgnoreCase.Value}, {nameof(UseRegex)}={this.UseRegex.Value}", Category.Warn, e);
            this.DialogService.Warn(e.Message);
            return;
        }
        finally
        {
            this.IsPending.Value = false;
        }
    }
}
