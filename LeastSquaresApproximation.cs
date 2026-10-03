using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphPlotter
{
    /// <summary>
    /// Наилучшее среднеквадратичное приближение (метод наименьших квадратов).
    /// Полиномиальные варианты строятся через нормальные уравнения
    /// (X^T X c = X^T y), решаемые методом Гаусса (<see cref="LinearAlgebraUtils"/>).
    /// Показательная y=a·e^(b·x) и логарифмическая y=a·ln(x)+b модели сводятся
    /// к линейной регрессии через стандартную линеаризацию (ln по нужной оси).
    /// Все формулы выведены и реализованы самостоятельно, без библиотек
    /// численных методов/статистики.
    /// </summary>
    public static class LeastSquaresApproximation
    {
        public class ModelResult
        {
            public string Name { get; init; } = "";
            public double[] Coefficients { get; init; } = Array.Empty<double>();
            public Func<double, double> Evaluate { get; init; } = _ => double.NaN;
            public double Rmse { get; init; }
            public double SumSquaredError { get; init; }
            public bool Valid { get; init; } = true;
            public string? Note { get; init; }
        }

        /// <summary>Полином степени <paramref name="degree"/>: y = c0 + c1·x + ... + c_degree·x^degree.</summary>
        public static ModelResult FitPolynomial(double[] x, double[] y, int degree, string name)
        {
            int m = degree + 1;
            int n = x.Length;

            // Нормальные уравнения МНК: (X^T X) c = X^T y, X — матрица Вандермонда [1, x, x^2, ...].
            var ata = new double[m, m];
            var aty = new double[m];

            for (int i = 0; i < n; i++)
            {
                var powers = new double[m];
                powers[0] = 1.0;
                for (int p = 1; p < m; p++)
                    powers[p] = powers[p - 1] * x[i];

                for (int r = 0; r < m; r++)
                {
                    aty[r] += powers[r] * y[i];
                    for (int c = 0; c < m; c++)
                        ata[r, c] += powers[r] * powers[c];
                }
            }

            var coeffs = LinearAlgebraUtils.SolveLinearSystem(ata, aty);
            if (coeffs == null)
            {
                return new ModelResult { Name = name, Valid = false, Note = "Система нормальных уравнений вырождена (слишком высокая степень для данных узлов)." };
            }

            double EvalPoly(double xi)
            {
                double acc = 0.0, p = 1.0;
                foreach (double c in coeffs) { acc += c * p; p *= xi; }
                return acc;
            }

            var (sse, rmse) = ComputeError(x, y, EvalPoly);
            return new ModelResult { Name = name, Coefficients = coeffs, Evaluate = EvalPoly, Rmse = rmse, SumSquaredError = sse };
        }

        /// <summary>Показательная модель y = a·e^(b·x). Линеаризация: ln(y) = ln(a) + b·x — требует y&gt;0,
        /// точки с y&lt;=0 исключаются из подбора параметров (но учитываются при оценке ошибки).</summary>
        public static ModelResult FitExponential(double[] x, double[] y)
        {
            var xs = new List<double>();
            var lnYs = new List<double>();
            for (int i = 0; i < x.Length; i++)
            {
                if (y[i] > 0)
                {
                    xs.Add(x[i]);
                    lnYs.Add(Math.Log(y[i]));
                }
            }

            if (xs.Count < 2)
            {
                return new ModelResult { Name = "Показательная y=a·e^(bx)", Valid = false,
                    Note = "Недостаточно точек с y>0 для построения показательной модели." };
            }

            var linFit = FitPolynomial(xs.ToArray(), lnYs.ToArray(), 1, "вспом.лин.");
            if (!linFit.Valid)
            {
                return new ModelResult { Name = "Показательная y=a·e^(bx)", Valid = false, Note = "Не удалось решить линеаризованную систему." };
            }

            double lnA = linFit.Coefficients[0];
            double b = linFit.Coefficients[1];
            double a = Math.Exp(lnA);

            double EvalExp(double xi) => a * Math.Exp(b * xi);
            var (sse, rmse) = ComputeError(x, y, EvalExp);

            string? note = xs.Count < x.Length
                ? $"Из подбора параметров исключено {x.Length - xs.Count} точек с y\u22640 (ln(y) не определён)."
                : null;

            return new ModelResult
            {
                Name = "Показательная y=a\u00b7e^(bx)",
                Coefficients = new[] { a, b },
                Evaluate = EvalExp,
                Rmse = rmse,
                SumSquaredError = sse,
                Note = note
            };
        }

        /// <summary>Логарифмическая модель y = a·ln(x) + b. Требует x&gt;0 для всех точек (линейная регрессия y от ln(x)).</summary>
        public static ModelResult FitLogarithmic(double[] x, double[] y)
        {
            if (x.Any(xi => xi <= 0))
            {
                return new ModelResult { Name = "Логарифмическая y=a\u00b7ln(x)+b", Valid = false,
                    Note = "В данных есть x\u22640 — логарифмическая модель неприменима (нужен положительный промежуток x)." };
            }

            var lnX = x.Select(Math.Log).ToArray();
            var linFit = FitPolynomial(lnX, y, 1, "вспом.лин.");
            if (!linFit.Valid)
            {
                return new ModelResult { Name = "Логарифмическая y=a\u00b7ln(x)+b", Valid = false, Note = "Не удалось решить линеаризованную систему." };
            }

            double b = linFit.Coefficients[0];
            double a = linFit.Coefficients[1];

            double EvalLog(double xi) => a * Math.Log(xi) + b;
            var (sse, rmse) = ComputeError(x, y, EvalLog);

            return new ModelResult
            {
                Name = "Логарифмическая y=a\u00b7ln(x)+b",
                Coefficients = new[] { a, b },
                Evaluate = EvalLog,
                Rmse = rmse,
                SumSquaredError = sse
            };
        }

        private static (double Sse, double Rmse) ComputeError(double[] x, double[] y, Func<double, double> model)
        {
            double sse = 0.0;
            int n = x.Length;
            for (int i = 0; i < n; i++)
            {
                double d = model(x[i]) - y[i];
                sse += d * d;
            }
            return (sse, Math.Sqrt(sse / n));
        }

        /// <summary>Выбирает модель с наименьшей суммой квадратов ошибок среди валидных вариантов.</summary>
        public static ModelResult? PickBest(IEnumerable<ModelResult> models)
        {
            ModelResult? best = null;
            foreach (var m in models)
            {
                if (!m.Valid) continue;
                if (best == null || m.SumSquaredError < best.SumSquaredError)
                    best = m;
            }
            return best;
        }
    }
}
