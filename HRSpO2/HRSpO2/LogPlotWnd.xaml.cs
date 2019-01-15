using OxyPlot;
using OxyPlot.Series;
using System;
using System.Windows;

namespace HRSpO2
{
    /// <summary>
    /// LogPlot.xaml 的交互逻辑
    /// </summary>
    public partial class LogPlotWnd : Window
    {
        public LogPlotWnd()
        {
            this.MyModel = new PlotModel { Title = "Example 1" };
            this.MyModel.Series.Add(new FunctionSeries(Math.Cos, 0, 10, 0.1, "cos(x)"));

            this.DataContext = this;
            InitializeComponent();
        }

        public PlotModel MyModel { get; private set; }
    }
}
