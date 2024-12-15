using System.Windows.Controls;

using CRUDTableOperations.Contracts.Views;
using CRUDTableOperations.ViewModels;

using MahApps.Metro.Controls;

namespace CRUDTableOperations.Views;

public partial class ShellDialogWindow : MetroWindow, IShellDialogWindow
{
    public ShellDialogWindow(ShellDialogViewModel viewModel)
    {
        InitializeComponent();
        viewModel.SetResult = OnSetResult;
        DataContext = viewModel;
    }

    public Frame GetDialogFrame()
        => dialogFrame;

    private void OnSetResult(bool? result)
    {
        DialogResult = result;
        Close();
    }
}
