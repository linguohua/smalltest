using OxyPlot;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace HRSpO2.Data
{
    class LogParser
    {
        public static bool Parse(string text, out List<DataPoint> reds, out List<DataPoint> ireds)
        {
            reds = new List<DataPoint>();
            ireds = new List<DataPoint>();

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
    }
}
