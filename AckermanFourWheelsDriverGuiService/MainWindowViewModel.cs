using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Brumba.Simulation.AckermanFourWheelsDriverGuiService
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
            _servicePort.Post(new OnPower { Direction = power });
        }

        public void Break()
        {
            _servicePort.Post(new OnBreak());
        }        
    }
}
