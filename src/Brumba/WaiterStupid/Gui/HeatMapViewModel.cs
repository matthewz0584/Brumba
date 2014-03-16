using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Media;

using Matrix = MathNet.Numerics.LinearAlgebra.Double.Matrix;

namespace Brumba.WaiterStupid.GUI
{
    public class MatrixEntry
    {
        public int X { get; set; }
        public int Y { get; set; }
        public double Value { get; set; }
    }
    
    public class HeatMapViewModel : INotifyPropertyChanged
    {
        private double m_maxRange;
        private double m_minRange;

        private List<MatrixEntry> m_heatMap;
        private Color m_minColor;
        private Color m_maxColor;
        private string m_matrixName;

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        #region Properties

        public string MatrixName
        {
            get { return m_matrixName; }
            set
            {
                m_matrixName = value;
                PropertyChanged(this, new PropertyChangedEventArgs("MatrixName"));
            }
        }

        public List<MatrixEntry> HeatMap
        {
            get { return m_heatMap; }
            set
            {
                m_heatMap = value;
                PropertyChanged(this, new PropertyChangedEventArgs("HeatMap"));
            }
        }

        public double MaxRange
        {
            get { return m_maxRange; }
            set
            {
                m_maxRange = value;
                PropertyChanged(this, new PropertyChangedEventArgs("MaxRange"));
            }
        }

        public double MinRange
        {
            get { return m_minRange; }
            set
            {
                m_minRange = value;
                PropertyChanged(this, new PropertyChangedEventArgs("MinRange"));
            }
        }

        public Color MinColor
        {
            get { return m_minColor; }
            set
            {
                m_minColor = value;
                PropertyChanged(this, new PropertyChangedEventArgs("MinColor"));
            }
        }

        public Color MaxColor
        {
            get { return m_maxColor; }
            set
            {
                m_maxColor = value;
                PropertyChanged(this, new PropertyChangedEventArgs("MaxColor"));
            }
        }
        #endregion

        public HeatMapViewModel()
        {
            HeatMap = new List<MatrixEntry>();
            MinColor = Colors.Aquamarine;
            MaxColor = Colors.Green;
            MatrixName = "";
        }

        public void InitVisual(string matrixName, Color? minColor = null, Color? maxColor = null)
        {
            MatrixName = matrixName;
            MinColor = minColor.HasValue ? minColor.Value : Colors.Aquamarine;
            MaxColor = maxColor.HasValue ? maxColor.Value : Colors.Green;
        }

        public void UpdateHeatMap(Matrix matrix)
        {
            var lst = new List<MatrixEntry>();
            var ar = matrix.ToColumnWiseArray();
            MinRange = ar.Min();
            MaxRange = ar.Max();
            for (int i = 0; i < matrix.RowCount; i++)
            {
                for (int j = 0; j < matrix.ColumnCount; j++)
                    lst.Add(new MatrixEntry {X = i, Y = j, Value = matrix[i, j]});
            }
            HeatMap = lst;
        }
    }
}
