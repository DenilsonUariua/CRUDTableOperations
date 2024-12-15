using System.Windows.Controls;

using CRUDTableOperations.Contracts.Views;
using CRUDTableOperations.ViewModels;

using MahApps.Metro.Controls;

namespace CRUDTableOperations.Views;

public partial class ShellWindow : MetroWindow, IShellWindow
{
    public ShellWindow(ShellViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    public Frame GetNavigationFrame()
        => shellFrame;

    public Frame GetRightPaneFrame()
        => rightPaneFrame;

    public void ShowWindow()
        => Show();

    public void CloseWindow()
        => Close();

    public SplitView GetSplitView()
        => splitView;
}
