using System;
using System.Globalization;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace GraphPlotter
{
    /// <summary>
    /// Лабораторная работа №5: решение той же СЛАУ, что и в лаб. №4,
    /// методом релаксаций (SOR). Точность оценивается по максимальной
    /// по модулю разнице компонент решения между соседними итерациями.
    /// </summary>
    public class Lab5RelaxationControl : UserControl
    {
        private static readonly double[,] DefaultA =
        {
            { 53.56,  0.91,  -1.28,  -2.74 },
            { -8.02, -35.43,  4.15,   7.39 },
            {  1.73,  -4.22, -39.51, -0.52 },
            {  6.47,   2.55,  -3.88, -45.19 }
        };

        private static readonly double[] DefaultB = { 6.84, -1.67, 3.96, -0.41 };

        private readonly DataGridView _grid;
        private readonly TextBox _txtOmega;
        private readonly TextBox _txtEps;
        private readonly Button _btnSolve;
        private readonly Button _btnResetDefaults;
        private readonly ListView _lvIterations;
        private readonly Label _lblResult;
        private readonly TextBox _txtOutput;

        public Lab5RelaxationControl()
        {
            Dock = DockStyle.Fill;
            Font = new Font(FontFamily.GenericSansSerif, 9f);

            var topPanel = new Panel { Dock = DockStyle.Top, Height = 230 };

            var lblTask = new Label
            {
                Text = "Та же СЛАУ, что и в лаб. 4 (Ax = b). Метод релаксаций (SOR): x_i := (1-ω)x_i + ω·x_i(Зейдель).\n" +
                       "ω = 1 — метод Зейделя. Точность — по макс. |Δx_i| между итерациями.",
                Location = new Point(8, 6),
                AutoSize = false,
                Width = 760,
                Height = 34,
                ForeColor = Color.DimGray
            };

            _grid = new DataGridView
            {
                Location = new Point(8, 44),
                Width = 430,
                Height = 118,
                RowHeadersVisible = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                SelectionMode = DataGridViewSelectionMode.CellSelect
            };
            _grid.Columns.Add("x1", "x1");
            _grid.Columns.Add("x2", "x2");
            _grid.Columns.Add("x3", "x3");
            _grid.Columns.Add("x4", "x4");
            _grid.Columns.Add("b", "= b");
            foreach (DataGridViewColumn col in _grid.Columns)
            {
                col.Width = 78;
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            _grid.Rows.Add(4);
            for (int i = 0; i < 4; i++)
                _grid.Rows[i].HeaderCell.Value = $"Уравнение {i + 1}";

            ResetGridToDefaults();

            var lblOmega = new Label { Text = "ω =", Location = new Point(450, 48), AutoSize = true };
            _txtOmega = new TextBox { Location = new Point(478, 44), Width = 70, Text = "1.0" };
            var lblEps = new Label { Text = "ε =", Location = new Point(450, 78), AutoSize = true };
            _txtEps = new TextBox { Location = new Point(478, 74), Width = 70, Text = "0.0001" };

            _btnSolve = new Button
            {
                Text = "Решить (метод релаксаций)",
                Location = new Point(450, 110),
                Width = 220,
                Height = 30
            };
            _btnSolve.Click += (_, _) => Solve();

            _btnResetDefaults = new Button
            {
                Text = "Сбросить к значениям по умолчанию",
                Location = new Point(450, 146),
                Width = 220,
                Height = 30
            };
            _btnResetDefaults.Click += (_, _) => ResetGridToDefaults();

            _lblResult = new Label
            {
                Location = new Point(8, 170),
                Width = 900,
                Height = 56,
                ForeColor = Color.DimGray,
                Text = "Введите A, b, ω, ε (или оставьте значения по умолчанию) и нажмите «Решить»."
            };

            topPanel.Controls.Add(lblTask);
            topPanel.Controls.Add(_grid);
            topPanel.Controls.Add(lblOmega);
            topPanel.Controls.Add(_txtOmega);
            topPanel.Controls.Add(lblEps);
            topPanel.Controls.Add(_txtEps);
            topPanel.Controls.Add(_btnSolve);
            topPanel.Controls.Add(_btnResetDefaults);
            topPanel.Controls.Add(_lblResult);

            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                Panel1MinSize = 50,
                Panel2MinSize = 50
            };

            _lvIterations = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };
            _lvIterations.Columns.Add("№", 40);
            _lvIterations.Columns.Add("x1", 90);
            _lvIterations.Columns.Add("x2", 90);
            _lvIterations.Columns.Add("x3", 90);
            _lvIterations.Columns.Add("x4", 90);
            _lvIterations.Columns.Add("max|Δx|", 100);
            split.Panel1.Controls.Add(_lvIterations);

            _txtOutput = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Font = new Font(FontFamily.GenericMonospace, 9.5f),
                Text = "Здесь появится решение и невязка."
            };
            split.Panel2.Controls.Add(_txtOutput);

            Controls.Add(split);
            Controls.Add(topPanel);

            Load += (_, _) => ApplyInitialSplitterDistance(split);
        }

        private static void ApplyInitialSplitterDistance(SplitContainer split)
        {
            try
            {
                int total = split.Width;
                if (total < 300) return;
                int desired = (int)(total * 0.55);
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

        private void ResetGridToDefaults()
        {
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                    _grid.Rows[i].Cells[j].Value = DefaultA[i, j].ToString("0.##", CultureInfo.InvariantCulture);
                _grid.Rows[i].Cells[4].Value = DefaultB[i].ToString("0.##", CultureInfo.InvariantCulture);
            }
        }

        private bool TryReadSystem(out double[,] a, out double[] b)
        {
            a = new double[4, 4];
            b = new double[4];
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    string raw = Convert.ToString(_grid.Rows[i].Cells[j].Value, CultureInfo.InvariantCulture) ?? "";
                    if (!TryParseDouble(raw, out double value))
                    {
                        MessageBox.Show(this, $"Некорректное значение в строке {i + 1}, столбце x{j + 1}: '{raw}'.",
                            "Ошибка ввода", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }
                    a[i, j] = value;
                }

                string rawB = Convert.ToString(_grid.Rows[i].Cells[4].Value, CultureInfo.InvariantCulture) ?? "";
                if (!TryParseDouble(rawB, out double bValue))
                {
                    MessageBox.Show(this, $"Некорректное значение b в строке {i + 1}: '{rawB}'.",
                        "Ошибка ввода", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
                b[i] = bValue;
            }
            return true;
        }

        private void Solve()
        {
            if (!TryReadSystem(out double[,] a, out double[] b))
                return;

            if (!TryParseDouble(_txtOmega.Text, out double omega) || omega <= 0 || omega >= 2)
            {
                MessageBox.Show(this, "Параметр ω должен быть в интервале (0, 2). Рекомендуемое значение — 1.0.",
                    "Ошибка ввода", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!TryParseDouble(_txtEps.Text, out double eps) || eps <= 0)
            {
                eps = 1e-4;
                _txtEps.Text = "0.0001";
            }

            var result = RelaxationMethodSolver.Solve(a, b, omega, eps);

            _lvIterations.Items.Clear();
            foreach (var step in result.History)
            {
                var item = new ListViewItem(step.Index.ToString(CultureInfo.InvariantCulture));
                foreach (double xi in step.X)
                    item.SubItems.Add(xi.ToString("0.000000", CultureInfo.InvariantCulture));
                item.SubItems.Add(step.MaxDelta.ToString("0.######e+0", CultureInfo.InvariantCulture));
                _lvIterations.Items.Add(item);
            }

            if (!result.Converged)
            {
                _lblResult.ForeColor = Color.Firebrick;
                _lblResult.Text = "Не удалось найти решение.\n" + (result.Error ?? "Неизвестная ошибка.");
                _txtOutput.Text = _lblResult.Text;
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("Решение (столбец-вектор x):");
            for (int i = 0; i < result.X.Length; i++)
                sb.AppendLine($"  x{i + 1} = {result.X[i].ToString("0.000000", CultureInfo.InvariantCulture)}");
            sb.AppendLine();
            sb.AppendLine("Невязка r = A·x − b:");
            for (int i = 0; i < result.Residual.Length; i++)
                sb.AppendLine($"  r{i + 1} = {result.Residual[i].ToString("0.######e+0", CultureInfo.InvariantCulture)}");
            sb.AppendLine();
            sb.AppendLine($"max|r_i| = {result.ResidualNorm.ToString("0.######e+0", CultureInfo.InvariantCulture)}");
            sb.AppendLine($"Число итераций: {result.Iterations}");
            sb.AppendLine($"ω = {omega.ToString("0.###", CultureInfo.InvariantCulture)}, " +
                           $"ε = {eps.ToString("0.######", CultureInfo.InvariantCulture)}");
            sb.AppendLine("Точность ε по максимальному |Δx_i| между итерациями достигнута.");

            _txtOutput.Text = sb.ToString();

            _lblResult.ForeColor = Color.DarkGreen;
            var shortSummary = new StringBuilder();
            for (int i = 0; i < result.X.Length; i++)
                shortSummary.Append($"x{i + 1}={result.X[i].ToString("0.0000", CultureInfo.InvariantCulture)}  ");
            _lblResult.Text =
                $"{shortSummary}\u2713\n" +
                $"Итераций: {result.Iterations}, max|невязка| = {result.ResidualNorm.ToString("0.0e+0", CultureInfo.InvariantCulture)}";
        }

        private static bool TryParseDouble(string s, out double v)
        {
            s = (s ?? "").Trim().Replace(",", ".");
            return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v);
        }
    }
}
