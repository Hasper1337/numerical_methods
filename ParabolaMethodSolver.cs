using System;
using System.Collections.Generic;

namespace GraphPlotter
{
    /// <summary>
    /// Метод парабол (квадратичная интерполяция с сохранением локализации корня).
    /// На каждой итерации через три точки x0, x1=(x0+x2)/2, x2 проводится
    /// интерполяционная парабола (по формуле Ньютона с разделёнными разностями),
    /// находится её корень, лежащий внутри [x0, x2], и по знаку функции в нём
    /// сужается отрезок локализации — аналогично методу половинного деления,
    /// но со значительно более быстрой (сверхлинейной) сходимостью.
    /// </summary>
    public static class ParabolaMethodSolver
    {
        /// <summary>Один шаг итерационного процесса — для протокола решения.</summary>
        public readonly struct IterationStep
        {
            public int Index { get; }
            public double X0 { get; }
            public double X1 { get; }
            public double X2 { get; }
            public double XNew { get; }
            public double FNew { get; }
            public double IntervalWidth { get; }

            public IterationStep(int index, double x0, double x1, double x2, double xNew, double fNew, double intervalWidth)
            {
                Index = index;
                X0 = x0;
                X1 = x1;
                X2 = x2;
                XNew = xNew;
                FNew = fNew;
                IntervalWidth = intervalWidth;
            }
        }

        public class Result
        {
            public double Root { get; init; }
            public double FRoot { get; init; }
            public int Iterations { get; init; }
            public bool Converged { get; init; }
            public string? Error { get; init; }
            public List<IterationStep> History { get; init; } = new();
        }

        /// <summary>
        /// Уточняет корень уравнения f(x)=0 на отрезке [a, b], на концах которого
        /// функция должна иметь разные знаки (отрезок локализации подбирается
        /// пользователем визуально по графику — см. Лабораторную работу №1).
        /// </summary>
        public static Result Solve(Func<double, double> f, double a, double b, double eps, int maxIterations = 200)
        {
            if (eps <= 0) eps = 1e-4;
            if (a > b) (a, b) = (b, a);

            double x0 = a, x2 = b;
            double f0 = SafeEval(f, x0);
            double f2 = SafeEval(f, x2);

            if (double.IsNaN(f0) || double.IsNaN(f2))
            {
                return new Result { Error = "Функция не определена на одном из концов отрезка.", Converged = false };
            }
            if (Math.Abs(f0) < eps)
                return new Result { Root = x0, FRoot = f0, Iterations = 0, Converged = true };
            if (Math.Abs(f2) < eps)
                return new Result { Root = x2, FRoot = f2, Iterations = 0, Converged = true };
            if (f0 * f2 > 0)
            {
                return new Result
                {
                    Error = "На концах отрезка функция имеет одинаковый знак — отрезок не локализует корень. " +
                            "Подберите границы по графику так, чтобы f(a) и f(b) имели разные знаки.",
                    Converged = false
                };
            }

            var history = new List<IterationStep>();
            double xNew = double.NaN, fNew = double.NaN;
            int iter;

            for (iter = 1; iter <= maxIterations; iter++)
            {
                double x1 = (x0 + x2) / 2.0;
                double f1 = SafeEval(f, x1);

                xNew = SolveQuadraticNode(x0, f0, x1, f1, x2, f2);
                fNew = SafeEval(f, xNew);

                double width = x2 - x0;
                history.Add(new IterationStep(iter, x0, x1, x2, xNew, fNew, width));

                if (double.IsNaN(fNew))
                {
                    return new Result { Error = "Функция не определена в одной из промежуточных точек.", Converged = false, History = history, Iterations = iter };
                }

                if (Math.Abs(fNew) < eps || width < eps)
                {
                    return new Result { Root = xNew, FRoot = fNew, Iterations = iter, Converged = true, History = history };
                }

                if (f0 * fNew < 0)
                {
                    x2 = xNew; f2 = fNew;
                }
                else
                {
                    x0 = xNew; f0 = fNew;
                }
            }

            return new Result
            {
                Root = xNew,
                FRoot = fNew,
                Iterations = maxIterations,
                Converged = false,
                History = history,
                Error = $"Достигнуто максимальное число итераций ({maxIterations}) без выхода на заданную точность."
            };
        }

        /// <summary>
        /// Строит параболу (полином Ньютона 2-й степени) через три узла и
        /// возвращает её корень, лежащий внутри [x0, x2]. При вырождении
        /// (коэффициент при x^2 близок к нулю или дискриминант отрицателен)
        /// откатывается к формуле секущей по крайним точкам x0, x2.
        /// </summary>
        private static double SolveQuadraticNode(double x0, double f0, double x1, double f1, double x2, double f2)
        {
            double d01 = (f1 - f0) / (x1 - x0);
            double d12 = (f2 - f1) / (x2 - x1);
            double A = (d12 - d01) / (x2 - x0);
            double B = d01 - A * (x0 + x1);
            double C = f0 - d01 * x0 + A * x0 * x1;

            double secantFallback = x0 - f0 * (x2 - x0) / (f2 - f0);

            if (Math.Abs(A) < 1e-14)
                return secantFallback;

            double discriminant = B * B - 4 * A * C;
            if (discriminant < 0)
                return secantFallback;

            double sqrtD = Math.Sqrt(discriminant);
            double r1 = (-B + sqrtD) / (2 * A);
            double r2 = (-B - sqrtD) / (2 * A);

            bool r1Inside = r1 >= x0 && r1 <= x2;
            bool r2Inside = r2 >= x0 && r2 <= x2;

            if (r1Inside && r2Inside)
                return Math.Abs(r1 - x1) < Math.Abs(r2 - x1) ? r1 : r2;
            if (r1Inside) return r1;
            if (r2Inside) return r2;
            return secantFallback;
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
