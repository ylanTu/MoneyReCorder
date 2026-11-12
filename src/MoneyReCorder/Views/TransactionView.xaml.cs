using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using MoneyReCorder.ViewModels;

namespace MoneyReCorder.Views;

public partial class TransactionView : UserControl
{
    private readonly TransactionViewModel _viewModel;

    public TransactionView()
    {
        InitializeComponent();
        _viewModel = App.ServiceProvider!.GetRequiredService<TransactionViewModel>();
        DataContext = _viewModel;

        Loaded += async (_, _) =>
        {
            await _viewModel.LoadDataCommand.ExecuteAsync(null);
        };
    }
}
