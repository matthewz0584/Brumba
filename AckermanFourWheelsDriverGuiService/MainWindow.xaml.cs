using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Brumba.Simulation.AckermanFourWheelsDriverGuiService
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
            _vm.Steer(Keyboard.IsKeyDown(Key.Right) ? 1 : Keyboard.IsKeyDown(Key.Left) ? -1 : 0);
        }
    }
}
