using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Brumba.HamsterControls
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private readonly MainWindowEvents _servicePort;
        private BitmapImage _cameraFrame;
        private List<double> _irRfRingRanges;

        public MainWindowViewModel(MainWindowEvents servicePort)
        {
            _servicePort = servicePort;
            //Stolen from Hamster builder
            IrRfDispersionConeAngle = 4 * Math.PI / 180;
            IrRfMaximumRange = 1;
            IrRfPositionsPolar = new List<Vector>
                {
                    new Vector(0, 0.08f),
                    new Vector(Math.PI*1/6, 0.085f),
                    new Vector(Math.PI*2/6, 0.07f),
                    new Vector(Math.PI*3/6, 0.06f),
                    new Vector(Math.PI, 0.1f),
                    new Vector(Math.PI*9/6, 0.06f),
                    new Vector(Math.PI*10/6, 0.07f),
                    new Vector(Math.PI*11/6, 0.085f),
                };
            //IrRfRingRanges = new List<double> { 1, 0.9, 0.8, 0.7, 0.6, 0.5, 0.4, 0.3 };
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

        private float _turretBaseAngle = 0;
        public void SteerTurret(float direction)
        {
            _turretBaseAngle += direction * (float)Math.PI / 90 * 4;
            _servicePort.Post(new TurretBaseAngleRequest { Value = _turretBaseAngle });
        }

        public void UpdateCameraFrame(byte[] rawBitmap, int width, int height)
        {
            CameraFrame = BitmapToBitmapImage(MakeBitmap(rawBitmap, width, height));
        }

        public void UpdateIrRfRing(List<float> ranges)
        {
            IrRfRingRanges = ranges.Select(r => (double)r).ToList();
        }

        public BitmapImage CameraFrame
        {
            get { return _cameraFrame; }
            set
            {
                _cameraFrame = value;
                PropertyChanged(this, new PropertyChangedEventArgs("CameraFrame"));
            }
        }

        public List<double> IrRfRingRanges
        {
            get { return _irRfRingRanges; }
            set
            {
                _irRfRingRanges = value;
                PropertyChanged(this, new PropertyChangedEventArgs("IrRfRingRanges"));
            }
        }

        public List<Vector> IrRfPositionsPolar { get; set; }
        public double IrRfMaximumRange { get; set; }
        public double IrRfDispersionConeAngle { get; set; }

        static Bitmap MakeBitmap(byte[] rawBitmap, int width, int height)
        {
            var bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly,
                                    PixelFormat.Format24bppRgb);
            Marshal.Copy(rawBitmap, 0, data.Scan0, rawBitmap.Length);
            bmp.UnlockBits(data);
            return bmp;
        }

        static BitmapImage BitmapToBitmapImage(Bitmap bmp)
        {
            var bi = new BitmapImage();
            bi.BeginInit();
            var ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Png);
            ms.Position = 0;
            bi.StreamSource = ms;
            bi.EndInit();
            return bi;
        }
    }
}
