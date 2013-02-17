namespace Brumba.AckermanVehicleDriverGuiService
{
    public class MainWindowViewModel
    {
        private MainWindowEvents _servicePort;

        public MainWindowViewModel(MainWindowEvents servicePort)
        {
            _servicePort = servicePort;
        }

        public void Steer(float angle)
        {
            _servicePort.Post(new OnSteer { Direction = angle });
        }

        public void Power(float power)
        {
            _servicePort.Post(new OnPower { Power = power });
        }

        public void Break()
        {
            _servicePort.Post(new OnBreak());
        }        
    }
}
