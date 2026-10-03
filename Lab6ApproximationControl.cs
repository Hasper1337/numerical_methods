using System.Windows.Forms;

namespace GraphPlotter
{
    /// <summary>
    /// Лабораторная работа №6: аппроксимация функции. Объединяет три части
    /// задания в виде вложенных вкладок: интерполяция многочленом,
    /// кусочно-линейная интерполяция, наилучшее среднеквадратичное приближение.
    /// </summary>
    public class Lab6ApproximationControl : UserControl
    {
        public Lab6ApproximationControl()
        {
            Dock = DockStyle.Fill;

            var tabs = new TabControl { Dock = DockStyle.Fill };

            var tab1 = new TabPage("6.1 Интерполяция многочленом");
            tab1.Controls.Add(new Lab6Sub1InterpolationControl());

            var tab2 = new TabPage("6.2 Кусочно-линейная интерполяция");
            tab2.Controls.Add(new Lab6Sub2PiecewiseControl());

            var tab3 = new TabPage("6.3 Наилучшее СК-приближение (вар. 5)");
            tab3.Controls.Add(new Lab6Sub3LeastSquaresControl());

            tabs.TabPages.Add(tab1);
            tabs.TabPages.Add(tab2);
            tabs.TabPages.Add(tab3);

            Controls.Add(tabs);
        }
    }
}
