using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Media;
using Brumba.DsspUtils;
using MathNet.Numerics.LinearAlgebra.Double;
using Microsoft.Ccr.Adapters.Wpf;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using W3C.Soap;
using Matrix = MathNet.Numerics.LinearAlgebra.Double.Matrix;

namespace Brumba.WaiterStupid.GUI
{
   

    /// <summary>
    /// ONLY AN EXAMPLE OF USAGE
    /// </summary>
    [Contract(Contract.Identifier)]
    [DisplayName("Matrix vizualizer")]
    [Description("Brumba Matrix vizualizer")]
    public class MatrixVizualizerService : DsspServiceExposing
    {
        private Random m_rnd = new Random(1);

        [ServicePort("/Vizualizer", AllowMultipleInstances = true)]
        VizOperations _mainPort = new VizOperations();
        

        MatrixVizualizerServiceHelper mv = new MatrixVizualizerServiceHelper();
        MatrixVizualizerServiceHelper mv2 = new MatrixVizualizerServiceHelper();
        
        readonly TimerFacade _timerFacade;

        public MatrixVizualizerService(DsspServiceCreationPort creationPort)
            : base(creationPort)
        {
            _timerFacade = new TimerFacade(this, 5f);
        }

        
        protected override void Start()
        {
            mv.InitOnServiceStart(TaskQueue);
            mv2.InitOnServiceStart(TaskQueue);
            SpawnIterator(StartIt);
            base.Start();
        }

        IEnumerator<ITask> StartIt()
        {
            mv.InitVisual("FirstMatrix", Colors.Yellow, Colors.Chocolate);
            mv2.InitVisual("SecondMatrix");
            yield return To.Exec(() => mv.StartGui());
            yield return To.Exec(() => mv2.StartGui());

            MainPortInterleave.CombineWith(new Interleave(new ExclusiveReceiverGroup(), new ConcurrentReceiverGroup(
                    Arbiter.ReceiveWithIterator(true, _timerFacade.TickPort, ShowSmth))));

            yield return To.Exec(() => _timerFacade.Set());
            
        }

        IEnumerator<ITask> ShowSmth(TimeSpan dt)
        {
            var matrix = CreateMatrix();
            yield return To.Exec(()=>mv.ShowMatrix(matrix));
            yield return To.Exec(() => mv2.ShowMatrix(matrix));
        }

        private Matrix CreateMatrix()
        {
            var intRnd = new Random();
            var matrix = new DenseMatrix(intRnd.Next(10,77));
            
            for (var i = 0; i < matrix.RowCount; i++)
            {
                for (var j = 0; j < matrix.ColumnCount; j++)
                {
                    matrix[i, j] = m_rnd.NextDouble();
                }
            }
            return matrix;
        }
    }

    [DataContract]
    public class MatrixVizualizerState
    {
    }

    public sealed class Contract
    {
        [DataMember]
        public const string Identifier = "http://brumba.ru/contracts/2013/11/matrixvizualizerservice.html";
    }


    [ServicePort]
    public class VizOperations : PortSet<DsspDefaultLookup, DsspDefaultDrop, Get>
    {
    }

    public class Get : Get<GetRequestType, PortSet<MatrixVizualizerState, Fault>>
    {
        public Get()
        {
        }

        public Get(GetRequestType body)
            : base(body)
        {
        }

        public Get(GetRequestType body, PortSet<MatrixVizualizerState, Fault> responsePort)
            : base(body, responsePort)
        {
        }
    }
}
