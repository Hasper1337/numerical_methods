using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace GraphPlotter
{
    /// <summary>
    /// Лаб. 6, часть 2: кусочно-линейная интерполяция. Число узлов может быть
    /// произвольным, в том числе большим (10^4–10^5) — поиск отрезка при
    /// вычислении значения выполняется бинарным поиском (см. <see cref="PiecewiseLinearInterpolation"/>).
    /// </summary>
    public class Lab6Sub2PiecewiseControl : UserControl
    {
        private const int MaxPointsToDrawAsMarkers = 500;

        private readonly TextBox _txtEquation;
        private readonly TextBox _txtA;
        private readonly TextBox _txtB;
        private readonly NumericUpDown _nudPointCount;
        private readonly TextBox _txtPoint;
        private readonly Button _btnBuild;
        private readonly Button _btnResetView;

        private readonly NumericUpDown _nudSegmentIndex;
        private readonly Button _btnShowSegment;
        private readonly Label _lblSegmentInfo;
        private readonly Label _lblResult;

        private readonly GraphControl _graphControl;
        private readonly StatusStrip _statusStrip;
        private readonly ToolStripStatusLabel _lblCoords;
        private readonly ToolStripStatusLabel _lblScale;

        private PiecewiseLinearInterpolation? _interp;

        public Lab6Sub2PiecewiseControl()
        {
            Dock = DockStyle.Fill;
            Font = new Font(FontFamily.GenericSansSerif, 9f);

            var topPanel = new Panel { Dock = DockStyle.Top, Height = 140 };

            var lblEq = new Label { Text = "f(x) =", Location = new Point(8, 10), AutoSize = true };
            _txtEquation = new TextBox { Location = new Point(58, 6), Width = 300, Text = "x^2-3-2*cos(2*pi*x)" };

            var lblA = new Label { Text = "a =", Location = new Point(368, 10), AutoSize = true };
            _txtA = new TextBox { Location = new Point(394, 6), Width = 60, Text = "-3" };
            var lblB = new Label { Text = "b =", Location = new Point(462, 10), AutoSize = true };
            _txtB = new TextBox { Location = new Point(488, 6), Width = 60, Text = "3" };

            var lblN = new Label { Text = "Точек:", Location = new Point(8, 42), AutoSize = true };
            _nudPointCount = new NumericUpDown
            {
                Location = new Point(58, 38),
                Width = 90,
                Minimum = 2,
                Maximum = 200000,
                Value = 20,
                ThousandsSeparator = true
            };

            var lblPoint = new Label { Text = "Точка x0 =", Location = new Point(160, 42), AutoSize = true };
            _txtPoint = new TextBox { Location = new Point(236, 38), Width = 70, Text = "0.5" };

            _btnBuild = new Button { Text = "Построить", Location = new Point(320, 36), Width = 110, Height = 26 };
            _btnBuild.Click += (_, _) => Build();

            _btnResetView = new Button { Text = "Сбросить вид", Location = new Point(440, 36), Width = 110, Height = 26 };
            _btnResetView.Click += (_, _) => _graphControl.ResetView();

            var lblSeg = new Label { Text = "Просмотр отрезка №:", Location = new Point(8, 74), AutoSize = true };
            _nudSegmentIndex = new NumericUpDown { Location = new Point(150, 70), Width = 90, Minimum = 0, Maximum = 0 };
            _nudSegmentIndex.ValueChanged += (_, _) => UpdateSegmentInfo();
            _btnShowSegment = new Button { Text = "Показать на графике", Location = new Point(250, 68), Width = 160, Height = 26 };
            _btnShowSegment.Click += (_, _) => ShowSegment();

            _lblSegmentInfo = new Label
            {
                Location = new Point(8, 100),
                Width = 900,
                Height = 18,
                ForeColor = Color.DimGray,
                Text = "Сначала постройте кусочно-линейную функцию."
            };

            var hint = new Label
            {
                Text = "Синий — исходная функция, оранжевый — кусочно-линейная интерполяция, зелёные точки — узлы, звёздочка — значение в x0.",
                Location = new Point(8, 120),
                AutoSize = false,
                Width = 900,
                Height = 18,
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
            topPanel.Controls.Add(lblPoint);
            topPanel.Controls.Add(_txtPoint);
            topPanel.Controls.Add(_btnBuild);
            topPanel.Controls.Add(_btnResetView);
            topPanel.Controls.Add(lblSeg);
            topPanel.Controls.Add(_nudSegmentIndex);
            topPanel.Controls.Add(_btnShowSegment);
            topPanel.Controls.Add(_lblSegmentInfo);
            topPanel.Controls.Add(hint);

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

            int n = (int)_nudPointCount.Value;
            var xs = new double[n];
            var ys = new double[n];
            for (int i = 0; i < n; i++)
            {
                double xi = n == 1 ? a : a + (b - a) * i / (n - 1);
                xs[i] = xi;
                ys[i] = evaluator.Evaluate(xi);
            }

            _interp = new PiecewiseLinearInterpolation(xs, ys);
            double valAtX0 = _interp.Evaluate(x0);

            _graphControl.Function = x => evaluator.Evaluate(x);
            _graphControl.ExtraCurves.Clear();
            _graphControl.ExtraCurves.Add((_interp.Evaluate, Color.FromArgb(230, 140, 20)));
            _graphControl.TablePoints = null;
            _graphControl.ExtraPointSets.Clear();
            if (n <= MaxPointsToDrawAsMarkers)
            {
                var pts = new List<(double X, double Y)>(n);
                for (int i = 0; i < n; i++) pts.Add((xs[i], ys[i]));
                _graphControl.ExtraPointSets.Add((pts, Color.FromArgb(30, 150, 60), 3f));
            }
            _graphControl.MarkerPoint = null;
            _graphControl.StarMarker = (x0, valAtX0, Color.Purple);
            _graphControl.ResetView();

            _nudSegmentIndex.Maximum = Math.Max(0, _interp.Segments.Length - 1);
            _nudSegmentIndex.Value = 0;
            UpdateSegmentInfo();

            string pointsNote = n > MaxPointsToDrawAsMarkers
                ? $" (узлы не отображаются по отдельности — их {n}, показана сама функция)"
                : "";
            _lblResult.Text = $"Узлов: {n}{pointsNote}.   " +
                               $"S({x0.ToString("0.####", CultureInfo.InvariantCulture)}) = {valAtX0.ToString("0.000000", CultureInfo.InvariantCulture)},   " +
                               $"f({x0.ToString("0.####", CultureInfo.InvariantCulture)}) = {evaluator.Evaluate(x0).ToString("0.000000", CultureInfo.InvariantCulture)}";
        }

        private void UpdateSegmentInfo()
        {
            if (_interp == null || _interp.Segments.Length == 0)
            {
                _lblSegmentInfo.Text = "Сначала постройте кусочно-линейную функцию.";
                return;
            }

            int idx = (int)_nudSegmentIndex.Value;
            if (idx < 0 || idx >= _interp.Segments.Length) return;

            var seg = _interp.Segments[idx];
            _lblSegmentInfo.Text =
                $"Отрезок {idx}: x \u2208 [{seg.X0.ToString("0.000000", CultureInfo.InvariantCulture)}, " +
                $"{seg.X1.ToString("0.000000", CultureInfo.InvariantCulture)}]   " +
                $"y = {seg.K.ToString("0.000000", CultureInfo.InvariantCulture)}\u00b7x " +
                $"{(seg.B >= 0 ? "+" : "\u2212")} {Math.Abs(seg.B).ToString("0.000000", CultureInfo.InvariantCulture)}";
        }

        private void ShowSegment()
        {
            UpdateSegmentInfo();
            if (_interp == null || _interp.Segments.Length == 0) return;

            int idx = (int)_nudSegmentIndex.Value;
            if (idx < 0 || idx >= _interp.Segments.Length) return;

            var seg = _interp.Segments[idx];
            double cx = (seg.X0 + seg.X1) / 2.0;
            double cy = (seg.Y0 + seg.Y1) / 2.0;
            double span = Math.Max(seg.X1 - seg.X0, 1e-6);
            double desiredPixelsForSpan = Math.Max(300.0, Width * 0.5);
            double scale = desiredPixelsForSpan / (span * 3.0);
            _graphControl.CenterAndZoom(cx, cy, scale);
        }

        private static bool TryParseDouble(string s, out double v)
        {
            s = (s ?? "").Trim().Replace(",", ".");
            return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v);
        }
    }
}
