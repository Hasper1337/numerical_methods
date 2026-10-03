using System;
using System.Collections.Generic;

namespace GraphPlotter
{
    /// <summary>
    /// Метод релаксаций (SOR — Successive Over-Relaxation) для решения СЛАУ Ax=b.
    /// На каждой итерации компоненты пересчитываются по формуле
    /// x_i := (1-ω)·x_i + ω/a_ii·(b_i − Σ_{j&lt;i} a_ij·x_j^new − Σ_{j&gt;i} a_ij·x_j^old),
    /// то есть уже обновлённые в текущей итерации компоненты используются сразу
    /// (как в методе Зейделя), а множитель ω ускоряет/замедляет сходимость.
    /// При ω = 1 метод в точности совпадает с методом Зейделя. Точность
    /// оценивается по максимальной по модулю разнице компонент между
    /// соседними итерациями. Собственная реализация, без библиотек линейной алгебры.
    /// </summary>
    public static class RelaxationMethodSolver
    {
        public class IterationStep
        {
            public int Index { get; init; }
            public double[] X { get; init; } = Array.Empty<double>();
            public double MaxDelta { get; init; }
        }

        public class Result
        {
            public double[] X { get; init; } = Array.Empty<double>();
            public double[] Residual { get; init; } = Array.Empty<double>();
            public double ResidualNorm { get; init; }
            public int Iterations { get; init; }
            public bool Converged { get; init; }
            public string? Error { get; init; }
            public List<IterationStep> History { get; init; } = new();
        }

        public static Result Solve(double[,] a, double[] b, double omega, double eps, int maxIterations = 10000)
        {
            int n = b.Length;

            for (int i = 0; i < n; i++)
            {
                if (Math.Abs(a[i, i]) < 1e-14)
                {
                    return new Result
                    {
                        Error = $"Диагональный элемент a[{i + 1},{i + 1}] практически равен нулю — " +
                                "метод релаксаций в таком виде неприменим (нужна перестановка строк).",
                        Converged = false
                    };
                }
            }

            double[] x = new double[n];
            var history = new List<IterationStep>();

            int iter;
            for (iter = 1; iter <= maxIterations; iter++)
            {
                double maxDelta = 0.0;

                for (int i = 0; i < n; i++)
                {
                    double sum = 0.0;
                    for (int j = 0; j < n; j++)
                        if (j != i) sum += a[i, j] * x[j];

                    double xGaussSeidel = (b[i] - sum) / a[i, i];
                    double xNewI = (1.0 - omega) * x[i] + omega * xGaussSeidel;

                    double delta = Math.Abs(xNewI - x[i]);
                    if (delta > maxDelta) maxDelta = delta;

                    x[i] = xNewI;
                }

                history.Add(new IterationStep { Index = iter, X = (double[])x.Clone(), MaxDelta = maxDelta });

                if (double.IsNaN(maxDelta) || double.IsInfinity(maxDelta) || maxDelta > 1e12)
                {
                    return BuildResult(a, b, x, iter, history, converged: false,
                        error: "Метод расходится при выбранном ω. Попробуйте ω = 1 (метод Зейделя) " +
                               "или уменьшите ω (0 < ω < 2).");
                }

                if (maxDelta < eps)
                {
                    return BuildResult(a, b, x, iter, history, converged: true, error: null);
                }
            }

            return BuildResult(a, b, x, maxIterations, history, converged: false,
                error: $"Достигнуто максимальное число итераций ({maxIterations}) без выхода на заданную точность.");
        }

        private static Result BuildResult(double[,] a, double[] b, double[] x, int iterations,
            List<IterationStep> history, bool converged, string? error)
        {
            int n = b.Length;
            var residual = new double[n];
            double maxRes = 0.0;
            for (int i = 0; i < n; i++)
            {
                double axi = 0.0;
                for (int j = 0; j < n; j++)
                    axi += a[i, j] * x[j];
                residual[i] = axi - b[i];
                maxRes = Math.Max(maxRes, Math.Abs(residual[i]));
            }

            return new Result
            {
                X = x,
                Residual = residual,
                ResidualNorm = maxRes,
                Iterations = iterations,
                Converged = converged,
                Error = error,
                History = history
            };
        }
    }
}
