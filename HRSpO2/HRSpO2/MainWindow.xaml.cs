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

        public MainWindow()
        {
            InitializeComponent();

            Button_Refresh_Port_Click(null, null);
        }

        private void Button_Refresh_Port_Click(object sender, RoutedEventArgs e)
        {
            string[] theSerialPortNames = enumerateSerialPorts();
            cbPort.Items.Clear();

            foreach(var pn in theSerialPortNames)
            {
                cbPort.Items.Add(pn);
            }

            if(theSerialPortNames.Length < 1)
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
            var str = System.Text.Encoding.ASCII.GetString(buffer, 0, count);
            this.Dispatcher.InvokeAsync(() =>
            {
                TbLogger.Text += str;
                TbLogger.CaretIndex = TbLogger.Text.Length;
                TbLogger.ScrollToEnd();
            });
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

            if(!int.TryParse(text, out baudRate))
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
        }
    }
}
