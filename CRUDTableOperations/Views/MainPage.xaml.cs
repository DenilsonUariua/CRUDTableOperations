using System.Windows.Controls;

using CRUDTableOperations.ViewModels;

namespace CRUDTableOperations.Views;

public partial class MainPage : Page
{
    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
