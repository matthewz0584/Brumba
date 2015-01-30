using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using MathNet.Numerics.LinearAlgebra.Double;
using Matrix = MathNet.Numerics.LinearAlgebra.Double.Matrix;

namespace Brumba.WaiterStupid.GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            HeatMapVm = new HeatMapViewModel();
			HeatMapVm2 = new HeatMapViewModel();  
        }

	    public HeatMapViewModel HeatMapVm2 { get; set; }

	    public HeatMapViewModel HeatMapVm { get; set; }

        public void ShowMatrix(Matrix matrix)
        {            
            HeatMapVm.UpdateHeatMap(matrix);
        }
		public void ShowMatrix2(Matrix matrix)
        {            
            HeatMapVm2.UpdateHeatMap(matrix);
        }
        public void InitVisual(string matrixName, Color? minColor = null, Color? maxColor = null)
        {
            HeatMapVm.InitVisual(matrixName,minColor,maxColor);
        }
    }

}

