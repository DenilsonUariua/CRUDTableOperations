using System.Windows.Controls;

using CRUDTableOperations.ViewModels;

namespace CRUDTableOperations.Views;

public partial class DataGrid1Page : Page
{
    public DataGrid1Page(DataGrid1ViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
