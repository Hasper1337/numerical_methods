using System;
using System.Collections.Generic;

namespace GraphPlotter
{
    /// <summary>
    /// Метод парабол для поиска экстремума унимодальной на отрезке [a, b] функции.
    /// На каждой итерации через три точки x0 &lt; x1 &lt; x2 проводится парабола,
    /// вычисляется абсцисса её вершины, и по значению функции в ней тройка точек
    /// сужается, сохраняя унимодальность. Реализация полностью собственная —
    /// без использования библиотек оптимизации.
    /// </summary>
    public static class ExtremumMethodSolver
    {
        public enum ExtremumKind { Minimum, Maximum }

        public readonly struct IterationStep
        {
            public int Index { get; }
            public double X0 { get; }
            public double X1 { get; }
            public double X2 { get; }
            public double XNew { get; }
            public double FNew { get; }
            public double Width { get; }

            public IterationStep(int index, double x0, double x1, double x2, double xNew, double fNew, double width)
            {
                Index = index;
                X0 = x0; X1 = x1; X2 = x2;
                XNew = xNew; FNew = fNew; Width = width;
            }
        }

        public class Result
        {
            public double X { get; init; }
            public double F { get; init; }
            public double FirstDerivative { get; init; }
            public double SecondDerivative { get; init; }
            public ExtremumKind Kind { get; init; }
            public int Iterations { get; init; }
            public bool Converged { get; init; }
            public string? Error { get; init; }
            public List<IterationStep> History { get; init; } = new();
        }

        /// <summary>
        /// Уточняет экстремум функции f на отрезке унимодальности [a, b]
        /// (отрезок подбирается пользователем визуально по графику — см. лаб. №1).
        /// </summary>
        public static Result Solve(Func<double, double> f, double a, double b, double eps, ExtremumKind kind, int maxIterations = 200)
        {
            if (eps <= 0) eps = 1e-4;
            if (a > b) (a, b) = (b, a);

            // Для поиска максимума ищем минимум функции g(x) = -f(x).
            double Sign = kind == ExtremumKind.Maximum ? -1.0 : 1.0;
            double G(double x) => Sign * SafeEval(f, x);

            double x0 = a, x2 = b, x1 = (a + b) / 2.0;
            double f0 = G(x0), f1 = G(x1), f2 = G(x2);

            if (double.IsNaN(f0) || double.IsNaN(f1) || double.IsNaN(f2))
            {
                return new Result { Error = "Функция не определена на отрезке или в его середине.", Converged = false };
            }

            if (!(f1 <= f0 && f1 <= f2))
            {
                string wantedKind = kind == ExtremumKind.Maximum ? "максимума" : "минимума";
                return new Result
                {
                    Error = $"Условие унимодальности не выполнено для поиска {wantedKind}: значение в середине отрезка " +
                            "должно быть не хуже значений на его концах. Подберите отрезок [a, b] по графику заново.",
                    Converged = false
                };
            }

            var history = new List<IterationStep>();
            double xNew = x1, fNew = f1;
            int iter;

            for (iter = 1; iter <= maxIterations; iter++)
            {
                double num = (x1 - x0) * (x1 - x0) * (f1 - f2) - (x1 - x2) * (x1 - x2) * (f1 - f0);
                double den = (x1 - x0) * (f1 - f2) - (x1 - x2) * (f1 - f0);

                if (Math.Abs(den) < 1e-14)
                {
                    xNew = (x0 + x2) / 2.0;
                }
                else
                {
                    xNew = x1 - 0.5 * num / den;
                    if (xNew <= x0 || xNew >= x2 || double.IsNaN(xNew))
                        xNew = (x0 + x2) / 2.0;
                }

                fNew = G(xNew);
                double width = x2 - x0;
                history.Add(new IterationStep(iter, x0, x1, x2, xNew, Sign * fNew, width));

                if (double.IsNaN(fNew))
                {
                    return new Result { Error = "Функция не определена в одной из промежуточных точек.", Converged = false, History = history, Iterations = iter };
                }

                if (Math.Abs(xNew - x1) < eps || width < eps)
                {
                    return BuildResult(f, xNew, kind, iter, history, converged: true, error: null);
                }

                // Из четырёх точек {x0,x1,x2,xNew} формируем новую тройку,
                // сохраняя окно из трёх соседних (по x) точек, содержащее наименьшее значение g.
                var pts = new (double X, double F)[] { (x0, f0), (x1, f1), (x2, f2), (xNew, fNew) };
                Array.Sort(pts, (p, q) => p.X.CompareTo(q.X));

                int minIdx = 0;
                for (int i = 1; i < pts.Length; i++)
                    if (pts[i].F < pts[minIdx].F) minIdx = i;

                int lo = minIdx <= 1 ? 0 : 1;
                x0 = pts[lo].X; f0 = pts[lo].F;
                x1 = pts[lo + 1].X; f1 = pts[lo + 1].F;
                x2 = pts[lo + 2].X; f2 = pts[lo + 2].F;
            }

            return BuildResult(f, xNew, kind, maxIterations, history, converged: false,
                error: $"Достигнуто максимальное число итераций ({maxIterations}) без выхода на заданную точность.");
        }

        private static Result BuildResult(Func<double, double> f, double x, ExtremumKind kind, int iterations,
            List<IterationStep> history, bool converged, string? error)
        {
            double fx = SafeEval(f, x);
            double h = Math.Max(1e-5, Math.Abs(x) * 1e-5);
            double fPlus = SafeEval(f, x + h);
            double fMinus = SafeEval(f, x - h);

            double first = (fPlus - fMinus) / (2 * h);
            double second = (fPlus - 2 * fx + fMinus) / (h * h);

            return new Result
            {
                X = x,
                F = fx,
                FirstDerivative = first,
                SecondDerivative = second,
                Kind = kind,
                Iterations = iterations,
                Converged = converged,
                Error = error,
                History = history
            };
        }

        private static double SafeEval(Func<double, double> f, double x)
        {
            try
            {
                double y = f(x);
                return double.IsInfinity(y) ? double.NaN : y;
            }
            catch
            {
                return double.NaN;
            }
        }
    }
}
