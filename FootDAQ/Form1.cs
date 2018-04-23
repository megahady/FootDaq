using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Management;
using System.IO;
using System.Diagnostics;
using System.IO.Ports;
namespace FootDAQ
{
    public partial class Form1 : Form
    {
        string bufferString = "";
        double[][] buffer = new double[5][]; // Main Buffer
        int NumberOfSensors = new int();
        long totalSample=new Int32();
        int SampleWindowSize=new int();  // width of your Chart 
        Stopwatch sw = new Stopwatch();
        private bool recordflag = false;  // Recording flag
        bool anydata=true; // for choosing between reading any data at delegatr or reading sensor data
        int indx = 0;  // to reset consol display every 100 readings

        public Form1()
        {
            
            InitializeComponent();
            string[] ports = SerialPort.GetPortNames();
            foreach (string port in ports)
            {
                comboBox1.Items.Add(port);
            }
            //comboBox1.SelectedItem = comboBox1.Items[1];
            ArdDataSize.SelectedItem = ArdDataSize.Items[3];
            BRate.SelectedItem = BRate.Items[6];
            //if (AutodetectArduinoPort() != "")
            {
                //comboBox1.Text = AutodetectArduinoPort();
                
                NumberOfSensors = 5;
                SampleWindowSize=500;
                totalSample = 0;
                for(int i=0;i<5;i++)
                {
                    buffer[i] = new double[500]; // Initialize the chart buffer
                }
                
            }
            //else
            {
                //MessageBox.Show("Arduino Not Connected");
            }
            

        }

        //About DialogBox
        private void aboutToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            AboutBox1 aa = new AboutBox1();
            aa.Show();
        }

        // Arduino autodetection method
        string AutodetectArduinoPort()
        {
            ManagementScope connectionScope = new ManagementScope();
            SelectQuery serialQuery = new SelectQuery("SELECT * FROM Win32_SerialPort");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(connectionScope, serialQuery);
            try
            {
                foreach (ManagementObject item in searcher.Get())
                {
                    string desc = item["Description"].ToString();
                    string deviceId = item["DeviceID"].ToString();
                    if (desc.Contains("Arduino"))
                    {
                        return deviceId;
                    }
                }
            }
            catch (ManagementException e)
            {
                MessageBox.Show(e.Message);  /* Do Nothing */
            }
            return "";
        }
   
        // Check Arduino Btn 
        private void CheckArduino_Click(object sender, EventArgs e)
        {
            if (AutodetectArduinoPort() != "")
            {
                if (!comboBox1.Items.Contains(AutodetectArduinoPort()))
                {
                    comboBox1.Items.Add(AutodetectArduinoPort());
                    comboBox1.SelectedItem = AutodetectArduinoPort();
                    MESSAGE_("Arduino Port: " + AutodetectArduinoPort());
                }
            }
            else
            {
                MESSAGE_("Arduino Not Detected");
            }
            
        }
        // Connect Btn
        private void Connect_Click(object sender, EventArgs e)
        {
            ConnectMethod();
        }

        // Connect Menue
        private void connectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ConnectMethod();
        }

        // Disconnect Btn
        private void Disconnect_Click(object sender, EventArgs e)
        {
            DisconnectMethod();
        }

        // Disconnect Menue
        private void disconnectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DisconnectMethod();    
        }

        /// <summary>
        /// Connection Method
        private void ConnectMethod()
        {
            // Read Settings
            if (!serialPort1.IsOpen && AutodetectArduinoPort()!="")
            {
                if(ReadSetting())
                {
                    try
                    {
                        serialPort1.Open();             // Open the port
                        // Dactivate some controls
                        groupPorts.Enabled = false;
                        groupSetting.Enabled = false;
                        Disconnect.Enabled = true;
                        Connect.Enabled = false;
                        sw.Start();                     // Start StopWatch 
                        stateLabel4.Text = "0";         // Update timer
                        MESSAGE_("Ok");
                        // Read Number of Samples 
                        NumberOfSensors = Convert.ToInt32(NofSensors.Text);
                        double x = Convert.ToDouble(serialPort1.BaudRate / (Convert.ToDouble(NofSensors.Text) * 10 * (2 + ArdDataSize.SelectedIndex)));
                        stateLabel5.Text = x.ToString() + " Hz     ";
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("Something Error","Error");
                    }
                }
            }

           
            
        }

        /// Disconect Method
        private void DisconnectMethod()
        { 
            // Stop Stoopwatch 
            sw.Stop();
            sw.Reset();
            totalSample = 0;
            try
            {
                serialPort1.Close();
                serialPort1.Dispose();
                // Activate some controls
                groupPorts.Enabled = true;
                groupSetting.Enabled = true;
                Disconnect.Enabled = false;
                Connect.Enabled = true;
            }
            catch (Exception)
            {
                MessageBox.Show("Unable to Close Serial Port", "Error");
            }
           
        }

        /// Read Setting
        private bool ReadSetting()
        {
            string[] por=SerialPort.GetPortNames();
            if (por[0]!="")
            {

                serialPort1.PortName = "COM5";// comboBox1.SelectedText;
                serialPort1.BaudRate = Convert.ToInt32(BRate.SelectedItem.ToString());
                MESSAGE_("========Ok===========");
                return true;
            }
            else
            {
                MESSAGE_("Check connection settings");
                return false;
            }
        }

        // Save Date to .Dat file
        private void saveDateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string Header = "";
            // Create Sensors Headline
            for (int i = 1; i < NumberOfSensors; i++)
            {
                Header += "S" + i.ToString() + ",";
            }
            Header += "S" + NumberOfSensors.ToString() + "\n";

            bufferString = Header + bufferString;
            // Create a SaveFileDialog to request a path and file name to save to.
            SaveFileDialog saveFile1 = new SaveFileDialog();

            // Initialize the SaveFileDialog 
            saveFile1.DefaultExt = "*.dat";
            saveFile1.Filter = "Data Files|*.dat";

            // Determine if the user selected a file name from the saveFileDialog.
            if (saveFile1.ShowDialog() == System.Windows.Forms.DialogResult.OK &&
               saveFile1.FileName.Length > 0)
            {
                // Save the contents of the RichTextBox into the file.
                //bufferString..SaveFile(saveFile1.FileName, RichTextBoxStreamType.PlainText);
                File.WriteAllText(saveFile1.FileName, bufferString);
            }
        }

        //===============================================================================================================
        //===============================================================================================================
        // Data recieved method 
        // Simple Data Analysis
        private void serialPort1_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            string line;
            try
            {
                if (anydata)
                {
                    line = serialPort1.ReadLine();
                }
                else
                {
                    line = serialPort1.ReadTo("@");
                    line = line.TrimStart('\0');
                }
                string[] sepa = line.Split(',');
                // foreach (var value in sepa)
                for (int k = 0; k < NumberOfSensors; k++)
                {
                    buffer[k][SampleWindowSize - 1] = Convert.ToDouble(sepa[k]); // To see just the first signal change  "value" to "sepa[0]"
                    Array.Copy(buffer[k], 1, buffer[k], 0, SampleWindowSize - 1);
                }
                this.BeginInvoke(new LineReceivedEvent(LineReceived), line);
            }
            catch (Exception)
            { }
        }
        //===============================================================================================================
        // Delegate definition
        private delegate void LineReceivedEvent(string line);
        private void LineReceived(string line)
        {

            //YOU'VE A PURE DATA WITHIN line
            // RECORDING PROCESS Update
            if(recordflag)
            {
                bufferString += line + "\n";
                totalSample += 1;
            }
            else {
                if(indx> 100)
                {
                    myconsol.Text = "";
                    indx = 0;
                }
                else
                {
                    myconsol.Text += line + "\n";
                    indx++;
                }
            }
            // Chart update 
            chart1.Series["Sensor 1"].Points.Clear(); // you've to clear chart
            chart1.Series["Sensor 2"].Points.Clear();
            chart1.Series["Sensor 3"].Points.Clear();
            chart1.Series["Sensor 4"].Points.Clear();
            chart1.Series["Sensor 5"].Points.Clear();

            for (int j = 0; j < NumberOfSensors; ++j)
                for (int i = 0; i < SampleWindowSize - 1; ++i)
                {
                    chart1.Series["Sensor " + (j + 1).ToString()].Points.AddY(buffer[j][i]); //// very important
                }
            // Time Update
            stateLabel4.Text = (sw.Elapsed.Minutes).ToString() + ":" + (sw.Elapsed.Seconds).ToString() + ":" + (sw.Elapsed.Milliseconds).ToString();// myTimer.ToString();

            // Samples Update
            stateLabel2.Text = totalSample.ToString();
        }

        private void sampleArduinoCodeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            myconsol.Text =
            "\t/* Template Arduino Sketch for Sensor Reading" + "\n" +
            "\t*  Coded By : Mohamed Abdelhady" + "\n" +
            "\t*  Cleveland State University" + "\n" +
            "\t*  megahady@gmaiil.com" + "\n" +
            "\t*/" + "\n" +
            "\tvoid setup() {" + "\n" +
            "\tSerial.begin(115200);}" + "\n\n" +
            "\tvoid loop() {" + "\n" +
            "\t{" + "\n" +
            "if(i<2*3.1415)  {i=i+0.1;} else {i=0;} \n"+
            "\tSerial.print(sin(i)*100); \n"+
            "//delay(10);" + "\n" +
            "\tSerial.print(',');" + "\n" +
            "//delay(10);" + "\n" +
            "\tSerial.print(random(0,255));" + "\n" +
            "//delay(10);" + "\n" +
            "\tSerial.print(',');" + "\n" +
            "//delay(10);" + "\n" +
            "\tSerial.print(random(0,255));" + "\n" +
            "//delay(10);" + "\n" +
            "\tSerial.print(',');" + "\n" +
            "//delay(10);" + "\n" +
            "\tSerial.print(random(0,255));" + "\n" +
            "\tSerial.print(',');" + "\n" +
            "\tSerial.print(random(0,255));" + "\n" +
            "\tSerial.print('@');" + "\n" +
            "\tdelay(100);" + "\n" +
            "}" + "\n" +
            "}" + "\n";
        }

        private void RECORD_Click(object sender, EventArgs e)
        {
            recordflag = !recordflag;
            if (recordflag==true)
            {
                // Clear buffer
                bufferString = "";

                RECORD.Text = "Stop Recording";
                RECORD.BackColor = Color.Red;
                MESSAGE_("Recording Start");


            }
            else
            {
                RECORD.Text = "Recording";
                RECORD.BackColor = SystemColors.Control;
                MESSAGE_("Recording Stop");
            }
        }
        private void MESSAGE_(string msg)
        {
            myconsol.Text +=DateTime.Now.ToShortTimeString() + ">>"+msg+"\n";
        }
        //===============================================================================================================
        //===============================================================================================================
    }
}
