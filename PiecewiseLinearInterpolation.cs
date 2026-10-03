using System;

namespace GraphPlotter
{
    /// <summary>
    /// Кусочно-линейная интерполяция: на каждом отрезке [x_i, x_{i+1}] строится
    /// прямая y = k_i·x + b_i, проходящая через соседние табличные точки.
    /// Для больших таблиц (10^4–10^5 точек) поиск нужного отрезка выполняется
    /// бинарным поиском — O(log n) на вычисление значения. Реализация
    /// полностью собственная.
    /// </summary>
    public class PiecewiseLinearInterpolation
    {
        public readonly struct Segment
        {
            public int Index { get; }
            public double X0 { get; }
            public double X1 { get; }
            public double Y0 { get; }
            public double Y1 { get; }
            public double K { get; }
            public double B { get; }

            public Segment(int index, double x0, double x1, double y0, double y1)
            {
                Index = index;
                X0 = x0; X1 = x1; Y0 = y0; Y1 = y1;
                K = (y1 - y0) / (x1 - x0);
                B = y0 - K * x0;
            }
        }

        private readonly double[] _x;
        private readonly double[] _y;
        public Segment[] Segments { get; }

        public int PointCount => _x.Length;

        public PiecewiseLinearInterpolation(double[] xNodes, double[] yNodes)
        {
            if (xNodes.Length != yNodes.Length || xNodes.Length < 2)
                throw new ArgumentException("Нужно как минимум 2 узла, количество x и y должно совпадать.");

            _x = xNodes;
            _y = yNodes;

            Segments = new Segment[_x.Length - 1];
            for (int i = 0; i < Segments.Length; i++)
                Segments[i] = new Segment(i, _x[i], _x[i + 1], _y[i], _y[i + 1]);
        }

        /// <summary>Находит индекс отрезка, содержащего x (бинарный поиск), с учётом краёв диапазона.</summary>
        public int FindSegmentIndex(double x)
        {
            int n = _x.Length;
            if (x <= _x[0]) return 0;
            if (x >= _x[n - 1]) return n - 2;

            int lo = 0, hi = n - 1;
            while (hi - lo > 1)
            {
                int mid = (lo + hi) / 2;
                if (_x[mid] <= x) lo = mid; else hi = mid;
            }
            return lo;
        }

        /// <summary>Вычисляет значение кусочно-линейной функции в точке x (экстраполирует за краями по крайним отрезкам).</summary>
        public double Evaluate(double x)
        {
            var seg = Segments[FindSegmentIndex(x)];
            return seg.K * x + seg.B;
        }
    }
}
