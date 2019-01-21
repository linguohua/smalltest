using OxyPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace HRSpO2.Data
{
    class HRContext
    {
        public List<DataPoint> redsRaw;
        public List<DataPoint> iredsRaw;

        public List<DataPoint> redsMV1;
        public List<DataPoint> redsMV2;

        public List<DataPoint> iredsMV1;
        public List<DataPoint> iredsMV2;

        public List<DataPoint> redsOSC;
        public List<DataPoint> iredsOSC;

        public List<DataPoint> redsIFFTOSC;
        public List<DataPoint> iredsIFFTOSC;

        public float SpO2;
        public int HeartRate;
        public float R;

        public void Refine()
        {
            const int acMVWindow = 11;
            const int dcMVWindow = 101;
            LogParser.MoveAverage(redsRaw, acMVWindow, out redsMV1);
            LogParser.MoveAverage(redsMV1, dcMVWindow, out redsMV2);

            LogParser.MoveAverage(this.iredsRaw, acMVWindow, out iredsMV1);
            LogParser.MoveAverage(iredsMV1, dcMVWindow, out iredsMV2);

            LogParser.Sub(redsMV1, redsMV2, out redsOSC);
            LogParser.Sub(iredsMV1, iredsMV2, out iredsOSC);

            float freq1, freq2;

            redsIFFTOSC = FFTFilter(redsOSC, out freq1);
            iredsIFFTOSC = FFTFilter(iredsOSC, out freq2);

            SpO2 = LogParser.Spo2Calc(this, out R);

            HeartRate = (int)(freq1 * 60);
        }

        private static List<DataPoint> FFTFilter(List<DataPoint> osc, out float freq)
        {
            freq = 0.0f;
            var n = (int)(osc.Count / 100) * 100;
            var complex = new Complex[n];
            var used = 0;
            foreach (var dp in osc)
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
            foreach (var c in complex)
            {
                float m = (float)c.Magnitude;
                if (m > maxAmplitude)
                {
                    maxAmplitude = m;
                    index = i;
                }

                i++;

                // only half search
                if (i > complex.Length / 2)
                {
                    break;
                }
            }

            const int sampleRate = 100;
            var freqs = MathNet.Numerics.IntegralTransforms.Fourier.FrequencyScale(complex.Length, sampleRate);
            freq = (float)freqs[index];

            // reset others to 0
            for (i = 0; i < complex.Length; i++)
            {
                if (i != index && (i != (complex.Length - index)))
                {
                    complex[i] = new Complex();
                }
            }

            // inverse
            List<DataPoint> osc2 = new List<DataPoint>();
            MathNet.Numerics.IntegralTransforms.Fourier.Inverse(complex);
            for (i = 0; i < complex.Length; i++)
            {
                osc2.Add(new DataPoint(i, complex[i].Real));
            }

            return osc2;
        }
    }
}
