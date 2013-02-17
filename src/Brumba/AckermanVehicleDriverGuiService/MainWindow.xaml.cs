using System.Windows;
using System.Windows.Input;

namespace Brumba.AckermanVehicleDriverGuiService
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowViewModel _vm;

        public MainWindow(MainWindowEvents servicePort)
            : this()
        {
            DataContext = _vm = new MainWindowViewModel(servicePort);
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Up:
                case Key.Down:
                case Key.Left:
                case Key.Right:
                case Key.Space:
                    ProcessControls();
                    break;
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Up:
                case Key.Down:
                case Key.Left:
                case Key.Right:
                case Key.Space:
                    ProcessControls();
                    break;
            }
        }

        private void ProcessControls()
        {
            if (Keyboard.IsKeyDown(Key.Space))
                _vm.Break();
            else
                _vm.Power(Keyboard.IsKeyDown(Key.Up) ? 1 : Keyboard.IsKeyDown(Key.Down) ? -1 : 0);
            _vm.Steer(Keyboard.IsKeyDown(Key.Right) ? -1 : Keyboard.IsKeyDown(Key.Left) ? 1 : 0);
        }
    }
}
