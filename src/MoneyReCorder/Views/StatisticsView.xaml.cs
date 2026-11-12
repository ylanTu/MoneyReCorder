using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using MoneyReCorder.ViewModels;

namespace MoneyReCorder.Views;

public partial class StatisticsView : UserControl
{
    private readonly StatisticsViewModel _viewModel;

    public StatisticsView()
    {
        InitializeComponent();
        _viewModel = App.ServiceProvider!.GetRequiredService<StatisticsViewModel>();
        DataContext = _viewModel;

        Loaded += async (_, _) =>
        {
            await _viewModel.LoadStatisticsCommand.ExecuteAsync(null);
        };
    }
}
