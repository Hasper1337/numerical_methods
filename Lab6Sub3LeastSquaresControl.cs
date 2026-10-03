using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Windows.Forms;

namespace GraphPlotter
{
    /// <summary>
    /// Лаб. 6, часть 3, вариант 5: наилучшее среднеквадратичное приближение —
    /// многочлены 4-й и 5-й степени, показательная и логарифмическая функции,
    /// с выбором наилучшего варианта по минимуму суммы квадратов ошибок.
    /// Исходные данные — значения f(x) из таблицы со случайной погрешностью
    /// 30–300% в каждой точке. Промежуток [a,b] по умолчанию взят строго
    /// положительным (и по x, и по f(x)) — это нужно для показательной и
    /// особенно для логарифмической модели (ln(x) определён только при x&gt;0).
    /// </summary>
    public class Lab6Sub3LeastSquaresControl : UserControl
    {
        private static readonly Color PolyColor4 = Color.FromArgb(30, 90, 200);
        private static readonly Color PolyColor5 = Color.FromArgb(230, 140, 20);
        private static readonly Color ExpColor = Color.FromArgb(30, 160, 70);
        private static readonly Color LogColor = Color.FromArgb(160, 40, 160);
        private static readonly Color TrueFnColor = Color.FromArgb(150, 150, 150);

        private readonly TextBox _txtEquation;
        private readonly TextBox _txtA;
        private readonly TextBox _txtB;
        private readonly NumericUpDown _nudPointCount;
        private readonly TextBox _txtErrMin;
        private readonly TextBox _txtErrMax;
        private readonly Button _btnGenerate;
        private readonly Button _btnResetView;

        private readonly GraphControl _graphControl;
        private readonly TextBox _txtOutput;

        private readonly StatusStrip _statusStrip;
        private readonly ToolStripStatusLabel _lblCoords;
        private readonly ToolStripStatusLabel _lblScale;

        private readonly Random _random = new();

        public Lab6Sub3LeastSquaresControl()
        {
            Dock = DockStyle.Fill;
            Font = new Font(FontFamily.GenericSansSerif, 9f);

            var topPanel = new Panel { Dock = DockStyle.Top, Height = 118 };

            var lblEq = new Label { Text = "f(x) =", Location = new Point(8, 8), AutoSize = true };
            _txtEquation = new TextBox { Location = new Point(58, 4), Width = 280, Text = "x^2-3-2*cos(2*pi*x)" };

            var lblA = new Label { Text = "a =", Location = new Point(348, 8), AutoSize = true };
            _txtA = new TextBox { Location = new Point(374, 4), Width = 60, Text = "2.5" };
            var lblB = new Label { Text = "b =", Location = new Point(442, 8), AutoSize = true };
            _txtB = new TextBox { Location = new Point(468, 4), Width = 60, Text = "5" };

            var lblN = new Label { Text = "Точек:", Location = new Point(540, 8), AutoSize = true };
            _nudPointCount = new NumericUpDown { Location = new Point(590, 4), Width = 60, Minimum = 20, Maximum = 50, Value = 30 };

            var lblErr = new Label { Text = "Погрешность, %:", Location = new Point(8, 40), AutoSize = true };
            _txtErrMin = new TextBox { Location = new Point(118, 36), Width = 50, Text = "30" };
            var lblErrDash = new Label { Text = "…", Location = new Point(172, 40), AutoSize = true };
            _txtErrMax = new TextBox { Location = new Point(188, 36), Width = 50, Text = "300" };

            _btnGenerate = new Button
            {
                Text = "Сгенерировать точки и построить приближения",
                Location = new Point(250, 34),
                Width = 320,
                Height = 28
            };
            _btnGenerate.Click += (_, _) => GenerateAndFit();

            _btnResetView = new Button { Text = "Сбросить вид", Location = new Point(580, 34), Width = 110, Height = 28 };
            _btnResetView.Click += (_, _) => _graphControl.ResetView();

            var hint = new Label
            {
                Text = "Отрезок [a,b] должен быть положительным (нужно для показательной и логарифмической моделей). " +
                       "Серый — исходная f(x), точки — зашумлённые данные; цвета моделей — в таблице результатов справа.",
                Location = new Point(8, 70),
                AutoSize = false,
                Width = 900,
                Height = 32,
                ForeColor = Color.DimGray
            };

            topPanel.Controls.Add(lblEq);
            topPanel.Controls.Add(_txtEquation);
            topPanel.Controls.Add(lblA);
            topPanel.Controls.Add(_txtA);
            topPanel.Controls.Add(lblB);
            topPanel.Controls.Add(_txtB);
            topPanel.Controls.Add(lblN);
            topPanel.Controls.Add(_nudPointCount);
            topPanel.Controls.Add(lblErr);
            topPanel.Controls.Add(_txtErrMin);
            topPanel.Controls.Add(lblErrDash);
            topPanel.Controls.Add(_txtErrMax);
            topPanel.Controls.Add(_btnGenerate);
            topPanel.Controls.Add(_btnResetView);
            topPanel.Controls.Add(hint);

            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                Panel1MinSize = 50,
                Panel2MinSize = 50
            };

            _graphControl = new GraphControl { Dock = DockStyle.Fill };
            _graphControl.MouseWorldMove += (_, world) =>
            {
                _lblCoords.Text = $"x = {world.X.ToString("0.####", CultureInfo.InvariantCulture)}, " +
                                   $"y = {world.Y.ToString("0.####", CultureInfo.InvariantCulture)}";
            };
            _graphControl.ViewChanged += (_, _) =>
            {
                _lblScale.Text = $"Масштаб: {_graphControl.Scale.ToString("0.####", CultureInfo.InvariantCulture)} пикс./ед.";
            };
            split.Panel1.Controls.Add(_graphControl);

            _txtOutput = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Font = new Font(FontFamily.GenericMonospace, 9.5f),
                Text = "Нажмите «Сгенерировать точки и построить приближения»."
            };
            split.Panel2.Controls.Add(_txtOutput);

            _statusStrip = new StatusStrip();
            _lblCoords = new ToolStripStatusLabel { Text = "x = —, y = —", BorderSides = ToolStripStatusLabelBorderSides.Right };
            _lblScale = new ToolStripStatusLabel { Text = "Масштаб: 40 пикс./ед." };
            _statusStrip.Items.Add(_lblCoords);
            _statusStrip.Items.Add(_lblScale);

            Controls.Add(split);
            Controls.Add(topPanel);
            Controls.Add(_statusStrip);

            Load += (_, _) =>
            {
                ApplyInitialSplitterDistance(split);
                GenerateAndFit();
            };
        }

        private static void ApplyInitialSplitterDistance(SplitContainer split)
        {
            try
            {
                int total = split.Width;
                if (total < 300) return;
                int desired = (int)(total * 0.6);
                int min = split.Panel1MinSize;
                int max = total - split.Panel2MinSize;
                if (max <= min) return;
                split.SplitterDistance = Math.Max(min, Math.Min(desired, max));
            }
            catch
            {
                // Оставляем распределение по умолчанию, если контрол ещё не готов к размещению.
            }
        }

        private void GenerateAndFit()
        {
            if (!TryParseDouble(_txtA.Text, out double a) || !TryParseDouble(_txtB.Text, out double b) || Math.Abs(a - b) < 1e-9)
            {
                MessageBox.Show(this, "Некорректный отрезок [a, b].", "Ошибка ввода", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (a > b) (a, b) = (b, a);
            if (a <= 0)
            {
                MessageBox.Show(this, "Отрезок должен быть положительным (a > 0) — это нужно для логарифмической модели.",
                    "Ошибка ввода", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!TryParseDouble(_txtErrMin.Text, out double errMin) || errMin < 0) errMin = 30;
            if (!TryParseDouble(_txtErrMax.Text, out double errMax) || errMax < errMin) errMax = Math.Max(errMin, 300);

            ExpressionEvaluator evaluator;
            try
            {
                evaluator = new ExpressionEvaluator(_txtEquation.Text);
                evaluator.Validate();
            }
            catch (ExpressionParseException ex)
            {
                MessageBox.Show(this, ex.Message, "Ошибка в выражении", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int n = (int)_nudPointCount.Value;
            var xs = new double[n];
            var ysTrue = new double[n];
            var ysNoisy = new double[n];

            for (int i = 0; i < n; i++)
            {
                double xi = a + (b - a) * i / (n - 1);
                double trueVal = evaluator.Evaluate(xi);

                double pct = (errMin + _random.NextDouble() * (errMax - errMin)) / 100.0;
                double sign = _random.NextDouble() < 0.5 ? -1.0 : 1.0;
                double noisyVal = trueVal * (1.0 + sign * pct);

                xs[i] = xi;
                ysTrue[i] = trueVal;
                ysNoisy[i] = noisyVal;
            }

            var poly4 = LeastSquaresApproximation.FitPolynomial(xs, ysNoisy, 4, "Полином 4-й степени");
            var poly5 = LeastSquaresApproximation.FitPolynomial(xs, ysNoisy, 5, "Полином 5-й степени");
            var exp = LeastSquaresApproximation.FitExponential(xs, ysNoisy);
            var log = LeastSquaresApproximation.FitLogarithmic(xs, ysNoisy);

            var models = new[] { poly4, poly5, exp, log };
            var best = LeastSquaresApproximation.PickBest(models);

            var dataPoints = new List<(double X, double Y)>(n);
            for (int i = 0; i < n; i++) dataPoints.Add((xs[i], ysNoisy[i]));

            _graphControl.Function = null; // «истинная» функция рисуется ниже серым через ExtraCurves
            _graphControl.ExtraCurves.Clear();
            _graphControl.ExtraCurves.Add((x => evaluator.Evaluate(x), TrueFnColor));
            if (poly4.Valid) _graphControl.ExtraCurves.Add((poly4.Evaluate, PolyColor4));
            if (poly5.Valid) _graphControl.ExtraCurves.Add((poly5.Evaluate, PolyColor5));
            if (exp.Valid) _graphControl.ExtraCurves.Add((exp.Evaluate, ExpColor));
            if (log.Valid) _graphControl.ExtraCurves.Add((log.Evaluate, LogColor));
            _graphControl.TablePoints = null;
            _graphControl.ExtraPointSets.Clear();
            _graphControl.ExtraPointSets.Add((dataPoints, Color.Black, 3f));
            _graphControl.MarkerPoint = null;
            _graphControl.StarMarker = null;
            _graphControl.FitToPoints(dataPoints);

            _txtOutput.Text = BuildReport(models, best, errMin, errMax, n);
        }

        private static string BuildReport(LeastSquaresApproximation.ModelResult[] models,
            LeastSquaresApproximation.ModelResult? best, double errMin, double errMax, int n)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Точек: {n}, погрешность: {errMin:0.#}%..{errMax:0.#}% (случайный знак в каждой точке).");
            sb.AppendLine("Цвета на графике: серый = f(x), синий = полином 4, оранжевый = полином 5, " +
                           "зелёный = показательная, фиолетовый = логарифмическая, чёрные точки = данные.");
            sb.AppendLine();

            foreach (var m in models)
            {
                sb.AppendLine($"--- {m.Name} ---");
                if (!m.Valid)
                {
                    sb.AppendLine("  Не построена: " + (m.Note ?? "ошибка"));
                    sb.AppendLine();
                    continue;
                }

                sb.Append("  Коэффициенты: ");
                sb.AppendLine(FormatCoefficients(m));
                sb.AppendLine($"  Сумма квадратов ошибок (SSE) = {m.SumSquaredError.ToString("0.######e+0", CultureInfo.InvariantCulture)}");
                sb.AppendLine($"  Среднеквадратичная ошибка (RMSE) = {m.Rmse.ToString("0.######", CultureInfo.InvariantCulture)}");
                if (m.Note != null) sb.AppendLine("  " + m.Note);
                sb.AppendLine();
            }

            sb.AppendLine("=== Вывод ===");
            if (best == null)
            {
                sb.AppendLine("Ни один вариант не удалось построить — проверьте отрезок [a, b] и уравнение.");
            }
            else
            {
                sb.AppendLine($"Наилучшее приближение (минимальная SSE): {best.Name}, " +
                               $"RMSE = {best.Rmse.ToString("0.######", CultureInfo.InvariantCulture)}.");
                sb.AppendLine("При таком уровне случайной погрешности (30–300%) данные сильно зашумлены, " +
                               "поэтому у всех моделей RMSE заметно больше нуля; наилучший вариант — тот, чья форма " +
                               "кривой ближе всего воспроизводит форму f(x) на выбранном отрезке при имеющемся разбросе точек.");
            }

            return sb.ToString();
        }

        private static string FormatCoefficients(LeastSquaresApproximation.ModelResult m)
        {
            if (m.Name.StartsWith("Полином"))
            {
                var parts = new List<string>();
                for (int i = 0; i < m.Coefficients.Length; i++)
                    parts.Add($"c{i}={m.Coefficients[i].ToString("0.####e+0", CultureInfo.InvariantCulture)}");
                return string.Join(", ", parts);
            }
            if (m.Coefficients.Length == 2)
            {
                return $"a={m.Coefficients[0].ToString("0.####e+0", CultureInfo.InvariantCulture)}, " +
                       $"b={m.Coefficients[1].ToString("0.####e+0", CultureInfo.InvariantCulture)}";
            }
            return string.Join(", ", Array.ConvertAll(m.Coefficients, c => c.ToString("0.####e+0", CultureInfo.InvariantCulture)));
        }

        private static bool TryParseDouble(string s, out double v)
        {
            s = (s ?? "").Trim().Replace(",", ".");
            return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v);
        }
    }
}
