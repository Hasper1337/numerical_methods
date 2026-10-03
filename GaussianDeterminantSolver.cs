using System;
using System.Collections.Generic;

namespace GraphPlotter
{
    /// <summary>
    /// Метод Гаусса с постолбцовым выбором главного элемента (частичный выбор
    /// ведущего элемента: на шаге k ищется максимальный по модулю элемент
    /// среди строк k..n-1 в столбце k, его строка переставляется на место k).
    /// Определитель = (±1, по числу перестановок строк) × произведение
    /// диагональных элементов треугольной формы. Собственная реализация,
    /// без использования библиотек линейной алгебры.
    /// </summary>
    public static class GaussianDeterminantSolver
    {
        public class StepLog
        {
            public int Step { get; init; }
            public int PivotRow { get; init; }
            public double PivotValue { get; init; }
            public int SwappedWithRow { get; init; } = -1;
            public double[,] MatrixAfter { get; init; } = new double[0, 0];
        }

        public class Result
        {
            public double Determinant { get; init; }
            public bool Singular { get; init; }
            public int SwapCount { get; init; }
            public double[,] FinalUpperTriangular { get; init; } = new double[0, 0];
            public List<StepLog> Steps { get; init; } = new();
        }

        public static Result Compute(double[,] inputMatrix)
        {
            int n = inputMatrix.GetLength(0);
            var m = (double[,])inputMatrix.Clone();
            int sign = 1;
            int swapCount = 0;
            var steps = new List<StepLog>();
            bool singular = false;

            for (int k = 0; k < n; k++)
            {
                int pivotRow = k;
                double maxAbs = Math.Abs(m[k, k]);
                for (int i = k + 1; i < n; i++)
                {
                    double v = Math.Abs(m[i, k]);
                    if (v > maxAbs) { maxAbs = v; pivotRow = i; }
                }

                int swappedWith = -1;
                if (pivotRow != k)
                {
                    SwapRows(m, k, pivotRow, n);
                    sign = -sign;
                    swapCount++;
                    swappedWith = pivotRow;
                }

                if (Math.Abs(m[k, k]) < 1e-12)
                {
                    singular = true;
                    steps.Add(new StepLog
                    {
                        Step = k + 1,
                        PivotRow = k,
                        PivotValue = m[k, k],
                        SwappedWithRow = swappedWith,
                        MatrixAfter = (double[,])m.Clone()
                    });
                    break;
                }

                for (int i = k + 1; i < n; i++)
                {
                    double factor = m[i, k] / m[k, k];
                    for (int j = k; j < n; j++)
                        m[i, j] -= factor * m[k, j];
                    m[i, k] = 0.0;
                }

                steps.Add(new StepLog
                {
                    Step = k + 1,
                    PivotRow = k,
                    PivotValue = m[k, k],
                    SwappedWithRow = swappedWith,
                    MatrixAfter = (double[,])m.Clone()
                });
            }

            double det;
            if (singular)
            {
                det = 0.0;
            }
            else
            {
                det = sign;
                for (int i = 0; i < n; i++)
                    det *= m[i, i];
            }

            return new Result
            {
                Determinant = det,
                Singular = singular,
                SwapCount = swapCount,
                FinalUpperTriangular = m,
                Steps = steps
            };
        }

        private static void SwapRows(double[,] m, int r1, int r2, int n)
        {
            for (int j = 0; j < n; j++)
                (m[r1, j], m[r2, j]) = (m[r2, j], m[r1, j]);
        }
    }

    /// <summary>
    /// Независимый способ вычисления определителя — разложение по алгебраическим
    /// дополнениям (миноры по первой строке) — используется только для проверки
    /// результата метода Гаусса, как того и требует задание.
    /// </summary>
    public static class DeterminantCofactor
    {
        public static double Compute(double[,] matrix)
        {
            int n = matrix.GetLength(0);
            return ComputeRecursive(matrix, n);
        }

        private static double ComputeRecursive(double[,] a, int n)
        {
            if (n == 1) return a[0, 0];
            if (n == 2) return a[0, 0] * a[1, 1] - a[0, 1] * a[1, 0];

            double det = 0.0;
            int sign = 1;
            for (int col = 0; col < n; col++)
            {
                double[,] minor = Minor(a, 0, col, n);
                det += sign * a[0, col] * ComputeRecursive(minor, n - 1);
                sign = -sign;
            }
            return det;
        }

        private static double[,] Minor(double[,] a, int skipRow, int skipCol, int n)
        {
            var result = new double[n - 1, n - 1];
            int ri = 0;
            for (int i = 0; i < n; i++)
            {
                if (i == skipRow) continue;
                int rj = 0;
                for (int j = 0; j < n; j++)
                {
                    if (j == skipCol) continue;
                    result[ri, rj] = a[i, j];
                    rj++;
                }
                ri++;
            }
            return result;
        }
    }
}
