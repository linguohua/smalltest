using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Management;
using System.Windows;

namespace HRSpO2
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private Serial.MySerialPort mySerialPort;

        private byte[] loggerBuffer = new byte[10 * 1024];
        private int loggerBufferWriteIndex = 0;
        private bool loggerBufferOverflow = false;
        private int currentLineCount = 0;

        private System.Windows.Threading.DispatcherTimer dispatcherTimer;

        public MainWindow()
        {
            InitializeComponent();

            Button_Refresh_Port_Click(null, null);

            dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(DispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 500);
            dispatcherTimer.Start();
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            UpdateLoggerText();
        }

        private void Button_Refresh_Port_Click(object sender, RoutedEventArgs e)
        {
            string[] theSerialPortNames = enumerateSerialPorts();
            cbPort.Items.Clear();

            foreach (var pn in theSerialPortNames)
            {
                cbPort.Items.Add(pn);
            }

            if (theSerialPortNames.Length < 1)
            {
                return;
            }

            cbPort.SelectedIndex = 0;
        }

        private static string[] enumerateSerialPorts()
        {
            var portNamesUsb = new List<string>();
            var portNamesRaw = new List<string>();

            try
            {
                ManagementObjectSearcher searcher =
                    new ManagementObjectSearcher("root\\WMI",
                    "SELECT * FROM MSSerial_PortName");

                foreach (ManagementObject queryObj in searcher.Get())
                {
                    //If the serial port's instance name contains USB 
                    //it must be a USB to serial device
                    if (queryObj["InstanceName"].ToString().Contains("USB"))
                    {
                        portNamesUsb.Add(queryObj["PortName"].ToString());
                    }
                    else
                    {
                        portNamesRaw.Add(queryObj["PortName"].ToString());
                    }
                }
            }
            catch (ManagementException e)
            {
                Console.WriteLine("An error occurred while querying for WMI data: " + e.Message);
            }

            portNamesUsb.AddRange(portNamesRaw);

            return portNamesUsb.ToArray();
        }

        private void Button_Open_Close_Click(object sender, RoutedEventArgs e)
        {
            if (mySerialPort != null)
            {
                mySerialPort.Close();
                mySerialPort = null;
                btnOpen.Content = "Open";
            }
            else
            {
                try
                {
                    var cfg = CollectPortCfg();
                    mySerialPort = new Serial.MySerialPort(cfg, OnSerialPortDataRecv);
                    mySerialPort.Open();

                    btnOpen.Content = "Close";
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show(ex.Message);

                    mySerialPort = null;
                }
            }
        }

        private void OnSerialPortDataRecv(byte[] buffer, int count)
        {
            lock (this)
            {
                var remain = loggerBuffer.Length - loggerBufferWriteIndex;
                var copy = count;
                if (copy > remain)
                {
                    copy = remain;
                }

                if (copy == 0)
                {
                    loggerBufferOverflow = true;
                    return;
                }

                Array.Copy(buffer, 0, loggerBuffer, loggerBufferWriteIndex, copy);
                loggerBufferWriteIndex += copy;
            }
        }


        private void UpdateLoggerText()
        {
            string str = null;
            lock (this)
            {
                var buffer = loggerBuffer;
                var count = loggerBufferWriteIndex;

                if (count < 1)
                {
                    return;
                }

                str = System.Text.Encoding.ASCII.GetString(buffer, 0, count);

                loggerBufferWriteIndex = 0;
                if (loggerBufferOverflow)
                {
                    loggerBufferOverflow = false;
                    AppendLoggerText("----logger overflow\r\n");
                }
            }

            if (str != null)
            {
                AppendLoggerText(str);
            }
        }

        private void AppendLoggerText(string str)
        {
            int length = TbLogger.Text.Length;
            if (length > 100 * 1024)
            {
                length = 0;
                TbLogger.Text = "----Logger exceed max, clear\r\n";
            }

            TbLogger.Text += str;

            TbLogger.CaretIndex = (length + str.Length);
            TbLogger.ScrollToEnd();

            var lineCount = CountLine(str);
            currentLineCount += lineCount;
            txtCount.Text = $"{currentLineCount}";
        }

        private int CountLine(string str)
        {
            int countLine = 0;

            foreach (var c in str)
            {
                if (c == '\n')
                {
                    countLine++;
                }
            }

            return countLine;
        }

        private Serial.SerialPortCfg CollectPortCfg()
        {
            int baudRate = 115200;
            int dataBits = 8;
            Parity parity = Parity.None;
            StopBits stopBits = StopBits.One;
            Handshake handshake = Handshake.None;

            var text = tbBaud.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new System.Exception("please input valid baud rate");
            }

            if (!int.TryParse(text, out baudRate))
            {
                throw new System.Exception("baud rate can't convert to int");
            }

            text = tbDataBits.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new System.Exception("please input valid data bits");
            }

            if (!int.TryParse(text, out dataBits))
            {
                throw new System.Exception("data bits can't convert to int");
            }

            parity = (Parity)(cbParity.SelectedIndex);
            stopBits = (StopBits)(cbStopBits.SelectedIndex);
            handshake = (Handshake)(cbHandshake.SelectedIndex);

            var cfg = new Serial.SerialPortCfg();
            cfg.baudRate = baudRate;
            cfg.dataBits = dataBits;
            cfg.parity = parity;
            cfg.stopBits = stopBits;
            cfg.handshake = handshake;

            // port name
            cfg.portName = cbPort.SelectedValue as string;

            return cfg;
        }

        private void Button_Clear_Click(object sender, RoutedEventArgs e)
        {
            TbLogger.Text = "";

            currentLineCount = 0;
            txtCount.Text = $"{currentLineCount}";
        }

        private void Button_Plot_Click(object sender, RoutedEventArgs e)
        {
            var text = TbLogger.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            Data.ViewExt.plotRedIRed(text, this);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (mySerialPort != null)
            {
                mySerialPort.Close();
                mySerialPort = null;
            }
        }
    }
}
