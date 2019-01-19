using OxyPlot;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace HRSpO2.Data
{
    class LogParser
    {
        public static bool Parse(string text, HRContext ctx)
        {
            var reds = new List<DataPoint>();
            var ireds = new List<DataPoint>();

            ctx.redsRaw = reds;
            ctx.iredsRaw = ireds;

            var index = 0;
            using (var ss = new StringReader(text))
            {
                while (true)
                {
                    var line1 = ss.ReadLine();
                    if (line1 == null)
                    {
                        break;
                    }

                    if (!line1.StartsWith("r:"))
                    {
                        continue;
                    }

                    var line2 = ss.ReadLine();
                    if (line2 == null)
                    {
                        break;
                    }

                    if (!line2.StartsWith("ir:"))
                    {
                        continue;
                    }

                    int intValue1;
                    if (!int.TryParse(line1.Substring(2), out intValue1))
                    {
                        continue;
                    }


                    int intValue2;
                    if (!int.TryParse(line2.Substring(3), out intValue2))
                    {
                        continue;
                    }

                    reds.Add(new DataPoint(index, intValue1));
                    ireds.Add(new DataPoint(index, intValue2));

                    index++;
                }
            }

            return reds.Count > 0 || ireds.Count > 0;
        }

        public static bool FFT(List<DataPoint> data, out List<DataPoint> output)
        {
            output = new List<DataPoint>();

            const int sampleRate = 100;

            var n = (int)(data.Count / 100) * 100;
            var complex = new Complex[n];
            var used = 0;
            foreach (var dp in data)
            {
                complex[used] = new Complex(dp.Y, 0d);
                used++;

                if (used == n)
                {
                    break;
                }
            }

            MathNet.Numerics.IntegralTransforms.Fourier.Forward(complex);

            var freq = MathNet.Numerics.IntegralTransforms.Fourier.FrequencyScale(n, sampleRate);

            for (var i = 0; i < n / 2; i++)
            {
                output.Add(new DataPoint(freq[i], complex[i].Magnitude));
            }

            return true;
        }

        public static float FFTMaxAmplitude(List<DataPoint> fft)
        {
            double amplitude = 0;
            float freq = 0;

            foreach (var dp in fft)
            {
                if (dp.Y > amplitude)
                {
                    amplitude = dp.Y;
                    freq = (float)dp.X;
                }
            }

            return freq;
        }

        public static void MoveAverage(List<DataPoint> data, int window, out List<DataPoint> output)
        {
            output = new List<DataPoint>();

            int total = data.Count;
            float[] fdata = new float[total];
            var index = 0;
            foreach (var dp in data)
            {
                fdata[index] = (float)dp.Y;
                index++;
            }

            float[] odata = new float[total];
            float sum = 0;
            int windowSizeMax = window;
            int half = (int)windowSizeMax / 2;
            int feed = 0;
            int windowSize = 0;
            index = 0;

            for (var i = 0; i < (1 + half) && feed < total; i++)
            {
                sum += fdata[feed];
                feed++;
                windowSize++;
            }

            for (; index < (half) && feed < total;)
            {
                odata[index] = sum / (float)windowSize;

                sum += fdata[feed];

                feed++;
                index++;
                windowSize++;
            }

            for (; feed < total;)
            {
                odata[index] = sum / (float)windowSize;

                sum += fdata[feed];
                sum -= fdata[feed - windowSize];

                feed++;
                index++;
            }

            for (; index < total;)
            {
                odata[index] = sum / (float)windowSize;

                windowSize--;
                sum -= fdata[total - windowSize];
                index++;
            }

            index = 0;
            foreach (var y in odata)
            {
                output.Add(new DataPoint(index, y));
                index++;
            }
        }

        public static void Sub(List<DataPoint> src1, List<DataPoint> src2, out List<DataPoint> output)
        {
            output = new List<DataPoint>();

            var src2Array = src2.ToArray();
            var i = 0;
            foreach (var dp in src1)
            {
                output.Add(new DataPoint(dp.X, (dp.Y - src2Array[i].Y)));
                i++;
            }
        }

        public static void Maxim(List<DataPoint> ireds, List<DataPoint> reds, out List<DataPoint> dx, out List<DataPoint> dxBeforeHamming)
        {
            dx = new List<DataPoint>();
            dxBeforeHamming = new List<DataPoint>();

            int supportedLength = HRCLib.HRCLib.BufferSizeSupported();
            var redsArray = new int[supportedLength];
            var iredsArray = new int[supportedLength];

            var i = 0;
            foreach (var dp in reds)
            {
                redsArray[i] = (int)dp.Y;
                i++;

                if (i == supportedLength)
                {
                    break;
                }
            }

            i = 0;
            foreach (var dp in ireds)
            {
                iredsArray[i] = (int)dp.Y;
                i++;

                if (i == supportedLength)
                {
                    break;
                }
            }

            HRCLib.HRCLib.HRCalc(iredsArray, redsArray);
            var dxArray = new int[supportedLength];
            HRCLib.HRCLib.GetDx(dxArray);
            i = 0;
            foreach (var d in dxArray)
            {
                dx.Add(new DataPoint(i++, d));
            }

            HRCLib.HRCLib.GetDxBeforeHamming(dxArray);
            i = 0;
            foreach (var d in dxArray)
            {
                dxBeforeHamming.Add(new DataPoint(i++, d));
            }
        }

        public static void FindPositivePeak(List<DataPoint> osc, out List<int> peakLocs)
        {
            int length = osc.Count;
            var oscArray = osc.ToArray();

            peakLocs = new List<int>();

            for (var i = 1; i < oscArray.Length-1; i++)
            {
                var p0 = oscArray[i-1];
                var p1 = oscArray[i];
                var p2 = oscArray[i + 1];

                if (p1.Y > 0)
                {
                    if (p1.Y > p0.Y && p1.Y > p2.Y)
                    {
                        peakLocs.Add(i);
                    }
                }
            }
        }

        public static float Spo2Calc(HRContext ctx)
        {
            List<int> redPeakLocs;
            List<int> iredPeakLocs;

            FindPositivePeak(ctx.redsOSC, out redPeakLocs);
            FindPositivePeak(ctx.iredsOSC, out iredPeakLocs);

            List<float> redACDC = new List<float>();
            foreach(var rpl in redPeakLocs)
            {
                var redAC = ctx.redsMV1[rpl].Y;
                var redDC = ctx.redsMV2[rpl].Y;

                var ratio = redAC / redDC;
                redACDC.Add((float)ratio);
            }

            List<float> iredACDC = new List<float>();
            foreach (var irpl in iredPeakLocs)
            {
                var iredAC = ctx.redsMV1[irpl].Y;
                var iredDC = ctx.redsMV2[irpl].Y;

                var ratio = iredAC / iredDC;
                iredACDC.Add((float)ratio);
            }

            float ratioFinal = Average(redACDC) / Average(iredACDC);

            double spO2 =  -45.060* ratioFinal * ratioFinal + 30.354 * ratioFinal + 94.845 ;  // for comparison with table
            return (float)spO2;
        }

        private static float Average(List<float> series)
        {
            float sum = 0;
            foreach(var v in series)
            {
                sum += v;
            }

            return sum / (float)series.Count;
        }
    }
}
