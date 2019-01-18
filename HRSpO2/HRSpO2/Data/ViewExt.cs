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
            List<DataPoint> reds;
            List<DataPoint> ireds;
            if (!LogParser.Parse(text, out reds, out ireds))
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
            LogParser.MoveAverage(reds, 21, out redsMV);
            LogParser.MoveAverage(redsMV, 101, out redsMV2);
            var s11 = new OxyPlot.Series.LineSeries();
            s11.Title = "R-MV";
            s11.Points.AddRange(redsMV);
            model.Series.Add(s11);
            var s12 = new OxyPlot.Series.LineSeries();
            s12.Title = "R-MV2";
            s12.Points.AddRange(redsMV2);
            model.Series.Add(s12);

            AddPeak2Serials(redsMV, redsMV2, model, "R-MV-P");

            PlotRedAC(redsMV, redsMV2, owner);

            var s2 = new OxyPlot.Series.LineSeries();
            s2.Title = "IR";
            s2.Points.AddRange(ireds);
            model.Series.Add(s2);
            List<DataPoint> iredsMV;
            List<DataPoint> iredsMV2;
            LogParser.MoveAverage(ireds, 21, out iredsMV);
            LogParser.MoveAverage(iredsMV, 101, out iredsMV2);
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
            LogParser.Sub(redsMV, redsMV2, out osc1);
            LogParser.FFT(osc1, out fft1);
            LogParser.Sub(iredsMV, iredsMV2, out osc2);
            LogParser.FFT(osc2, out fft2);

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

            List<DataPoint> dx;
            List<DataPoint> dxBeforeHamming;
            LogParser.Maxim(ireds, reds, out dx, out dxBeforeHamming);
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

        private static void PlotRedAC(List<DataPoint> redsMV, List<DataPoint> redsMV2, System.Windows.Window owner)
        {
            List<DataPoint> osc1;
            LogParser.Sub(redsMV, redsMV2, out osc1);

            //const int sampleRate = 100;

            var n = (int)(osc1.Count / 100) * 100;
            var complex = new Complex[n];
            var used = 0;
            foreach (var dp in osc1)
            {
                complex[used] = new Complex(dp.Y, 0d);
                used++;

                if (used == n)
                {
                    break;
                }
            }

            MathNet.Numerics.IntegralTransforms.Fourier.Forward(complex);

            // reset others to 0
            float maxAmplitude = 0;
            int index = -1;
            var i = 0;
            foreach(var c in complex)
            {
                float m = (float)c.Magnitude;
                if (m > maxAmplitude)
                {
                    maxAmplitude = m;
                    index = i;
                }

                i++;

                // only half search
                if (i > complex.Length/2)
                {
                    break;
                }
            }
            // reset others to 0
            for ( i = 0; i < complex.Length; i++)
            {
                if (i != index && (i != (complex.Length - index)))
                {
                    complex[i] = new Complex();
                }
            }

            List<DataPoint> fft1 = new List<DataPoint>();
            i = 0;
            foreach(var c in complex)
            {
                fft1.Add(new DataPoint(i++, c.Magnitude/complex.Length));
            }


            // inverse
            List<DataPoint> osc2 = new List<DataPoint>();
            MathNet.Numerics.IntegralTransforms.Fourier.Inverse(complex);
            for(i = 0; i < complex.Length; i++)
            {
                osc2.Add(new DataPoint(i, complex[i].Real));
            }

            //for (var i = 0; i < n / 2; i++)
            //{
            //    //
            //}
            var fftWnd = new LogPlotWnd();
            fftWnd.MyModel.Title = "osc";
            fftWnd.MyModel.Series.Clear();
            var s3 = new OxyPlot.Series.LineSeries();
            s3.Title = $"fft";
            s3.Points.AddRange(fft1);
            fftWnd.MyModel.Series.Add(s3);
            
            var s4 = new OxyPlot.Series.LineSeries();
            s4.Title = "osc";
            s4.Points.AddRange(osc1);
            fftWnd.MyModel.Series.Add(s4);

            var s5 = new OxyPlot.Series.LineSeries();
            s5.Title = "inverse";
            s5.Points.AddRange(osc2);
            fftWnd.MyModel.Series.Add(s5);

            fftWnd.Owner = owner;
            fftWnd.Show();
        }

        private static void AddPeak2Serials(List<DataPoint> redsMV, List<DataPoint> redsMV2, PlotModel model, string name)
        {
            List<int> peakLocs;
            LogParser.FindPositivePeak(redsMV, redsMV2, out peakLocs);

            if (peakLocs.Count < 3)
            {
                return;
            }

            List<DataPoint> peaks = new List<DataPoint>();
            foreach(var ploc in peakLocs)
            {
                peaks.Add(new DataPoint(ploc, redsMV[ploc].Y));
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
