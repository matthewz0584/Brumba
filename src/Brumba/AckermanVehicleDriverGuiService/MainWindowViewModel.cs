using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;

namespace Brumba.AckermanVehicleDriverGuiService
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

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

        public void UpdateCameraFrame(byte[] rawBitmap, int width, int height)
        {
            CameraFrame = BitmapToBitmapImage(MakeBitmap(rawBitmap, width, height));
            PropertyChanged(this, new PropertyChangedEventArgs("CameraFrame"));
        }

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

        public BitmapImage CameraFrame { get; set; }
    }
}
