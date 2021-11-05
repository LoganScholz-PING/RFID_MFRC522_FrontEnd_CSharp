using System;
using System.Collections.Generic;
using System.Text;

using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data;
using System.Linq;

using System.ComponentModel;

namespace demoCSHARPtoRFID
{
    public partial class Form1
    {
        public SerialPort _serialPort = new SerialPort();

        public void InitializeSerialComm()
        {
            _serialPort.PortName = "COM12";
            _serialPort.BaudRate = 9600;
            _serialPort.DataBits = 8;
            _serialPort.Parity = System.IO.Ports.Parity.None;
            _serialPort.Encoding = System.Text.Encoding.Default;
            _serialPort.StopBits = StopBits.One;
            _serialPort.Handshake = Handshake.None;

            //_serialPort.DtrEnable = true;
            _serialPort.RtsEnable = true;

            _serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);

            string[] COMports = SerialPort.GetPortNames();
            Array.Sort(COMports);

            foreach (string port in COMports)
            {
                cbPORTS.Items.Add(port);
            }

            try
            {
                if (_serialPort.IsOpen)
                {
                    _serialPort.Close();
                }

                _serialPort.Open();
                txtOUTPUT.AppendText("[INFO] OPENED serial comm on port " + _serialPort.PortName + ".\r\n");

                // clean up the serial buffer
                _serialPort.DiscardInBuffer();
                _serialPort.DiscardOutBuffer();
                _serialPort.BaseStream.Flush();
            }
            catch (Exception ex)
            {
                txtOUTPUT.AppendText("[ERROR] could not connect to serial port " + _serialPort.PortName + ":\r\n" + ex + "\r\n");
            }
        }

        private void cbPORTS_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_serialPort.IsOpen) { _serialPort.Close(); }

            _serialPort.PortName = cbPORTS.SelectedItem.ToString();

            //try
            //{
            //    _serialPort.Open();
            //    txtOUTPUT.AppendText("[INFO] Opened serial comm on port " + _serialPort.PortName + "!\r\n");
            //}
            //catch (Exception ex) { txtOUTPUT.AppendText("[ERROR] COULD NOT open serial comm on port " + _serialPort.PortName + ":\r\n" + ex + "\r\n"); }
        
        }

        private void btnSERIAL_Click(object sender, EventArgs e)
        {
            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
                txtOUTPUT.AppendText("[INFO] CLOSED serial comm on port " + _serialPort.PortName + "!\r\n");
            }
            else
            {
                try
                {
                    _serialPort.Open();
                    txtOUTPUT.AppendText("[INFO] OPENED serial comm on port " + _serialPort.PortName + "!\r\n");
                }
                catch (Exception ex)
                {
                    txtOUTPUT.AppendText("[ERROR] COULD NOT open serial comm on port " + _serialPort.PortName + ":\r\n" + ex + "\r\n");
                }
            }
        }



        // SEND <R> to uC (instruct uC to read tag's workorder # and send the info back to us)
        private async void sendREADPacketToRC522()
        {
            if (!_serialPort.IsOpen)
            {
                txtOUTPUT.AppendText("[ERROR] Attempt to SEND but serial port is closed!" + "\r\n");
                return;
            }

            try
            {
                // clean up the serial buffer
                _serialPort.DiscardInBuffer();
                _serialPort.DiscardOutBuffer();
                _serialPort.BaseStream.Flush();

                lblWORKORDER.Text = "";

                await Task.Run(() => _serialPort.WriteLine("<R>"));
                txtOUTPUT.AppendText("[INFO] Sent \"<R>\" packet to uC\r\n");
            }
            catch (Exception ex)
            {
                txtOUTPUT.AppendText("[ERROR] Error sending <R> to uC:\r\n" + ex + "\r\n");
            }

            // NOTE: When sending <R>, we need to anticipate the Teensy's <D:######> response
        }

        // SEND <W:#######> to uC (instruct uC to write the ####### number to the RFID tag)
        private async void sendWRITEPacketToRC522(string woData)
        {
            if(!_serialPort.IsOpen)
            {
                txtOUTPUT.AppendText("[ERROR] Attempt to SEND but serial port is closed!" + "\r\n");
                return;
            }

            //// clean up the serial buffer
            //_serialPort.DiscardInBuffer();
            //_serialPort.DiscardOutBuffer();
            //_serialPort.BaseStream.Flush();

            try
            {
                await Task.Run(() => _serialPort.WriteLine("<W:" + woData + ">"));
                txtOUTPUT.AppendText("\r\n[INFO] Sent \"<W:" + woData + ">\" packet to uC" + "\r\n");
            }
            catch (Exception ex)
            {
                txtOUTPUT.AppendText("[ERROR] Error sending \"<W:" + woData + ">\" to uC:\r\n" + ex + "\r\n");
            }
        }


        public void parseWorkorderFromRC522(string tempwo)
        {
            if (tempwo.Length > 0)
            {
                if (tempwo.Contains('#'))
                {
                    txtOUTPUT.AppendText("\r\n[DEBUG] uC returned BLANK or CORRUPT workorder: " + tempwo + "\r\n");
                    lblWORKORDER.Text = "CORRUPT [" + tempwo + "]";
                    return;
                }
                else
                {
                    txtOUTPUT.AppendText("\r\n[DEBUG] uC returned workorder: " + tempwo + "\r\n");
                    lblWORKORDER.Text = tempwo;
                }
            }
            else
            {
                txtOUTPUT.AppendText("\r\n[ERROR] UNUSABLE wo [" + tempwo + "]\r\n");
                return;
            }
            
        }


        // -------------- begin all the fancy threading stuff you need to do to receive serial comm with System.IO.Ports --------------
        public delegate void SetTextDelegate(string text);
        private void si_DataReceived(string data)
        {
            // TODO when I know how the serial data exchange will be formatted
            // Ideas:
            // 1. CSHARP sends '<R>' to Teensy to request the RC522 to read the workorder (+ others later possibly) block of memory
            //      1a. TEENSY replies with '<D:@@@@@@@>' to CSHARP's '<R>', containing the workorder # at @@@@@@@
            // 2. CSHARP sends '<W:#######>' to Teensy to request the WRITE of workorder ####### to memory
            //      2a. TEENSY replies with ACK or NACK depending on write success? ***TBD***

            string output = "[DEBUG] Received Packet from Teensy: " + data;

            txtOUTPUT.AppendText(output + Environment.NewLine); // testing Environment.Newline here

            //           0123456789
            //           <D:#######>
            // (11 characters total in <D:######> packet)

            // attempt to validate the data before using an index to select a substring
            if (data.Length == 11 && data.Substring(1,2) == "D:")
            {
                string tempwo = data.Substring(3, 7);
                parseWorkorderFromRC522(tempwo);
            }
        }

        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string inSerialData = _serialPort.ReadLine();
                //inSerialData.Trim('\r', '\n');
                inSerialData = inSerialData.Trim();
                if (inSerialData.Length > 0)
                {
                    this.BeginInvoke(new SetTextDelegate(si_DataReceived), new object[] { inSerialData });
                }
            }
            catch (Exception ex)
            {
                // communication probably interrupted during transmission
            }
        }
        // -------------- end all fancy serial comm threading stuff --------------
    }
}
