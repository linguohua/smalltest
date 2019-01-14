using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HRSpO2.Serial
{
    class SerialPortCfg
    {
        [JsonProperty("portName")]
        public string portName = "";

        [JsonProperty("baudRate")]
        public int baudRate = 0;

        [JsonProperty("parity")]
        public Parity parity = Parity.None;

        [JsonProperty("dataBits")]
        public int dataBits = 0;

        [JsonProperty("stopBits")]
        public StopBits stopBits = StopBits.One;

        [JsonProperty("handshake")]
        public Handshake handshake = Handshake.None;

        public SerialPortCfg()
        {

        }

        public static SerialPortCfg LoadFromFile(string filePathName)
        {
            var str = Utility.Utility.WriteSafeReadAllLines(filePathName);
            var des = (SerialPortCfg)JsonConvert.DeserializeObject(str, typeof(SerialPortCfg));

            return des;
        }

        public void Save2File(string filePathName)
        {
            var str = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(filePathName, str);
        }
    }
}
