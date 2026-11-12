using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using MoneyReCorder.ViewModels;

namespace MoneyReCorder.Views;

public partial class SettingsView : UserControl
{
    private readonly SettingsViewModel _viewModel;

    public SettingsView()
    {
        InitializeComponent();
        _viewModel = App.ServiceProvider!.GetRequiredService<SettingsViewModel>();
        DataContext = _viewModel;
    }
}
