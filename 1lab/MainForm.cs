using System;
using System.Drawing;
using System.Windows.Forms;

namespace GraphPlotter
{
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

            tabs.TabPages.Add(tabLab1);
            tabs.TabPages.Add(tabLab2);

            Controls.Add(tabs);
        }
    }
}
