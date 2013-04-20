using System;

namespace Brumba.AckermanVehicleDriverGuiService
{
    public class MainWindowViewModel
    {
        private readonly MainWindowEvents _servicePort;
        private float _turretBaseAngle = 0;

        public MainWindowViewModel(MainWindowEvents servicePort)
        {
            _servicePort = servicePort;
        }

        public void Steer(float angle)
        {
            _servicePort.Post(new SteerRequest { Value = angle });
        }

        public void Power(float power)
        {
            _servicePort.Post(new PowerRequest { Value = power });
        }

        public void Break()
        {
            _servicePort.Post(new BreakRequest());
        }

        public void SteerTurret(float direction)
        {
            _turretBaseAngle += direction * (float)Math.PI / 90 * 4;
            _servicePort.Post(new TurretBaseAngleRequest { Value = _turretBaseAngle });
        }
    }
}
