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

            sendWRITEPacketToRC522(woData);
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
