using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace Brumba.AckermanVehicleDriverGuiService
{
    /// <summary>
    /// Interaction logic for RangeFinderRingUserControl.xaml
    /// </summary>
    public partial class RangeFinderRingUserControl : UserControl
    {
        public static readonly DependencyProperty DispersionConeAngleProperty =
            DependencyProperty.Register("DispersionConeAngle", typeof (double),
                                        typeof (RangeFinderRingUserControl), new FrameworkPropertyMetadata());
        public static readonly DependencyProperty MaximumRangeProperty =
            DependencyProperty.Register("MaximumRange", typeof(double),
                                        typeof(RangeFinderRingUserControl), new FrameworkPropertyMetadata());
        public static readonly DependencyProperty PositionsPolarProperty =
            DependencyProperty.Register("PositionsPolar", typeof(List<Vector>),
                                        typeof(RangeFinderRingUserControl), new FrameworkPropertyMetadata());

        public static readonly DependencyProperty RangesProperty =
            DependencyProperty.Register("Ranges", typeof (List<double>),
                                        typeof (RangeFinderRingUserControl),
                                        new FrameworkPropertyMetadata(
                                            (o, args) => (o as RangeFinderRingUserControl).Invalidate()));

        const int MAX_RANGE_IN_PIXELS = 100;

        public RangeFinderRingUserControl()
        {
            InitializeComponent();
        }

        public double MaximumRange
        {
            get { return (double)GetValue(MaximumRangeProperty); }
            set { SetValue(MaximumRangeProperty, value); }
        }

        public double DispersionConeAngle
        {
            get { return (double)GetValue(DispersionConeAngleProperty); }
            set { SetValue(DispersionConeAngleProperty, value); }
        }

        public List<Vector> PositionsPolar
        {
            get { return (List<Vector>)GetValue(PositionsPolarProperty); }
            set { SetValue(PositionsPolarProperty, value); }
        }

        public List<double> Ranges
        {
            get { return (List<double>)GetValue(RangesProperty); }
            set { SetValue(RangesProperty, value); }
        }

        public void Invalidate()
        {
            Debug.Assert(Ranges.Count == PositionsPolar.Count);
            _canvas.Children.Clear();

            for (var i = 0; i < PositionsPolar.Count; ++i)
            {
                var rangeCone = CreateRangeConePath(PositionsPolar[i].X, PositionsPolar[i].Y,
                                                    Ranges[i], Ranges[i] < MaximumRange*0.99);

                _canvas.Children.Add(rangeCone);
                Canvas.SetLeft(rangeCone, MAX_RANGE_IN_PIXELS*1.2);
                Canvas.SetTop(rangeCone, MAX_RANGE_IN_PIXELS*1.2);
            }
        }

        Path CreateRangeConePath(double rfAnglePos, double rfRadius, double range, bool red)
        {
            var rangeCone = new Path
                {
                    Fill = red ? Brushes.Red : Brushes.Green,
                    Stroke = Brushes.Black,
                    StrokeThickness = 0.5,
                    Data =
                        new PathGeometry(new[] {CreateRangeConePathFigure(range)}, FillRule.Nonzero,
                                         new TransformGroup
                                             {
                                                 Children =
                                                     {
                                                         new RotateTransform(-rfAnglePos*180/Math.PI),
                                                         new TranslateTransform(M2P*-Math.Sin(rfAnglePos)*rfRadius,
                                                                                M2P*-Math.Cos(rfAnglePos)*rfRadius)
                                                     }
                                             })
                };
            return rangeCone;
        }

        PathFigure CreateRangeConePathFigure(double range)
        {
            return new PathFigure(new Point(), new PathSegment[]
                {
                    new LineSegment(new Point(-M2P*range*Math.Sin(DispersionConeAngle/2),
                                              -M2P*range*Math.Cos(DispersionConeAngle/2)),
                                    true),
                    new ArcSegment(new Point(M2P*range*Math.Sin(DispersionConeAngle/2),
                                             -M2P*range*Math.Cos(DispersionConeAngle/2)),
                                   new Size(M2P*range, M2P*range),
                                   DispersionConeAngle*180/Math.PI, false,
                                   SweepDirection.Clockwise, true),
                    new LineSegment(new Point(), true)
                }, false);
        }

        double M2P
        {
            get { return MAX_RANGE_IN_PIXELS / MaximumRange; }
        }
    }
}
