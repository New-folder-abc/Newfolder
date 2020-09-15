using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        public static Form1 mf = null;
        public static Form2 f2 = null;
        public static String ConnectIP = null;

        public const int CMD_SCREEN = 1;
        public const int CMD_MOUSE = 2;
        public const int CMD_KEYBOARD = 3;

        public const int Port = 65535;
        public class cp
        {
            public int cmd;
            public int func;
            public int size;
            public byte[] data; 
        }

        byte[] SendBuffer = null;
        TcpClient tcp_client = null;
        // Receive
        public const int RecvBufSize = (10 * 1024);
        cp p = new cp();
        public const int MIN_RECV_BYTES = 32;
        public Form1()
        {
            InitializeComponent();
        }

        // Receive

        private const int RecvBufferSize = 1024;
        public string Status = string.Empty;
        public Thread T = null;

        private void Form1_Load(object sender, EventArgs e)
        {
            label1.Text = "Server is Running...";
            ThreadStart Ts = new ThreadStart(StartReceiving);
            T = new Thread(Ts);

            // ik add
            T.SetApartmentState(ApartmentState.STA);

            T.Start();

            Send_Load(sender, e);


            if (Connect(sender, e))
            { }
            else
            {
                AppExit(sender, e);
            }
        }
        public void StartReceiving()
        {
            ReceiveTCP(Port);
        }


        public void ReceiveTCP(int portN)
        {
            TcpListener Listener = null;
            try
            {
                Listener = new TcpListener(IPAddress.Any, portN);
                Listener.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            byte[] RecData = new byte[RecvBufSize];
            int RecBytes;

            for (; ; )
            {
                TcpClient client = null;
                NetworkStream netstream = null;
                Status = string.Empty;
                try
                {


                    string message = "Accept the Incoming File ";
                    string caption = "Incoming Connection";
                    MessageBoxButtons buttons = MessageBoxButtons.YesNo;
                    DialogResult result;


                    if (Listener.Pending())
                    {
                        client = Listener.AcceptTcpClient();
                        netstream = client.GetStream();
                        Status = "Connected to a client\n";

                        result = MessageBox.Show(message, caption, buttons);

                        if (result == System.Windows.Forms.DialogResult.Yes)
                        {
                            string SaveFileName = string.Empty;
                            SaveFileDialog DialogSave = new SaveFileDialog();
                            DialogSave.Filter = "All files (*.*)|*.*";
                            DialogSave.RestoreDirectory = true;
                            DialogSave.Title = "Where do you want to save the file?";
                            DialogSave.InitialDirectory = @"C:/";
                            if (DialogSave.ShowDialog() == DialogResult.OK)
                                SaveFileName = DialogSave.FileName;
                            if (SaveFileName != string.Empty)
                            {
                                int totalrecbytes = 0;
                                MemoryStream ms = new MemoryStream(RecvBufSize);
                                while ((RecBytes = netstream.Read(RecData, 0, RecData.Length)) > 0)
                                {
                                    if (totalrecbytes >= MIN_RECV_BYTES)
                                    {
                                        // ik add
                                        RecvProcessCmd(p);
                                    }
                                    totalrecbytes += RecBytes;
                                }
                            }
                            netstream.Close();
                            client.Close();

                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    //netstream.Close();
                }
            }
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            T.Abort();
            this.Close();
        }


        // Send


        public string SendingFilePath = string.Empty;
        private const int SendBufferSize = 1024;


        private void Send_Load(object sender, EventArgs e)
        {
            progressBar1.Visible = true;
            progressBar1.Minimum = 1;
            progressBar1.Value = 1;
            progressBar1.Step = 1;

        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            OpenFileDialog Dlg = new OpenFileDialog();
            Dlg.Filter = "All Files (*.*)|*.*";
            Dlg.CheckFileExists = true;
            Dlg.Title = "Choose a File";
            Dlg.InitialDirectory = @"C:\";
            if (Dlg.ShowDialog() == DialogResult.OK)
            {
                SendingFilePath = Dlg.FileName;

            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            if (SendingFilePath != string.Empty)
            {
                SendTCP(SendingFilePath, txtIP.Text, Int32.Parse(txtPort.Text));
            }
            else
                MessageBox.Show("Select a file", "Warning");
        }
        public void SendTCP(string M, string IPA, Int32 PortN)
        {
            byte[] SendingBuffer = null;
            TcpClient client = null;
            lblStatus.Text = "";
            NetworkStream netstream = null;
            try
            {
                client = new TcpClient(IPA, PortN);
                lblStatus.Text = "Connected to the Server...\n";
                netstream = client.GetStream();
                FileStream Fs = new FileStream(M, FileMode.Open, FileAccess.Read);
                int NoOfPackets = Convert.ToInt32(Math.Ceiling(Convert.ToDouble(Fs.Length) / Convert.ToDouble(SendBufferSize)));
                progressBar1.Maximum = NoOfPackets;
                int TotalLength = (int)Fs.Length, CurrentPacketLength, counter = 0;
                for (int i = 0; i < NoOfPackets; i++)
                {
                    if (TotalLength > SendBufferSize)
                    {
                        CurrentPacketLength = SendBufferSize;
                        TotalLength = TotalLength - CurrentPacketLength;
                    }
                    else
                        CurrentPacketLength = TotalLength;
                    SendingBuffer = new byte[CurrentPacketLength];
                    Fs.Read(SendingBuffer, 0, CurrentPacketLength);
                    netstream.Write(SendingBuffer, 0, (int)SendingBuffer.Length);
                    if (progressBar1.Value >= progressBar1.Maximum)
                        progressBar1.Value = progressBar1.Minimum;
                    progressBar1.PerformStep();
                }

                lblStatus.Text = lblStatus.Text + "Sent " + Fs.Length.ToString() + " bytes to the server";
                Fs.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                netstream.Close();
                client.Close();

            }
        }

        private void turnOffToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AppExit(sender, e);
        }

        // my code
        // ik add
        private bool Connect(object sender, EventArgs e)
        {
            f2 = new Form2(this);
            DialogResult dr = f2.ShowDialog(this);
            if (dr == DialogResult.OK)
            {
                ConnectIP = f2.textBox1.Text;
                // Connect to server
                try
                {
                    tcp_client = new TcpClient(ConnectIP, Port);
                    label5.Text = "Connected to the Server...\n";
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                finally
                {/*
                    netstream.Close();
                    client.Close();
                    */
                }
                return true;
            }
            ConnectIP = null;
            return false;
        }

        private void AppExit(object sender, EventArgs e)
        {
            if (T != null)
            {
                T.Abort();
            }
            Close();
        }

        private void minimizeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
        }

        // My Code
        // Send
        public static void SendText(String text)
        {

        }

        public static void SendKey(String text)
        {

        }

        // Receive

        public static void RecvProcessCmd(cp p)
        {
            switch (p.cmd)
            {
                case CMD_SCREEN:
                    break;
                case CMD_MOUSE:
                    break;
                case CMD_KEYBOARD:
                    break;
            }
        }

        // Program
        private const int MIN_COUNTER = 1;
        public static int counter = MIN_COUNTER;
        private const int MAX_COUNTER = 1000;
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (counter >= MAX_COUNTER)
            {
                counter = MIN_COUNTER;
            }
            else
            {
                counter++;
            }
        }
    }
}
