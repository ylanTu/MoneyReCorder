using System.Windows.Controls;

namespace MoneyReCorder.Views;

/// <summary>
/// 总览页。DataContext 直接继承自 MainWindow 的 MainViewModel，
/// 否则会拿到另一个空实例（总资产显示 0、账户列表为空）。
/// </summary>
public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
    }
}
