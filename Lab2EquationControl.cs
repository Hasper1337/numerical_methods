using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace GraphPlotter
{
    /// <summary>
    /// Лабораторная работа №2: уточнение корня нелинейного уравнения f(x)=0
    /// методом парабол. Локализация корня (отрезок [a, b] со сменой знака)
    /// выполняется визуально на графике — том же компоненте, что и в лаб. №1.
    /// </summary>
    public class Lab2EquationControl : UserControl
    {
        private readonly TextBox _txtEquation;
        private readonly Button _btnPlot;
        private readonly TextBox _txtA;
        private readonly TextBox _txtB;
        private readonly TextBox _txtEps;
        private readonly Button _btnSolve;
        private readonly Button _btnFocusRoot;
        private readonly Button _btnResetView;

        private readonly SplitContainer _split;
        private readonly GraphControl _graphControl;
        private readonly ListView _lvIterations;
        private readonly Label _lblResult;

        private readonly StatusStrip _statusStrip;
        private readonly ToolStripStatusLabel _lblCoords;
        private readonly ToolStripStatusLabel _lblScale;
        private readonly ToolStripStatusLabel _lblHint;

        private ExpressionEvaluator? _currentEvaluator;
        private double _lastRoot;
        private double _lastFRoot;
        private double _lastEps = 1e-4;

        public Lab2EquationControl()
        {
            Dock = DockStyle.Fill;
            Font = new Font(FontFamily.GenericSansSerif, 9f);

            var topPanel = new Panel { Dock = DockStyle.Top, Height = 128 };

            var lblEq = new Label { Text = "f(x) =", Location = new Point(8, 10), AutoSize = true };
            _txtEquation = new TextBox
            {
                Location = new Point(58, 6),
                Width = 380,
                Text = "x^2-3-2*cos(2*pi*x)"
            };
            _btnPlot = new Button { Text = "Построить график", Location = new Point(446, 4), Width = 150, Height = 26 };
            _btnPlot.Click += (_, _) => PlotFunction();

            var lblA = new Label { Text = "a =", Location = new Point(8, 40), AutoSize = true };
            _txtA = new TextBox { Location = new Point(34, 36), Width = 90, Text = "1.5" };
            var lblB = new Label { Text = "b =", Location = new Point(136, 40), AutoSize = true };
            _txtB = new TextBox { Location = new Point(162, 36), Width = 90, Text = "2" };
            var lblEps = new Label { Text = "ε =", Location = new Point(264, 40), AutoSize = true };
            _txtEps = new TextBox { Location = new Point(290, 36), Width = 90, Text = "0.0001" };

            _btnSolve = new Button
            {
                Text = "Найти корень (метод парабол)",
                Location = new Point(8, 68),
                Width = 250,
                Height = 28
            };
            _btnSolve.Click += (_, _) => Solve();

            _btnFocusRoot = new Button
            {
                Text = "Показать точность (zoom к корню)",
                Location = new Point(266, 68),
                Width = 250,
                Height = 28,
                Enabled = false
            };
            _btnFocusRoot.Click += (_, _) => FocusOnRoot();

            _btnResetView = new Button
            {
                Text = "Сбросить вид",
                Location = new Point(524, 68),
                Width = 120,
                Height = 28
            };
            _btnResetView.Click += (_, _) => _graphControl.ResetView();

            var hint = new Label
            {
                Text = "Подсказка: постройте график, при необходимости увеличьте масштаб и сместите вид (как в лаб. 1), " +
                       "визуально найдите отрезок [a, b], на концах которого функция меняет знак, и введите его выше.",
                Location = new Point(8, 100),
                AutoSize = false,
                Width = 900,
                Height = 26,
                ForeColor = Color.DimGray
            };

            topPanel.Controls.Add(lblEq);
            topPanel.Controls.Add(_txtEquation);
            topPanel.Controls.Add(_btnPlot);
            topPanel.Controls.Add(lblA);
            topPanel.Controls.Add(_txtA);
            topPanel.Controls.Add(lblB);
            topPanel.Controls.Add(_txtB);
            topPanel.Controls.Add(lblEps);
            topPanel.Controls.Add(_txtEps);
            topPanel.Controls.Add(_btnSolve);
            topPanel.Controls.Add(_btnFocusRoot);
            topPanel.Controls.Add(_btnResetView);
            topPanel.Controls.Add(hint);

            _split = new SplitContainer
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
            _split.Panel1.Controls.Add(_graphControl);

            var resultsPanel = new Panel { Dock = DockStyle.Fill };
            _lvIterations = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };
            _lvIterations.Columns.Add("№", 36);
            _lvIterations.Columns.Add("x0", 85);
            _lvIterations.Columns.Add("x1 (сред.)", 85);
            _lvIterations.Columns.Add("x2", 85);
            _lvIterations.Columns.Add("x_new", 95);
            _lvIterations.Columns.Add("f(x_new)", 110);
            _lvIterations.Columns.Add("|b-a|", 85);

            _lblResult = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 110,
                Text = "Введите отрезок [a, b] и нажмите «Найти корень».",
                Padding = new Padding(6),
                BorderStyle = BorderStyle.FixedSingle
            };

            resultsPanel.Controls.Add(_lvIterations);
            resultsPanel.Controls.Add(_lblResult);
            _split.Panel2.Controls.Add(resultsPanel);

            _statusStrip = new StatusStrip();
            _lblCoords = new ToolStripStatusLabel { Text = "x = —, y = —", BorderSides = ToolStripStatusLabelBorderSides.Right };
            _lblScale = new ToolStripStatusLabel { Text = "Масштаб: 40 пикс./ед." };
            _lblHint = new ToolStripStatusLabel
            {
                Text = "Колесо мыши — масштаб, перетаскивание левой кнопкой мыши — сдвиг",
                Spring = true,
                TextAlign = ContentAlignment.MiddleRight
            };
            _statusStrip.Items.Add(_lblCoords);
            _statusStrip.Items.Add(_lblScale);
            _statusStrip.Items.Add(_lblHint);

            Controls.Add(_split);
            Controls.Add(topPanel);
            Controls.Add(_statusStrip);

            Load += (_, _) =>
            {
                ApplyInitialSplitterDistance();
                PlotFunction();
            };
        }

        private void ApplyInitialSplitterDistance()
        {
            try
            {
                int total = _split.Width;
                if (total < 300) return;
                int desired = (int)(total * 0.62);
                int min = _split.Panel1MinSize;
                int max = total - _split.Panel2MinSize;
                if (max <= min) return;
                _split.SplitterDistance = Math.Max(min, Math.Min(desired, max));
            }
            catch
            {
                // Оставляем распределение по умолчанию, если контрол ещё не готов к размещению.
            }
        }

        /// <summary>Разбирает текущий текст формулы, обновляет функцию на графике,
        /// но НЕ сбрасывает текущий масштаб/положение вида.</summary>
        private bool EnsureEvaluator()
        {
            try
            {
                var evaluator = new ExpressionEvaluator(_txtEquation.Text);
                evaluator.Validate();
                _currentEvaluator = evaluator;
                _graphControl.Function = x => evaluator.Evaluate(x);
                return true;
            }
            catch (ExpressionParseException ex)
            {
                MessageBox.Show(this, ex.Message, "Ошибка в выражении",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Не удалось разобрать выражение: " + ex.Message,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
        }

        private void PlotFunction()
        {
            if (!EnsureEvaluator()) return;

            _graphControl.TablePoints = null;
            _graphControl.MarkerPoint = null;
            _graphControl.ResetView();
            _btnFocusRoot.Enabled = false;
        }

        private void Solve()
        {
            // Пересобираем функцию из текущего текста поля ввода (без сброса вида),
            // чтобы график и вычисления не могли разойтись.
            if (!EnsureEvaluator()) return;

            if (!TryParseDouble(_txtA.Text, out double a) || !TryParseDouble(_txtB.Text, out double b))
            {
                MessageBox.Show(this, "Некорректные границы отрезка a, b.", "Ошибка ввода",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (Math.Abs(a - b) < 1e-12)
            {
                MessageBox.Show(this, "Границы отрезка a и b должны различаться.", "Ошибка ввода",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!TryParseDouble(_txtEps.Text, out double eps) || eps <= 0)
            {
                eps = 1e-4;
                _txtEps.Text = "0.0001";
            }

            var evaluator = _currentEvaluator!;
            double Fx(double x) => evaluator.Evaluate(x);

            var result = ParabolaMethodSolver.Solve(Fx, a, b, eps);

            _lvIterations.Items.Clear();
            foreach (var step in result.History)
            {
                var item = new ListViewItem(step.Index.ToString(CultureInfo.InvariantCulture));
                item.SubItems.Add(Fmt(step.X0));
                item.SubItems.Add(Fmt(step.X1));
                item.SubItems.Add(Fmt(step.X2));
                item.SubItems.Add(Fmt(step.XNew));
                item.SubItems.Add(step.FNew.ToString("0.######e+0", CultureInfo.InvariantCulture));
                item.SubItems.Add(Fmt(step.IntervalWidth));
                _lvIterations.Items.Add(item);
            }

            if (!result.Converged)
            {
                _lblResult.ForeColor = Color.Firebrick;
                _lblResult.Text = "Не удалось найти корень.\n" + (result.Error ?? "Неизвестная ошибка.");
                _graphControl.MarkerPoint = null;
                _btnFocusRoot.Enabled = false;
                return;
            }

            _lblResult.ForeColor = Color.Black;
            _lblResult.Text =
                $"Корень: x* = {result.Root.ToString("0.000000", CultureInfo.InvariantCulture)}\n" +
                $"f(x*) = {result.FRoot.ToString("0.######e+0", CultureInfo.InvariantCulture)}\n" +
                $"Число итераций: {result.Iterations}\n" +
                $"Точность ε = {eps.ToString("0.######", CultureInfo.InvariantCulture)} достигнута.";

            _graphControl.MarkerPoint = (result.Root, result.FRoot);
            _btnFocusRoot.Enabled = true;

            _lastRoot = result.Root;
            _lastFRoot = result.FRoot;
            _lastEps = eps;
        }

        private void FocusOnRoot()
        {
            double desiredPixelsForEps = Math.Max(200.0, Width * 0.3);
            double scale = desiredPixelsForEps / _lastEps;
            _graphControl.CenterAndZoom(_lastRoot, _lastFRoot, scale);
        }

        private static string Fmt(double v) => v.ToString("0.000000", CultureInfo.InvariantCulture);

        private static bool TryParseDouble(string s, out double v)
        {
            s = s.Trim().Replace(",", ".");
            return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v);
        }
    }
}
