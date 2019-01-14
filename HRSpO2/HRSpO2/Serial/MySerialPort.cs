using System.IO.Ports;
using System.Threading;

namespace HRSpO2.Serial
{
    public delegate void SerialPortRecvHnalder(byte[] buffer, int count);

    class MySerialPort
    {
        private const int THREAD_READ_BUFFER_SIZE = 256;

        private SerialPortCfg portCfg;

        private SerialPort serialPort;

        private Thread readingThread;

        private SerialPortRecvHnalder handler;

        public MySerialPort(SerialPortCfg portCfg, SerialPortRecvHnalder handler)
        {
            this.portCfg = portCfg;
            this.handler = handler;
        }

        public bool HasOpen
        {
            get
            {
                return serialPort != null;
            }
        }

        public void Open()
        {
            if (HasOpen)
            {
                throw new System.Exception($"serial port {portCfg.portName} has opened");
            }

            if (portCfg == null)
            {
                throw new System.Exception($"serial port has no valid cfg");
            }

            // Create a new SerialPort object with default settings.
            serialPort = new SerialPort();

            // Allow the user to set the appropriate properties.
            serialPort.PortName = portCfg.portName;
            serialPort.BaudRate = portCfg.baudRate;
            serialPort.Parity = portCfg.parity;
            serialPort.DataBits = portCfg.dataBits;
            serialPort.StopBits = portCfg.stopBits;
            serialPort.Handshake = portCfg.handshake;

            serialPort.Open();

            readingThread = new Thread(ReadPort);
            readingThread.Start(this);
        }

        public void Close()
        {
            if (!HasOpen)
            {
                throw new System.Exception($"serial port {portCfg.portName} has not been opened");
            }

            serialPort.Close();
            readingThread.Join();

            serialPort = null;
            readingThread = null;
        }

        private static void ReadPort(object portInstance)
        {
            var myPortInstance = (MySerialPort) (portInstance);
            var sp = myPortInstance.serialPort;

            var buffer = new byte[THREAD_READ_BUFFER_SIZE];
            while (sp.IsOpen)
            {
                try
                {
                    int read = sp.Read(buffer, 0, buffer.Length);
                    if (read > 0)
                    {
                        myPortInstance.OnRecv(buffer, read);
                    }
                }
                catch (System.Exception) { }
            }
        }

        private void OnRecv(byte[] buffer, int count)
        {
            this.handler(buffer, count);
        }
    }
}
