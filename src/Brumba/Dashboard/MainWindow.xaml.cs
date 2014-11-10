using System.Windows;

namespace Brumba.Dashboard
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public MainWindow(DashboardService serviceAsVm)
            : this()
        {
            DataContext = serviceAsVm;
        }
    }
}
