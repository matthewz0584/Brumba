using System.Collections.Generic;
using System.Linq;
using Brumba.Utils;
using Brumba.WaiterStupid;
using MathNet.Numerics.LinearAlgebra.Double;
using Microsoft.Xna.Framework;
using DC = System.Diagnostics.Contracts;

namespace Brumba.DwaNavigator
{
    [DC.ContractClassAttribute(typeof(IVelocityEvaluatorContract))]
    public interface IVelocityEvaluator
    {
        double Evaluate(Velocity v);
    }

    [DC.ContractClassForAttribute(typeof(IVelocityEvaluator))]
    abstract class IVelocityEvaluatorContract : IVelocityEvaluator
    {
        public double Evaluate(Velocity v)
        {
            DC.Contract.Ensures(DC.Contract.Result<double>().BetweenRL(0, 1));

            return default(double);
        }
    }

    public class DwaProblemOptimizer
    {
        readonly double[] _smoothingKernel = { 1, 2, 1, 2, 4, 2, 1, 2, 1 };

        public DwaProblemOptimizer(IVelocitySearchSpaceGenerator velocitySearchSpaceGenerator, IVelocityEvaluator velocityEvaluator)
        {
            DC.Contract.Requires(velocitySearchSpaceGenerator != null);
            DC.Contract.Requires(velocityEvaluator != null);

            VelocitySearchSpaceGenerator = velocitySearchSpaceGenerator;
            VelocityEvaluator = velocityEvaluator;
        }

        public IVelocitySearchSpaceGenerator VelocitySearchSpaceGenerator { get; private set; }
        public IVelocityEvaluator VelocityEvaluator { get; private set; }

        public Vector2 FindOptimalVelocity(Pose velocity)
        {
            DC.Contract.Ensures(DC.Contract.Result<Vector2>().BetweenRL(new Vector2(-1, -1), new Vector2(1)));

            var velocityWheelAcc = VelocitySearchSpaceGenerator.Generate(new Velocity(velocity.Position.Length(), velocity.Bearing));
            var velocityEvals = DenseMatrix.Create(velocityWheelAcc.GetLength(0), velocityWheelAcc.GetLength(1),
                (row, col) => velocityWheelAcc[row, col].Velocity.Linear >= 0 ? VelocityEvaluator.Evaluate(velocityWheelAcc[row, col].Velocity) : -1);
            var maxRowColVal = Smooth(velocityEvals).IndexedEnumerator().OrderByDescending(rowColVal => rowColVal.Item3).First();
            return velocityWheelAcc[maxRowColVal.Item1, maxRowColVal.Item2].WheelAcceleration;
        }

        public DenseMatrix Smooth(DenseMatrix matrix)
        {
            DC.Contract.Requires(matrix != null);
            DC.Contract.Requires(matrix.RowCount >= 3 && matrix.ColumnCount >= 3);
            DC.Contract.Ensures(DC.Contract.Result<DenseMatrix>() != null);
            DC.Contract.Ensures(DC.Contract.Result<DenseMatrix>().RowCount == matrix.RowCount && DC.Contract.Result<DenseMatrix>().ColumnCount == matrix.ColumnCount);

            var smoothed = new DenseMatrix(matrix.RowCount, matrix.ColumnCount);
            smoothed.SetRow(0, matrix.Row(0));
            smoothed.SetRow(smoothed.RowCount - 1, matrix.Row(smoothed.RowCount - 1));
            smoothed.SetColumn(0, matrix.Column(0));
            smoothed.SetColumn(smoothed.ColumnCount - 1, matrix.Column(smoothed.ColumnCount - 1));

            for (var row = 1; row < matrix.RowCount - 1; ++row)
                for (var col = 1; col < matrix.RowCount - 1; ++col)
                {
                    if (matrix[row, col] < 0)
                    {
                        smoothed[row, col] = matrix[row, col];
                        continue;
                    }
                    var area = matrix.SubMatrix(row - 1, 3, col - 1, 3).ToRowWiseArray();
                    var areaWeighted = area.Zip(_smoothingKernel, (m, w) => new { W = m >= 0 ? w : 0, V = m >= 0 ? m * w : 0 });
                    smoothed[row, col] = areaWeighted.Sum(p => p.V) / areaWeighted.Sum(p => p.W);
                }

            return smoothed;
        }
    }
}