using System.IO;
using System.Text;

namespace HRSpO2.Utility
{
    class Utility
    {
        public static string WriteSafeReadAllLines(string path)
        {
            using (var csv = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var sr = new StreamReader(csv, Encoding.Default))
            {
                return sr.ReadToEnd();
            }
        }
    }
}
