using OxyPlot;
using System.Collections.Generic;
using System;
using System.Numerics;

namespace HRSpO2.Data
{
    class ViewExt
    {
        public static void plotRedIRed(string text, System.Windows.Window owner)
        {
            var ctx = new HRContext();

            if (!LogParser.Parse(text, ctx))
            {
                return;
            }

            ctx.Refine();

            PlotRawMV(owner, ctx);

            //PlotOSC(ctx.redsOSC, ctx.redsIFFTOSC, "red", owner);

            //PlotOSC(ctx.redsOSC, ctx.iredsIFFTOSC, "ired", owner);

            //PlotFFT(owner, ctx);

            PlotMaxim(owner, ctx);
        }

        private static void PlotMaxim(System.Windows.Window owner, HRContext ctx)
        {
            List<DataPoint> dx;
            List<DataPoint> dxBeforeHamming;
            LogParser.Maxim(ctx.iredsRaw, ctx.redsRaw, out dx, out dxBeforeHamming);
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

        private static void PlotRawMV(System.Windows.Window owner, HRContext ctx)
        {
            var wnd = new LogPlotWnd();
            var model = wnd.MyModel;
            model.Title = "R-IR";
            model.Series.Clear();

            var s1 = new OxyPlot.Series.LineSeries();
            s1.Title = $"R:{ctx.HeartRate}";
            s1.Points.AddRange(ctx.redsRaw);
            model.Series.Add(s1);

            var s11 = new OxyPlot.Series.LineSeries();
            s11.Title = "R-MV";
            s11.Points.AddRange(ctx.redsMV1);
            model.Series.Add(s11);
            var s12 = new OxyPlot.Series.LineSeries();
            s12.Title = "R-MV2";
            s12.Points.AddRange(ctx.redsMV2);
            model.Series.Add(s12);

            var s2 = new OxyPlot.Series.LineSeries();
            var sp02 = ctx.SpO2.ToString("0.00");
            s2.Title = $"IR:{sp02}%";
            s2.Points.AddRange(ctx.iredsRaw);
            model.Series.Add(s2);

            var s21 = new OxyPlot.Series.LineSeries();
            var r = ctx.R.ToString("0.00");
            s21.Title = $"iR-MV:{r}";
            s21.Points.AddRange(ctx.iredsMV1);
            model.Series.Add(s21);
            var s22 = new OxyPlot.Series.LineSeries();
            s22.Title = "iR-MV2";
            s22.Points.AddRange(ctx.iredsMV2);
            model.Series.Add(s22);

            wnd.Owner = owner;
            wnd.Show();
        }

        private static void PlotFFT(System.Windows.Window owner, HRContext ctx)
        {
            List<DataPoint> fft1;
            List<DataPoint> fft2;
            LogParser.FFT(ctx.redsOSC, out fft1);
            LogParser.FFT(ctx.iredsOSC, out fft2);

            var fftWnd = new LogPlotWnd();
            fftWnd.MyModel.Title = "fft";
            fftWnd.MyModel.Series.Clear();
            var s3 = new OxyPlot.Series.LineSeries();
            float freq1 = LogParser.FFTMaxAmplitude(fft1);
            s3.Title = $"red-{(int)(freq1 * 60)}";
            s3.Points.AddRange(fft1);
            fftWnd.MyModel.Series.Add(s3);

            var s4 = new OxyPlot.Series.LineSeries();
            float freq2 = LogParser.FFTMaxAmplitude(fft2);
            s4.Title = $"ired-{(int)(freq2 * 60)}";
            s4.Points.AddRange(fft2);
            fftWnd.MyModel.Series.Add(s4);

            fftWnd.Owner = owner;
            fftWnd.Show();
        }

        private static void PlotOSC(List<DataPoint> osc,List<DataPoint> ifftOsc, string name, System.Windows.Window owner)
        {
            //const int sampleRate = 100;

            var oscWnd = new LogPlotWnd();
            oscWnd.MyModel.Title = $"osc-{name}";
            oscWnd.MyModel.Series.Clear();
            
            var s4 = new OxyPlot.Series.LineSeries();
            s4.Title = "osc";
            s4.Points.AddRange(osc);
            oscWnd.MyModel.Series.Add(s4);

            var s5 = new OxyPlot.Series.LineSeries();
            s5.Title = "fft-inverse";
            s5.Points.AddRange(ifftOsc);
            oscWnd.MyModel.Series.Add(s5);

            AddPeak2Serials(osc, oscWnd.MyModel, "osc-peak");
            oscWnd.Owner = owner;
            oscWnd.Show();
        }

        private static void AddPeak2Serials(List<DataPoint> osc, PlotModel model, string name)
        {
            List<int> peakLocs;
     
            LogParser.FindPositivePeak(osc, out peakLocs);

            if (peakLocs.Count < 3)
            {
                return;
            }

            List<DataPoint> peaks = new List<DataPoint>();
            foreach(var ploc in peakLocs)
            {
                peaks.Add(new DataPoint(ploc, osc[ploc].Y));
            }

            int totalIntervals = 0;
            for(var i = 0; i < peakLocs.Count - 1; i++)
            {
                totalIntervals += (peakLocs[i + 1] - peakLocs[i]);
            }

            float averageIntervals = (float)totalIntervals / (peakLocs.Count - 1);
            int hr = (int)(100 * 60 / averageIntervals);
            var s1 = new OxyPlot.Series.LineSeries();
            s1.Title = name+$"-{hr}";
            s1.Points.AddRange(peaks);
            s1.MarkerType = MarkerType.Triangle;
            s1.MarkerSize = 5;
            s1.LineStyle = LineStyle.None;

            model.Series.Add(s1);
        }
    }
}
