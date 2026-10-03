using System;
using System.Drawing;
using System.Windows.Forms;

namespace GraphPlotter
{
    /// <summary>
    /// Главное окно приложения: содержит лабораторные работы в виде вкладок.
    /// Компоненты (GraphControl, ExpressionEvaluator) переиспользуются между вкладками.
    /// </summary>
    public class MainForm : Form
    {
        public MainForm()
        {
            Text = "Лабораторные работы. Визуализация данных и нелинейные уравнения";
            Width = 1200;
            Height = 800;
            MinimumSize = new Size(800, 500);
            StartPosition = FormStartPosition.CenterScreen;

            var tabs = new TabControl { Dock = DockStyle.Fill };

            var tabLab1 = new TabPage("Лаб. 1 — Визуализация данных");
            tabLab1.Controls.Add(new Lab1VisualizationControl());

            var tabLab2 = new TabPage("Лаб. 2 — Нелинейные уравнения");
            tabLab2.Controls.Add(new Lab2EquationControl());

            var tabLab3 = new TabPage("Лаб. 3 — Экстремум функции");
            tabLab3.Controls.Add(new Lab3ExtremumControl());

            var tabLab4 = new TabPage("Лаб. 4 — Линейная алгебра");
            tabLab4.Controls.Add(new Lab4DeterminantControl());

            var tabLab5 = new TabPage("Лаб. 5 — Итерационные методы СЛАУ");
            tabLab5.Controls.Add(new Lab5RelaxationControl());

            var tabLab6 = new TabPage("Лаб. 6 — Аппроксимация функции");
            tabLab6.Controls.Add(new Lab6ApproximationControl());

            tabs.TabPages.Add(tabLab1);
            tabs.TabPages.Add(tabLab2);
            tabs.TabPages.Add(tabLab3);
            tabs.TabPages.Add(tabLab4);
            tabs.TabPages.Add(tabLab5);
            tabs.TabPages.Add(tabLab6);

            Controls.Add(tabs);
        }
    }
}
