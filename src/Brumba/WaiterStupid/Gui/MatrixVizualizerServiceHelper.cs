using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using Microsoft.Ccr.Adapters.Wpf;
using Microsoft.Ccr.Core;
using Matrix = MathNet.Numerics.LinearAlgebra.Double.Matrix;

namespace Brumba.WaiterStupid.GUI
{
    public class MatrixVizualizerServiceHelper
    {
        WpfServicePort _wpfPort;
        MainWindow _mainWindow;

        private string m_matrixName = "";
        private Color? m_minColor = null;
        private Color? m_maxColor = null;

        public void InitOnServiceStart(DispatcherQueue queue)
        {
            _wpfPort = WpfAdapter.Create(queue);

        }

        public void InitVisual(string matrixName, Color? minColor = null, Color? maxColor = null)
        {
            m_matrixName = matrixName;
            m_minColor = minColor;
            m_maxColor = maxColor;

        }
        public IEnumerator<ITask> StartGui()
        {
            var runWndResponse = _wpfPort.RunWindow(() => new MainWindow());
            yield return (Choice)runWndResponse;

            _mainWindow = (MainWindow)runWndResponse;
            _mainWindow.InitVisual(m_matrixName, m_minColor, m_maxColor);

        }

        public IEnumerator<ITask> ShowMatrix(Matrix m)
        {
            yield return (Choice)_wpfPort.Invoke(() => _mainWindow.ShowMatrix(m));
        }
    }
    
}
