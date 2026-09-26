using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;

namespace GraphPlotter
{
    /// <summary>
    /// Область построения графика: сетка, оси, аналитическая функция и/или
    /// табличные точки. Поддерживает масштабирование колесом мыши и
    /// перемещение (панорамирование) перетаскиванием левой кнопкой мыши.
    /// </summary>
    public class GraphControl : Control
    {
        // Мировые координаты точки, которая в данный момент находится
        // в центре области построения.
        private double _centerX;
        private double _centerY;

        // Масштаб: количество экранных пикселей на одну единицу мировых координат.
        private double _scale = 40.0;

        private const double MinScale = 1e-6;
        private const double MaxScale = 1e12;
        private const double DesiredGridSpacingPx = 80.0;

        private bool _isPanning;
        private Point _panStartScreen;
        private double _panStartCenterX;
        private double _panStartCenterY;

        /// <summary>Аналитически заданная функция y = f(x), либо null.</summary>
        public Func<double, double>? Function { get; set; }

        /// <summary>Табличные точки (x, y), отсортированные по x, либо null.</summary>
        public List<(double X, double Y)>? TablePoints { get; set; }

        /// <summary>Текущий масштаб (пикселей на единицу).</summary>
        public double Scale => _scale;

        /// <summary>Вызывается при изменении масштаба или центра области просмотра.</summary>
        public event EventHandler? ViewChanged;

        /// <summary>Вызывается при перемещении мыши над областью построения (мировые координаты).</summary>
        public event EventHandler<(double X, double Y)>? MouseWorldMove;

        public GraphControl()
        {
            DoubleBuffered = true;
            BackColor = Color.White;
            SetStyle(ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            TabStop = true;
        }

        /// <summary>Сбрасывает вид: начало координат в центре, масштаб по умолчанию.</summary>
        public void ResetView()
        {
            _centerX = 0.0;
            _centerY = 0.0;
            _scale = 40.0;
            Invalidate();
            ViewChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Подбирает масштаб и центр так, чтобы все точки поместились в область просмотра.</summary>
        public void FitToPoints(List<(double X, double Y)> points)
        {
            if (points == null || points.Count == 0)
            {
                ResetView();
                return;
            }

            double minX = double.MaxValue, maxX = double.MinValue;
            double minY = double.MaxValue, maxY = double.MinValue;
            foreach (var (x, y) in points)
            {
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }

            double rangeX = Math.Max(maxX - minX, 1e-9);
            double rangeY = Math.Max(maxY - minY, 1e-9);

            _centerX = (minX + maxX) / 2.0;
            _centerY = (minY + maxY) / 2.0;

            int w = Math.Max(Width, 100);
            int h = Math.Max(Height, 100);

            double scaleX = (w * 0.8) / rangeX;
            double scaleY = (h * 0.8) / rangeY;
            _scale = Math.Min(scaleX, scaleY);
            _scale = Math.Max(MinScale, Math.Min(_scale, MaxScale));

            Invalidate();
            ViewChanged?.Invoke(this, EventArgs.Empty);
        }

        public (double X, double Y) ScreenToWorld(Point p)
        {
            double wx = _centerX + (p.X - Width / 2.0) / _scale;
            double wy = _centerY - (p.Y - Height / 2.0) / _scale;
            return (wx, wy);
        }

        public Point WorldToScreen(double wx, double wy)
        {
            int sx = (int)Math.Round(Width / 2.0 + (wx - _centerX) * _scale);
            int sy = (int)Math.Round(Height / 2.0 - (wy - _centerY) * _scale);
            return new Point(sx, sy);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            var worldBefore = ScreenToWorld(e.Location);

            double factor = e.Delta > 0 ? 1.2 : 1.0 / 1.2;
            double newScale = _scale * factor;
            newScale = Math.Max(MinScale, Math.Min(newScale, MaxScale));
            _scale = newScale;

            var worldAfter = ScreenToWorld(e.Location);
            _centerX += worldBefore.X - worldAfter.X;
            _centerY += worldBefore.Y - worldAfter.Y;

            Invalidate();
            ViewChanged?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            // Гарантируем, что колесо мыши масштабирует график, даже если
            // элемент управления ещё не получал фокус.
            if (!Focused) Focus();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
            if (e.Button == MouseButtons.Left)
            {
                _isPanning = true;
                _panStartScreen = e.Location;
                _panStartCenterX = _centerX;
                _panStartCenterY = _centerY;
                Cursor = Cursors.Hand;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_isPanning)
            {
                double dxPixels = e.X - _panStartScreen.X;
                double dyPixels = e.Y - _panStartScreen.Y;
                _centerX = _panStartCenterX - dxPixels / _scale;
                _centerY = _panStartCenterY + dyPixels / _scale;
                Invalidate();
                ViewChanged?.Invoke(this, EventArgs.Empty);
            }

            var world = ScreenToWorld(e.Location);
            MouseWorldMove?.Invoke(this, world);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left)
            {
                _isPanning = false;
                Cursor = Cursors.Default;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackColor);

            double step = ComputeNiceStep(DesiredGridSpacingPx / _scale);

            DrawGrid(g, step);
            DrawAxes(g, step);

            if (TablePoints != null && TablePoints.Count > 0)
                DrawTablePoints(g);

            if (Function != null)
                DrawFunction(g);
        }

        /// <summary>
        /// Подбирает "красивый" шаг сетки (1, 2 или 5 умноженное на степень десяти),
        /// ближайший к желаемому значению. Работает корректно для сколь угодно
        /// малых шагов (в том числе до разряда десятитысячных и меньше).
        /// </summary>
        private static double ComputeNiceStep(double approxStep)
        {
            if (approxStep <= 0 || double.IsNaN(approxStep) || double.IsInfinity(approxStep))
                return 1.0;

            double exponent = Math.Floor(Math.Log10(approxStep));
            double fraction = approxStep / Math.Pow(10, exponent);

            double niceFraction;
            if (fraction < 1.5) niceFraction = 1;
            else if (fraction < 3.5) niceFraction = 2;
            else if (fraction < 7.5) niceFraction = 5;
            else niceFraction = 10;

            return niceFraction * Math.Pow(10, exponent);
        }

        private static int DecimalsForStep(double step)
        {
            if (step <= 0 || double.IsNaN(step)) return 0;
            int decimals = (int)Math.Ceiling(-Math.Log10(step) - 1e-9);
            return Math.Max(0, decimals);
        }

        private void DrawGrid(Graphics g, double step)
        {
            using var minorPen = new Pen(Color.FromArgb(235, 235, 235));
            using var majorPen = new Pen(Color.FromArgb(210, 210, 210));

            var worldTopLeft = ScreenToWorld(new Point(0, 0));
            var worldBottomRight = ScreenToWorld(new Point(Width, Height));

            double minX = worldTopLeft.X, maxX = worldBottomRight.X;
            double minY = worldBottomRight.Y, maxY = worldTopLeft.Y;

            long firstIndexX = (long)Math.Floor(minX / step);
            long lastIndexX = (long)Math.Ceiling(maxX / step);
            for (long i = firstIndexX; i <= lastIndexX; i++)
            {
                double wx = i * step;
                int sx = WorldToScreen(wx, 0).X;
                var pen = (i % 5 == 0) ? majorPen : minorPen;
                g.DrawLine(pen, sx, 0, sx, Height);
            }

            long firstIndexY = (long)Math.Floor(minY / step);
            long lastIndexY = (long)Math.Ceiling(maxY / step);
            for (long i = firstIndexY; i <= lastIndexY; i++)
            {
                double wy = i * step;
                int sy = WorldToScreen(0, wy).Y;
                var pen = (i % 5 == 0) ? majorPen : minorPen;
                g.DrawLine(pen, 0, sy, Width, sy);
            }
        }

        private void DrawAxes(Graphics g, double step)
        {
            using var axisPen = new Pen(Color.Black, 1.5f);
            using var textBrush = new SolidBrush(Color.Black);
            using var font = new Font(FontFamily.GenericSansSerif, 8f);

            int decimals = DecimalsForStep(step);
            string format = "F" + decimals;

            var worldTopLeft = ScreenToWorld(new Point(0, 0));
            var worldBottomRight = ScreenToWorld(new Point(Width, Height));
            double minX = worldTopLeft.X, maxX = worldBottomRight.X;
            double minY = worldBottomRight.Y, maxY = worldTopLeft.Y;

            // Ось Ox: рисуем на позиции y=0, если она не видна - прижимаем к краю,
            // чтобы подписи и линия всегда оставались доступны.
            int axisYScreen = WorldToScreen(0, 0).Y;
            int clampedAxisY = Math.Max(14, Math.Min(axisYScreen, Height - 4));
            g.DrawLine(axisPen, 0, clampedAxisY, Width, clampedAxisY);

            int axisXScreen = WorldToScreen(0, 0).X;
            int clampedAxisX = Math.Max(2, Math.Min(axisXScreen, Width - 40));
            g.DrawLine(axisPen, clampedAxisX, 0, clampedAxisX, Height);

            // Стрелки на концах осей
            DrawArrow(g, axisPen, new Point(Width - 2, clampedAxisY), 0);
            DrawArrow(g, axisPen, new Point(clampedAxisX, 2), 1);

            g.DrawString("x", font, textBrush, Width - 14, clampedAxisY - 16);
            g.DrawString("y", font, textBrush, clampedAxisX + 6, 2);

            long firstIndexX = (long)Math.Floor(minX / step);
            long lastIndexX = (long)Math.Ceiling(maxX / step);
            for (long i = firstIndexX; i <= lastIndexX; i++)
            {
                if (i == 0) continue;
                double wx = i * step;
                int sx = WorldToScreen(wx, 0).X;
                g.DrawLine(axisPen, sx, clampedAxisY - 3, sx, clampedAxisY + 3);
                string label = wx.ToString(format, CultureInfo.InvariantCulture);
                g.DrawString(label, font, textBrush, sx - 10, clampedAxisY + 4);
            }

            long firstIndexY = (long)Math.Floor(minY / step);
            long lastIndexY = (long)Math.Ceiling(maxY / step);
            for (long i = firstIndexY; i <= lastIndexY; i++)
            {
                if (i == 0) continue;
                double wy = i * step;
                int sy = WorldToScreen(0, wy).Y;
                g.DrawLine(axisPen, clampedAxisX - 3, sy, clampedAxisX + 3, sy);
                string label = wy.ToString(format, CultureInfo.InvariantCulture);
                g.DrawString(label, font, textBrush, clampedAxisX - 34, sy - 7);
            }

            g.DrawString("0", font, textBrush, clampedAxisX - 12, clampedAxisY + 4);
        }

        private static void DrawArrow(Graphics g, Pen pen, Point tip, int direction)
        {
            // direction: 0 = острие вправо (ось Ox), 1 = острие вверх (ось Oy)
            if (direction == 0)
            {
                g.DrawLine(pen, tip.X, tip.Y, tip.X - 8, tip.Y - 4);
                g.DrawLine(pen, tip.X, tip.Y, tip.X - 8, tip.Y + 4);
            }
            else
            {
                g.DrawLine(pen, tip.X, tip.Y, tip.X - 4, tip.Y + 8);
                g.DrawLine(pen, tip.X, tip.Y, tip.X + 4, tip.Y + 8);
            }
        }

        private void DrawFunction(Graphics g)
        {
            if (Function == null) return;

            using var pen = new Pen(Color.FromArgb(30, 90, 200), 2f);
            const double jumpThresholdFactor = 4.0; // во сколько раз больше высоты области считается разрывом

            List<PointF>? segment = null;
            double? prevScreenY = null;

            for (int px = 0; px <= Width; px++)
            {
                double wx = ScreenToWorld(new Point(px, 0)).X;
                double wy;
                try
                {
                    wy = Function(wx);
                }
                catch
                {
                    wy = double.NaN;
                }

                bool valid = !double.IsNaN(wy) && !double.IsInfinity(wy);
                double screenY = valid ? WorldToScreen(wx, wy).Y : double.NaN;

                bool bigJump = valid && prevScreenY.HasValue &&
                               Math.Abs(screenY - prevScreenY.Value) > Height * jumpThresholdFactor;

                if (!valid || bigJump)
                {
                    FlushSegment(g, pen, segment);
                    segment = null;
                    prevScreenY = null;
                    if (!valid) continue;
                }

                segment ??= new List<PointF>();
                segment.Add(new PointF(px, (float)screenY));
                prevScreenY = screenY;
            }

            FlushSegment(g, pen, segment);
        }

        private static void FlushSegment(Graphics g, Pen pen, List<PointF>? segment)
        {
            if (segment != null && segment.Count >= 2)
                g.DrawLines(pen, segment.ToArray());
        }

        private void DrawTablePoints(Graphics g)
        {
            if (TablePoints == null || TablePoints.Count == 0) return;

            using var linePen = new Pen(Color.FromArgb(200, 60, 60), 2f);
            using var pointBrush = new SolidBrush(Color.FromArgb(200, 60, 60));

            PointF[] screenPoints = new PointF[TablePoints.Count];
            for (int i = 0; i < TablePoints.Count; i++)
            {
                var (x, y) = TablePoints[i];
                var sp = WorldToScreen(x, y);
                screenPoints[i] = new PointF(sp.X, sp.Y);
            }

            if (screenPoints.Length >= 2)
                g.DrawLines(linePen, screenPoints);

            const float r = 3.5f;
            foreach (var p in screenPoints)
                g.FillEllipse(pointBrush, p.X - r, p.Y - r, 2 * r, 2 * r);
        }
    }
}
