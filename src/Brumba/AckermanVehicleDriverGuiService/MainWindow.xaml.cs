using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Brumba.AckermanVehicleDriverGuiService
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowViewModel _vm;
        private DispatcherTimer _timer;

        public MainWindow(MainWindowEvents servicePort)
            : this()
        {
            DataContext = _vm = new MainWindowViewModel(servicePort);

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _timer.Tick += (sender, args) => ProcessControls();
            _timer.Start();
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        void ProcessControls()
        {
                if (Keyboard.IsKeyDown(Key.Space))
                    _vm.Break();
                else if (Keyboard.IsKeyDown(Key.Up))
                    _vm.Power(1);
                else if (Keyboard.IsKeyDown(Key.Down))
                    _vm.Power(-1);
                else
                    _vm.Power(0);

                if (Keyboard.IsKeyDown(Key.Right))
                    _vm.Steer(-1);
                else if (Keyboard.IsKeyDown(Key.Left))
                    _vm.Steer(1);
                else
                    _vm.Steer(0);

                if (Keyboard.IsKeyDown(Key.A))
                    _vm.SteerTurret(1);
                else if (Keyboard.IsKeyDown(Key.D))
                    _vm.SteerTurret(-1);
        }
    }
}
