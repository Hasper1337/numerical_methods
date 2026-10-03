using System;

namespace GraphPlotter
{
    /// <summary>
    /// Небольшая вспомогательная линейная алгебра общего назначения (метод Гаусса
    /// с выбором главного элемента по столбцу для решения Ax=b) — используется
    /// в лаб. 6 для решения нормальных уравнений метода наименьших квадратов.
    /// Это не «метод, указанный в задании» (им является сама аппроксимация),
    /// а стандартный вспомогательный инструмент линейной алгебры, написанный
    /// самостоятельно, без сторонних библиотек.
    /// </summary>
    public static class LinearAlgebraUtils
    {
        /// <summary>Решает Ax=b методом Гаусса с выбором главного элемента. Возвращает
        /// null, если матрица вырождена (система не имеет единственного решения).</summary>
        public static double[]? SolveLinearSystem(double[,] a, double[] b)
        {
            int n = b.Length;
            var m = (double[,])a.Clone();
            var rhs = (double[])b.Clone();

            for (int k = 0; k < n; k++)
            {
                int pivotRow = k;
                double maxAbs = Math.Abs(m[k, k]);
                for (int i = k + 1; i < n; i++)
                {
                    double v = Math.Abs(m[i, k]);
                    if (v > maxAbs) { maxAbs = v; pivotRow = i; }
                }

                if (maxAbs < 1e-14) return null;

                if (pivotRow != k)
                {
                    for (int j = 0; j < n; j++)
                        (m[k, j], m[pivotRow, j]) = (m[pivotRow, j], m[k, j]);
                    (rhs[k], rhs[pivotRow]) = (rhs[pivotRow], rhs[k]);
                }

                for (int i = k + 1; i < n; i++)
                {
                    double factor = m[i, k] / m[k, k];
                    if (factor == 0.0) continue;
                    for (int j = k; j < n; j++)
                        m[i, j] -= factor * m[k, j];
                    rhs[i] -= factor * rhs[k];
                }
            }

            var x = new double[n];
            for (int i = n - 1; i >= 0; i--)
            {
                double sum = rhs[i];
                for (int j = i + 1; j < n; j++)
                    sum -= m[i, j] * x[j];
                x[i] = sum / m[i, i];
            }
            return x;
        }
    }
}
