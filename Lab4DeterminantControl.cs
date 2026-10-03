using System;
using System.Globalization;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace GraphPlotter
{
    /// <summary>
    /// Лабораторная работа №4, вариант 1: вычисление определителя матрицы
    /// методом Гаусса с постолбцовым выбором главного элемента для заданной
    /// СЛАУ 4x4. Ввод — через графический интерфейс со значениями по умолчанию,
    /// взятыми из задания. Проверка — определитель, вычисленный независимым
    /// способом (разложение по алгебраическим дополнениям).
    /// </summary>
    public class Lab4DeterminantControl : UserControl
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
        private readonly Button _btnCompute;
        private readonly Button _btnResetDefaults;
        private readonly TextBox _txtOutput;
        private readonly Label _lblSummary;

        public Lab4DeterminantControl()
        {
            Dock = DockStyle.Fill;
            Font = new Font(FontFamily.GenericSansSerif, 9f);

            var topPanel = new Panel { Dock = DockStyle.Top, Height = 230 };

            var lblTask = new Label
            {
                Text = "Вариант 1: определитель матрицы A методом Гаусса с постолбцовым выбором главного элемента.\n" +
                       "Столбец b показан только для справки (это СЛАУ из задания) и в вычислении определителя не участвует.",
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
            _grid.Columns.Add("b", "= b (справочно)");
            foreach (DataGridViewColumn col in _grid.Columns)
            {
                col.Width = 78;
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            _grid.Columns["b"].DefaultCellStyle.BackColor = Color.WhiteSmoke;
            _grid.Columns["b"].DefaultCellStyle.ForeColor = Color.DimGray;
            _grid.Rows.Add(4);
            for (int i = 0; i < 4; i++)
                _grid.Rows[i].HeaderCell.Value = $"Уравнение {i + 1}";

            ResetGridToDefaults();

            _btnCompute = new Button
            {
                Text = "Вычислить определитель",
                Location = new Point(450, 44),
                Width = 220,
                Height = 30
            };
            _btnCompute.Click += (_, _) => Compute();

            _btnResetDefaults = new Button
            {
                Text = "Сбросить к значениям по умолчанию",
                Location = new Point(450, 82),
                Width = 220,
                Height = 30
            };
            _btnResetDefaults.Click += (_, _) => ResetGridToDefaults();

            _lblSummary = new Label
            {
                Location = new Point(450, 122),
                Width = 300,
                Height = 90,
                ForeColor = Color.DimGray,
                Text = "Введите матрицу (или оставьте значения по умолчанию) и нажмите «Вычислить определитель»."
            };

            topPanel.Controls.Add(lblTask);
            topPanel.Controls.Add(_grid);
            topPanel.Controls.Add(_btnCompute);
            topPanel.Controls.Add(_btnResetDefaults);
            topPanel.Controls.Add(_lblSummary);

            _txtOutput = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Font = new Font(FontFamily.GenericMonospace, 9.5f),
                Text = "Здесь появится ход метода Гаусса и итоговая проверка."
            };

            Controls.Add(_txtOutput);
            Controls.Add(topPanel);
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

        private bool TryReadMatrix(out double[,] matrix)
        {
            matrix = new double[4, 4];
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    string raw = Convert.ToString(_grid.Rows[i].Cells[j].Value, CultureInfo.InvariantCulture) ?? "";
                    if (!TryParseDouble(raw, out double value))
                    {
                        MessageBox.Show(this,
                            $"Некорректное значение в строке {i + 1}, столбце x{j + 1}: '{raw}'.",
                            "Ошибка ввода", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }
                    matrix[i, j] = value;
                }
            }
            return true;
        }

        private void Compute()
        {
            if (!TryReadMatrix(out double[,] a))
                return;

            var result = GaussianDeterminantSolver.Compute(a);
            double checkDet = DeterminantCofactor.Compute(a);
            double discrepancy = Math.Abs(result.Determinant - checkDet);

            var sb = new StringBuilder();
            sb.AppendLine("Исходная матрица A:");
            sb.AppendLine(FormatMatrix(a));
            sb.AppendLine();
            sb.AppendLine("Ход метода Гаусса (постолбцовый выбор главного элемента):");
            sb.AppendLine();

            foreach (var step in result.Steps)
            {
                sb.Append($"Шаг {step.Step}: ведущий элемент — строка {step.PivotRow + 1}");
                if (step.SwappedWithRow >= 0)
                    sb.Append($" (переставлена со строкой {step.SwappedWithRow + 1})");
                sb.AppendLine($", значение = {step.PivotValue.ToString("0.000000", CultureInfo.InvariantCulture)}");

                if (result.Singular && step == result.Steps[^1])
                {
                    sb.AppendLine("  Ведущий элемент практически равен нулю — матрица вырождена, определитель = 0.");
                }
                else
                {
                    sb.AppendLine("  Матрица после исключения:");
                    sb.AppendLine(Indent(FormatMatrix(step.MatrixAfter)));
                }
                sb.AppendLine();
            }

            if (!result.Singular)
            {
                sb.AppendLine("Итоговая верхнетреугольная матрица:");
                sb.AppendLine(FormatMatrix(result.FinalUpperTriangular));
                sb.AppendLine();
                sb.AppendLine($"Число перестановок строк: {result.SwapCount} " +
                               $"(знак множителя: {(result.SwapCount % 2 == 0 ? "+1" : "-1")})");
            }

            sb.AppendLine();
            sb.AppendLine("=== Результат ===");
            sb.AppendLine($"Определитель (метод Гаусса):                    {result.Determinant.ToString("0.######", CultureInfo.InvariantCulture)}");
            sb.AppendLine($"Проверка (разложение по алгебр. дополнениям):   {checkDet.ToString("0.######", CultureInfo.InvariantCulture)}");
            sb.AppendLine($"Расхождение |Δ|:                                {discrepancy.ToString("0.0e+0", CultureInfo.InvariantCulture)}");
            sb.AppendLine(discrepancy < 1e-6
                ? "Результаты совпадают — определитель найден верно."
                : "Внимание: результаты заметно расходятся, проверьте ввод данных.");

            _txtOutput.Text = sb.ToString();

            _lblSummary.ForeColor = discrepancy < 1e-6 ? Color.DarkGreen : Color.Firebrick;
            _lblSummary.Text =
                $"det(A) = {result.Determinant.ToString("0.######", CultureInfo.InvariantCulture)}\n" +
                $"Проверка: {checkDet.ToString("0.######", CultureInfo.InvariantCulture)}\n" +
                (discrepancy < 1e-6 ? "Совпадает \u2713" : $"Расхождение: {discrepancy:0.0e+0}");
        }

        private static string FormatMatrix(double[,] m)
        {
            int rows = m.GetLength(0);
            int cols = m.GetLength(1);
            var sb = new StringBuilder();
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                    sb.Append(m[i, j].ToString("0.0000", CultureInfo.InvariantCulture).PadLeft(12));
                sb.AppendLine();
            }
            return sb.ToString().TrimEnd('\n', '\r');
        }

        private static string Indent(string text)
        {
            var lines = text.Replace("\r\n", "\n").Split('\n');
            for (int i = 0; i < lines.Length; i++)
                lines[i] = "    " + lines[i];
            return string.Join(Environment.NewLine, lines);
        }

        private static bool TryParseDouble(string s, out double v)
        {
            s = (s ?? "").Trim().Replace(",", ".");
            return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v);
        }
    }
}
