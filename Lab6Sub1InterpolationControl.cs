using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace GraphPlotter
{
    /// <summary>
    /// Лаб. 6, часть 1: интерполяция многочленом (барицентрическая форма
    /// Лагранжа), степень от 1 до 50. Узлы — равномерно расположенные точки
    /// на отрезке [a, b], количество узлов = степень + 1 (как и требуется —
    /// согласовано с числом параметров многочлена).
    /// </summary>
    public class Lab6Sub1InterpolationControl : UserControl
    {
        private readonly TextBox _txtEquation;
        private readonly TextBox _txtA;
        private readonly TextBox _txtB;
        private readonly NumericUpDown _nudDegree;
        private readonly TextBox _txtPoint;
        private readonly Button _btnBuild;
        private readonly Button _btnResetView;
        private readonly Label _lblResult;

        private readonly GraphControl _graphControl;
        private readonly StatusStrip _statusStrip;
        private readonly ToolStripStatusLabel _lblCoords;
        private readonly ToolStripStatusLabel _lblScale;

        public Lab6Sub1InterpolationControl()
        {
            Dock = DockStyle.Fill;
            Font = new Font(FontFamily.GenericSansSerif, 9f);

            var topPanel = new Panel { Dock = DockStyle.Top, Height = 110 };

            var lblEq = new Label { Text = "f(x) =", Location = new Point(8, 10), AutoSize = true };
            _txtEquation = new TextBox { Location = new Point(58, 6), Width = 300, Text = "x^2-3-2*cos(2*pi*x)" };

            var lblA = new Label { Text = "a =", Location = new Point(368, 10), AutoSize = true };
            _txtA = new TextBox { Location = new Point(394, 6), Width = 60, Text = "-3" };
            var lblB = new Label { Text = "b =", Location = new Point(462, 10), AutoSize = true };
            _txtB = new TextBox { Location = new Point(488, 6), Width = 60, Text = "3" };

            var lblDeg = new Label { Text = "Степень:", Location = new Point(8, 42), AutoSize = true };
            _nudDegree = new NumericUpDown
            {
                Location = new Point(68, 38),
                Width = 60,
                Minimum = 1,
                Maximum = 50,
                Value = 5
            };

            var lblPoint = new Label { Text = "Точка x0 =", Location = new Point(150, 42), AutoSize = true };
            _txtPoint = new TextBox { Location = new Point(226, 38), Width = 70, Text = "0.5" };

            _btnBuild = new Button { Text = "Построить", Location = new Point(320, 36), Width = 110, Height = 26 };
            _btnBuild.Click += (_, _) => Build();

            _btnResetView = new Button { Text = "Сбросить вид", Location = new Point(440, 36), Width = 110, Height = 26 };
            _btnResetView.Click += (_, _) => _graphControl.ResetView();

            var hint = new Label
            {
                Text = "Синий — исходная функция, оранжевый — интерполяционный многочлен, зелёные точки — узлы, звёздочка — значение в x0. " +
                       "При больших степенях на равномерной сетке возможны сильные колебания многочлена (явление Рунге) — это ожидаемо.",
                Location = new Point(8, 68),
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
            topPanel.Controls.Add(lblDeg);
            topPanel.Controls.Add(_nudDegree);
            topPanel.Controls.Add(lblPoint);
            topPanel.Controls.Add(_txtPoint);
            topPanel.Controls.Add(_btnBuild);
            topPanel.Controls.Add(_btnResetView);
            topPanel.Controls.Add(hint);

            _graphControl = new GraphControl { Dock = DockStyle.Fill };
            _graphControl.MouseWorldMove += (_, world) =>
            {
                _lblCoords!.Text = $"x = {world.X.ToString("0.####", CultureInfo.InvariantCulture)}, " +
                                    $"y = {world.Y.ToString("0.####", CultureInfo.InvariantCulture)}";
            };
            _graphControl.ViewChanged += (_, _) =>
            {
                _lblScale!.Text = $"Масштаб: {_graphControl.Scale.ToString("0.####", CultureInfo.InvariantCulture)} пикс./ед.";
            };

            _lblResult = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 30,
                Text = "Нажмите «Построить».",
                Padding = new Padding(6)
            };

            _statusStrip = new StatusStrip();
            _lblCoords = new ToolStripStatusLabel { Text = "x = —, y = —", BorderSides = ToolStripStatusLabelBorderSides.Right };
            _lblScale = new ToolStripStatusLabel { Text = "Масштаб: 40 пикс./ед." };
            _statusStrip.Items.Add(_lblCoords);
            _statusStrip.Items.Add(_lblScale);

            Controls.Add(_graphControl);
            Controls.Add(_lblResult);
            Controls.Add(topPanel);
            Controls.Add(_statusStrip);

            Load += (_, _) => Build();
        }

        private void Build()
        {
            if (!TryParseDouble(_txtA.Text, out double a) || !TryParseDouble(_txtB.Text, out double b) || Math.Abs(a - b) < 1e-9)
            {
                MessageBox.Show(this, "Некорректный отрезок [a, b].", "Ошибка ввода", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (a > b) (a, b) = (b, a);

            if (!TryParseDouble(_txtPoint.Text, out double x0))
            {
                MessageBox.Show(this, "Некорректная точка x0.", "Ошибка ввода", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

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

            int degree = (int)_nudDegree.Value;
            int nodeCount = degree + 1;

            var xs = new double[nodeCount];
            var ys = new double[nodeCount];
            var nodePoints = new List<(double X, double Y)>(nodeCount);
            for (int i = 0; i < nodeCount; i++)
            {
                double xi = nodeCount == 1 ? a : a + (b - a) * i / (nodeCount - 1);
                double yi = evaluator.Evaluate(xi);
                xs[i] = xi; ys[i] = yi;
                nodePoints.Add((xi, yi));
            }

            PolynomialInterpolation poly;
            try
            {
                poly = new PolynomialInterpolation(xs, ys);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Не удалось построить многочлен: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            double pAtX0 = poly.Evaluate(x0);

            _graphControl.Function = x => evaluator.Evaluate(x);
            _graphControl.ExtraCurves.Clear();
            _graphControl.ExtraCurves.Add((poly.Evaluate, Color.FromArgb(230, 140, 20)));
            _graphControl.TablePoints = null;
            _graphControl.ExtraPointSets.Clear();
            _graphControl.ExtraPointSets.Add((nodePoints, Color.FromArgb(30, 150, 60), 3.5f));
            _graphControl.MarkerPoint = null;
            _graphControl.StarMarker = (x0, pAtX0, Color.Purple);
            _graphControl.ResetView();

            _lblResult.Text = $"Степень {degree}, узлов: {nodeCount}.   " +
                               $"P({x0.ToString("0.####", CultureInfo.InvariantCulture)}) = {pAtX0.ToString("0.000000", CultureInfo.InvariantCulture)},   " +
                               $"f({x0.ToString("0.####", CultureInfo.InvariantCulture)}) = {evaluator.Evaluate(x0).ToString("0.000000", CultureInfo.InvariantCulture)}";
        }

        private static bool TryParseDouble(string s, out double v)
        {
            s = (s ?? "").Trim().Replace(",", ".");
            return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v);
        }
    }
}
