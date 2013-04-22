using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Brumba.HamsterControls
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel _vm;
        private readonly DispatcherTimer _timer;

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

        public MainWindowViewModel Vm
        {
            get { return _vm; }
        }

        void ProcessControls()
        {
                if (Keyboard.IsKeyDown(Key.Space))
                    Vm.Break();
                else if (Keyboard.IsKeyDown(Key.Up))
                    Vm.Power(1);
                else if (Keyboard.IsKeyDown(Key.Down))
                    Vm.Power(-1);
                else
                    Vm.Power(0);

                if (Keyboard.IsKeyDown(Key.Right))
                    Vm.Steer(-1);
                else if (Keyboard.IsKeyDown(Key.Left))
                    Vm.Steer(1);
                else
                    Vm.Steer(0);

                if (Keyboard.IsKeyDown(Key.A))
                    Vm.SteerTurret(1);
                else if (Keyboard.IsKeyDown(Key.D))
                    Vm.SteerTurret(-1);
        }
    }
}
