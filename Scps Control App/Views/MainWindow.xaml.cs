using Scps_Control_App.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace Scps_Control_App;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

	private void CycleTextBox_MouseWheel(object sender, MouseWheelEventArgs e)
	{
        int increment = MainWindowViewModel.ScrollWheelCycleDelayIncement;
        MainWindowViewModel viewModel = (MainWindowViewModel)DataContext;
        if (int.TryParse(CycleTextBox.Text, out int result))
        {
			viewModel.CycleDelay = result;
		}

        if (e.Delta > 0)
        {
            viewModel.CycleDelay += increment;
		}

        else if (e.Delta < 0 && viewModel.CycleDelay - increment >= MainWindowViewModel.MinCycleDelay)
        {
			viewModel.CycleDelay -= increment;
		}
    }
}