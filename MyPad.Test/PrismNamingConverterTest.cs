using MyPad.ViewModels;
using MyPad.ViewModels.Regions;
using MyPad.Views;
using MyPad.Views.Regions;
using NUnit.Framework;
using System;

namespace MyPad.Test
{
    public class PrismNamingConverterTest
    {
        [TestCase("AboutContentRegion",        typeof(AboutContentView))]
        [TestCase("OptionContentRegion",       typeof(OptionContentView))]
        [TestCase("PrintPreviewContentRegion", typeof(PrintPreviewContentView))]
        [TestCase("DiffContentRegion",         typeof(DiffContentView))]
        [TestCase("MenuBarRegion",             typeof(MenuBarView))]
        [TestCase("ToolBarRegion",             typeof(ToolBarView))]
        [TestCase("StatusBarRegion",           typeof(StatusBarView))]
        public void ConvertToRegionName(string expected, Type actual)
            => Assert.That(PrismNamingConverter.ConvertToRegionName(actual), Is.EqualTo(expected));

        [TestCase(typeof(Workspace),         typeof(WorkspaceViewModel))]
        [TestCase(typeof(MainWindow),        typeof(MainWindowViewModel))]
        [TestCase(typeof(AboutContentView),  typeof(AboutContentViewModel))]
        [TestCase(typeof(OptionContentView), typeof(OptionContentViewModel))]
        public void ViewModelTypeToViewType(Type expected, Type actual)
            => Assert.That(PrismNamingConverter.ViewModelTypeToViewType(actual), Is.EqualTo(expected));

        [TestCase(typeof(WorkspaceViewModel),     typeof(Workspace))]
        [TestCase(typeof(MainWindowViewModel),    typeof(MainWindow))]
        [TestCase(typeof(AboutContentViewModel),  typeof(AboutContentView))]
        [TestCase(typeof(OptionContentViewModel), typeof(OptionContentView))]
        public void ViewTypeToViewModelType(Type expected, Type actual)
            => Assert.That(PrismNamingConverter.ViewTypeToViewModelType(actual), Is.EqualTo(expected));
    }
}