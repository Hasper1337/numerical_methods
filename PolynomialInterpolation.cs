using System;

namespace GraphPlotter
{
    /// <summary>
    /// Интерполяционный многочлен в барицентрической форме формулы Лагранжа.
    /// Математически — тот же единственный многочлен степени n, что проходит
    /// через n+1 узел, что и классическая форма Лагранжа, но вычисляется
    /// значительно устойчивее (без вычитания близких больших чисел) —
    /// это важно, так как задание требует строить многочлены степеней
    /// вплоть до 50. Реализация полностью собственная.
    /// </summary>
    public class PolynomialInterpolation
    {
        private readonly double[] _x;
        private readonly double[] _y;
        private readonly double[] _weights;

        public int Degree => _x.Length - 1;

        public PolynomialInterpolation(double[] xNodes, double[] yNodes)
        {
            if (xNodes.Length != yNodes.Length || xNodes.Length < 2)
                throw new ArgumentException("Нужно как минимум 2 узла, количество x и y должно совпадать.");

            _x = (double[])xNodes.Clone();
            _y = (double[])yNodes.Clone();
            _weights = ComputeBarycentricWeights(_x);
        }

        private static double[] ComputeBarycentricWeights(double[] x)
        {
            int n = x.Length;
            var w = new double[n];
            for (int j = 0; j < n; j++)
            {
                double prod = 1.0;
                for (int k = 0; k < n; k++)
                {
                    if (k == j) continue;
                    prod *= (x[j] - x[k]);
                }
                w[j] = 1.0 / prod;
            }
            return w;
        }

        /// <summary>Вычисляет значение интерполяционного многочлена в точке x.</summary>
        public double Evaluate(double x)
        {
            double numerator = 0.0;
            double denominator = 0.0;

            for (int j = 0; j < _x.Length; j++)
            {
                double diff = x - _x[j];
                if (Math.Abs(diff) < 1e-12)
                    return _y[j]; // точное попадание в узел — избегаем деления на ноль

                double term = _weights[j] / diff;
                numerator += term * _y[j];
                denominator += term;
            }

            return numerator / denominator;
        }
    }
}
