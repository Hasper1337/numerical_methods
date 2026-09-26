using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace GraphPlotter
{
    public class MainForm : Form
    {
        private readonly RadioButton _rbAnalytical;
        private readonly RadioButton _rbTable;

        private readonly Panel _pnlAnalytical;
        private readonly TextBox _txtFormula;
        private readonly Button _btnPlot;

        private readonly Panel _pnlTable;
        private readonly Button _btnLoadFile;
        private readonly Label _lblFileName;

        private readonly Button _btnReset;
        private readonly GraphControl _graphControl;

        private readonly StatusStrip _statusStrip;
        private readonly ToolStripStatusLabel _lblCoords;
        private readonly ToolStripStatusLabel _lblScale;
        private readonly ToolStripStatusLabel _lblHint;

        public MainForm()
        {
            Text = "Лабораторная работа 1. Визуализация данных";
            Width = 1100;
            Height = 750;
            MinimumSize = new Size(700, 450);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font(FontFamily.GenericSansSerif, 9f);

            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 96,
                Padding = new Padding(8)
            };

            _rbAnalytical = new RadioButton
            {
                Text = "Аналитическая функция",
                Location = new Point(8, 6),
                AutoSize = true,
                Checked = true
            };
            _rbTable = new RadioButton
            {
                Text = "Табличная функция",
                Location = new Point(8, 28),
                AutoSize = true
            };
            _rbAnalytical.CheckedChanged += (_, _) => UpdateModeVisibility();

            _btnReset = new Button
            {
                Text = "Сбросить вид",
                Location = new Point(190, 6),
                Width = 120,
                Height = 26
            };
            _btnReset.Click += (_, _) => _graphControl!.ResetView();

            _pnlAnalytical = new Panel
            {
                Location = new Point(8, 52),
                Width = 900,
                Height = 34
            };
            var lblFormula = new Label
            {
                Text = "f(x) =",
                Location = new Point(0, 8),
                AutoSize = true
            };
            _txtFormula = new TextBox
            {
                Location = new Point(50, 4),
                Width = 500,
                Text = "sin(x) + 0.5*cos(2*x)"
            };
            _txtFormula.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    PlotAnalytical();
                }
            };
            _btnPlot = new Button
            {
                Text = "Построить",
                Location = new Point(560, 2),
                Width = 110,
                Height = 26
            };
            _btnPlot.Click += (_, _) => PlotAnalytical();
            _pnlAnalytical.Controls.Add(lblFormula);
            _pnlAnalytical.Controls.Add(_txtFormula);
            _pnlAnalytical.Controls.Add(_btnPlot);

            _pnlTable = new Panel
            {
                Location = new Point(8, 52),
                Width = 900,
                Height = 34,
                Visible = false
            };
            _btnLoadFile = new Button
            {
                Text = "Загрузить файл...",
                Location = new Point(0, 2),
                Width = 140,
                Height = 26
            };
            _btnLoadFile.Click += (_, _) => LoadTableFromFile();
            _lblFileName = new Label
            {
                Text = "Файл не выбран",
                Location = new Point(150, 8),
                AutoSize = true,
                ForeColor = Color.DimGray
            };
            _pnlTable.Controls.Add(_btnLoadFile);
            _pnlTable.Controls.Add(_lblFileName);

            topPanel.Controls.Add(_rbAnalytical);
            topPanel.Controls.Add(_rbTable);
            topPanel.Controls.Add(_btnReset);
            topPanel.Controls.Add(_pnlAnalytical);
            topPanel.Controls.Add(_pnlTable);

            _graphControl = new GraphControl
            {
                Dock = DockStyle.Fill
            };
            _graphControl.MouseWorldMove += (_, world) =>
            {
                _lblCoords.Text = $"x = {world.X.ToString("0.####", CultureInfo.InvariantCulture)}, " +
                                   $"y = {world.Y.ToString("0.####", CultureInfo.InvariantCulture)}";
            };
            _graphControl.ViewChanged += (_, _) =>
            {
                _lblScale.Text = $"Масштаб: {_graphControl.Scale.ToString("0.####", CultureInfo.InvariantCulture)} пикс./ед.";
            };

            _statusStrip = new StatusStrip();
            _lblCoords = new ToolStripStatusLabel { Text = "x = —, y = —", Spring = false, BorderSides = ToolStripStatusLabelBorderSides.Right };
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

            Controls.Add(_graphControl);
            Controls.Add(topPanel);
            Controls.Add(_statusStrip);

            UpdateModeVisibility();

            Shown += (_, _) => PlotAnalytical();
        }

        private void UpdateModeVisibility()
        {
            _pnlAnalytical.Visible = _rbAnalytical.Checked;
            _pnlTable.Visible = !_rbAnalytical.Checked;
        }

        private void PlotAnalytical()
        {
            string formula = _txtFormula.Text;
            try
            {
                var evaluator = new ExpressionEvaluator(formula);
                evaluator.Validate();

                _graphControl.Function = x => evaluator.Evaluate(x);
                _graphControl.TablePoints = null;
                _graphControl.ResetView();
            }
            catch (ExpressionParseException ex)
            {
                MessageBox.Show(this, ex.Message, "Ошибка в выражении",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Не удалось разобрать выражение: " + ex.Message,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void LoadTableFromFile()
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Выберите текстовый файл с табличными данными",
                Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*"
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                var points = ParseTableFile(dialog.FileName);
                if (points.Count == 0)
                {
                    MessageBox.Show(this, "Файл не содержит корректных числовых пар (x, y).",
                        "Пустые данные", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                points.Sort((a, b) => a.X.CompareTo(b.X));

                _graphControl.TablePoints = points;
                _graphControl.Function = null;
                _graphControl.FitToPoints(points);

                _lblFileName.Text = Path.GetFileName(dialog.FileName) + $"  ({points.Count} точек)";
                _lblFileName.ForeColor = Color.Black;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Не удалось прочитать файл: " + ex.Message,
                    "Ошибка чтения файла", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Читает текстовый файл с двумя столбцами чисел (x и y), разделёнными
        /// пробелом, табуляцией, точкой с запятой или их комбинацией. Строки,
        /// которые не удаётся разобрать, пропускаются.
        /// </summary>
        private static List<(double X, double Y)> ParseTableFile(string path)
        {
            var result = new List<(double X, double Y)>();
            var delimiterRegex = new Regex(@"[\t; ]+", RegexOptions.Compiled);

            foreach (var rawLine in File.ReadLines(path))
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("#") || line.StartsWith("//"))
                    continue;

                string[] parts = delimiterRegex.Split(line);
                var numeric = new List<string>();
                foreach (var part in parts)
                {
                    if (!string.IsNullOrWhiteSpace(part))
                        numeric.Add(part.Trim());
                }

                if (numeric.Count < 2)
                    continue;

                if (TryParseNumber(numeric[0], out double x) && TryParseNumber(numeric[1], out double y))
                    result.Add((x, y));
            }

            return result;
        }

        private static bool TryParseNumber(string token, out double value)
        {
            if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                return true;

            // Поддержка десятичной запятой (русская локаль).
            string normalized = token.Replace(",", ".");
            return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }
    }
}
