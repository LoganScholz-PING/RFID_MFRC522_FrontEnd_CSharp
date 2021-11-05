using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace demoCSHARPtoRFID
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            InitializeSerialComm();
        }

        private void btnREAD_Click(object sender, EventArgs e)
        {
            // sends '<R>' to Teensy instructing Teensy to read the RFID (if RFID chip is present)
            //      hopefully the Teensy sends back a '<D:@@@@@@@>' packet
            sendREADPacketToRC522();
        }

        private void btnWRITE_Click(object sender, EventArgs e)
        {
            string woData = txtWORKORDER.Text;
            woData = woData.Trim();

            // make sure the workorder is a parsable integer and is exactly 7 digits
            if (int.TryParse(woData, out int result) && woData.Length == 7)
            {
                sendWRITEPacketToRC522(woData);
            }
            else
            {
                txtOUTPUT.AppendText("\r\n[ERROR] A 7-digit INTEGER number is expected in the workorder field! Cancelling write request!\r\n");
            }
        }

        private void btnCLEAR_Click(object sender, EventArgs e)
        {
            txtOUTPUT.Clear();
            lblWORKORDER.Text = "NONE";
            txtWORKORDER.SelectAll();
            txtWORKORDER.Focus();
        }
    }
}
