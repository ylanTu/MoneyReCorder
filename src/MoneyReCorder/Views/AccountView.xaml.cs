using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using MoneyReCorder.ViewModels;

namespace MoneyReCorder.Views;

public partial class AccountView : UserControl
{
    private readonly AccountViewModel _viewModel;

    public AccountView()
    {
        InitializeComponent();
        _viewModel = App.ServiceProvider!.GetRequiredService<AccountViewModel>();
        DataContext = _viewModel;

        Loaded += async (_, _) =>
        {
            await _viewModel.LoadAccountsCommand.ExecuteAsync(null);
        };
    }
}
