using MyPad.ViewModels;
using MyPad.ViewModels.Regions;
using MyPad.Views;
using MyPad.Views.Regions;
using NUnit.Framework;
using System;

namespace MyPad.Test;

public class PrismConverterTest
{
    [TestCase("MainWindow", typeof(MainWindowViewModel))]
    [TestCase("AboutContent", typeof(AboutContentViewModel))]
    [TestCase("OptionContent", typeof(OptionContentViewModel))]
    [TestCase("AboutContent", typeof(AboutContentViewModel))]
    [TestCase("OptionContent", typeof(OptionContentViewModel))]
    [TestCase("AboutContent", typeof(AboutContentView))]
    [TestCase("OptionContent", typeof(OptionContentView))]
    [TestCase("PrintPreviewContent", typeof(PrintPreviewContentView))]
    [TestCase("DiffContent", typeof(DiffContentView))]
    [TestCase("MenuBar", typeof(MenuBarView))]
    [TestCase("ToolBar", typeof(ToolBarView))]
    [TestCase("StatusBar", typeof(StatusBarView))]
    public void ConvertToCoreName(string expected, Type actual)
        => Assert.That(PrismConverter.ConvertToCoreName(actual), Is.EqualTo(expected));

    [TestCase("AboutContentRegion", typeof(AboutContentView))]
    [TestCase("OptionContentRegion", typeof(OptionContentView))]
    [TestCase("PrintPreviewContentRegion", typeof(PrintPreviewContentView))]
    [TestCase("DiffContentRegion", typeof(DiffContentView))]
    [TestCase("MenuBarRegion", typeof(MenuBarView))]
    [TestCase("ToolBarRegion", typeof(ToolBarView))]
    [TestCase("StatusBarRegion", typeof(StatusBarView))]
    public void ConvertToRegionName(string expected, Type actual)
        => Assert.That(PrismConverter.ConvertToRegionName(actual), Is.EqualTo(expected));

    [TestCase(typeof(AboutContentView), "AboutContentRegion")]
    [TestCase(typeof(OptionContentView), "OptionContentRegion")]
    [TestCase(typeof(PrintPreviewContentView), "PrintPreviewContentRegion")]
    [TestCase(typeof(DiffContentView), "DiffContentRegion")]
    [TestCase(typeof(MenuBarView), "MenuBarRegion")]
    [TestCase(typeof(ToolBarView), "ToolBarRegion")]
    [TestCase(typeof(StatusBarView), "StatusBarRegion")]
    public void RegionNameToRegionType(Type expected, string actual)
        => Assert.That(PrismConverter.RegionNameToRegionType(actual), Is.EqualTo(expected));

    [TestCase(typeof(MainWindow), typeof(MainWindowViewModel))]
    [TestCase(typeof(AboutContentView), typeof(AboutContentViewModel))]
    [TestCase(typeof(OptionContentView), typeof(OptionContentViewModel))]
    public void ViewModelTypeToViewType(Type expected, Type actual)
        => Assert.That(PrismConverter.ViewModelTypeToViewType(actual), Is.EqualTo(expected));

    [TestCase(typeof(MainWindowViewModel), typeof(MainWindow))]
    [TestCase(typeof(AboutContentViewModel), typeof(AboutContentView))]
    [TestCase(typeof(OptionContentViewModel), typeof(OptionContentView))]
    public void ViewTypeToViewModelType(Type expected, Type actual)
        => Assert.That(PrismConverter.ViewTypeToViewModelType(actual), Is.EqualTo(expected));
}
