using OxyPlot;
using System.Collections.Generic;

namespace HRSpO2.Data
{
    class ViewExt
    {
        public static void plotRedIRed(string text, System.Windows.Window owner)
        {
            List<DataPoint> reds;
            List<DataPoint> ireds;
            if (!Data.LogParser.Parse(text, out reds, out ireds))
            {
                return;
            }

            var wnd = new LogPlotWnd();
            var model = wnd.MyModel;
            model.Title = "R-IR";
            model.Series.Clear();

            var s1 = new OxyPlot.Series.LineSeries();
            s1.Title = "R";
            s1.Points.AddRange(reds);
            model.Series.Add(s1);

            List<DataPoint> redsMV;
            List<DataPoint> redsMV2;
            Data.LogParser.MoveAverage(reds, 21, out redsMV);
            Data.LogParser.MoveAverage(redsMV, 101, out redsMV2);
            var s11 = new OxyPlot.Series.LineSeries();
            s11.Title = "R-MV";
            s11.Points.AddRange(redsMV);
            model.Series.Add(s11);
            var s12 = new OxyPlot.Series.LineSeries();
            s12.Title = "R-MV2";
            s12.Points.AddRange(redsMV2);
            model.Series.Add(s12);

            var s2 = new OxyPlot.Series.LineSeries();
            s2.Title = "IR";
            s2.Points.AddRange(ireds);
            model.Series.Add(s2);
            List<DataPoint> iredsMV;
            List<DataPoint> iredsMV2;
            Data.LogParser.MoveAverage(ireds, 21, out iredsMV);
            Data.LogParser.MoveAverage(iredsMV, 101, out iredsMV2);
            var s21 = new OxyPlot.Series.LineSeries();
            s21.Title = "iR-MV";
            s21.Points.AddRange(iredsMV);
            model.Series.Add(s21);
            var s22 = new OxyPlot.Series.LineSeries();
            s22.Title = "iR-MV2";
            s22.Points.AddRange(iredsMV2);
            model.Series.Add(s22);

            wnd.Owner = owner;
            wnd.Show();

            List<DataPoint> fft1;
            List<DataPoint> osc1;
            List<DataPoint> fft2;
            List<DataPoint> osc2;
            Data.LogParser.Sub(redsMV, redsMV2, out osc1);
            Data.LogParser.FFT(osc1, out fft1);
            Data.LogParser.Sub(iredsMV, iredsMV2, out osc2);
            Data.LogParser.FFT(osc2, out fft2);

            var fftWnd = new LogPlotWnd();
            fftWnd.MyModel.Title = "fft";
            fftWnd.MyModel.Series.Clear();
            var s3 = new OxyPlot.Series.LineSeries();
            s3.Title = "red";
            s3.Points.AddRange(fft1);
            fftWnd.MyModel.Series.Add(s3);

            var s4 = new OxyPlot.Series.LineSeries();
            s4.Title = "ired";
            s4.Points.AddRange(fft2);
            fftWnd.MyModel.Series.Add(s4);

            fftWnd.Owner = owner;
            fftWnd.Show();

            List<DataPoint> dx;
            List<DataPoint> dxBeforeHamming;
            Data.LogParser.Maxim(ireds, reds, out dx, out dxBeforeHamming);
            var maximWnd = new LogPlotWnd();
            maximWnd.MyModel.Title = "maxim";
            maximWnd.MyModel.Series.Clear();
            var s5 = new OxyPlot.Series.LineSeries();
            int hr = HRCLib.HRCLib.GetHeartRate();
            s5.Title = $"dx-{hr}";
            s5.Points.AddRange(dx);
            maximWnd.MyModel.Series.Add(s5);

            var s6 = new OxyPlot.Series.LineSeries();
            s6.Title = "dx-b-hamming";
            s6.Points.AddRange(dxBeforeHamming);
            maximWnd.MyModel.Series.Add(s6);
            maximWnd.Owner = owner;
            maximWnd.Show();
        }
    }
}
