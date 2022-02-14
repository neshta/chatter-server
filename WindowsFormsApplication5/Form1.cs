using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;

namespace WindowsFormsApplication5
{
    public partial class Form1 : Form
    {
        int _clientCount = 0;
        bool _working = false;
        bool _stopNetwork = false;
        const int MAX_CLIENTS = 200;
        TcpListener _server;
        TcpClient[] clients = new TcpClient[MAX_CLIENTS];

        public Form1()
        {
            InitializeComponent();
            //System.Windows.Forms.Control.CheckForIllegalCrossThreadCalls = false;

        }

        private void button1_Click(object sender, EventArgs e)
        {
            Console.Beep(3000, 80);
            Console.Beep(1000, 100);
            StartServer();
        }

        private void StartServer()
        {
            if (_working == false)
            {
                try
                {
                    richTextBox1.AppendText("Запуск сервера...\n");
                    _stopNetwork = false;
                    _clientCount = 0;
                    int port = int.Parse(textBox1.Text);
                    _server = new TcpListener(IPAddress.Any, port);
                    _server.Start();
                    Thread acceptThread = new Thread(AcceptClients);
                    acceptThread.Start();
                    System.Drawing.Bitmap image = WindowsFormsApplication5.Properties.Resources.img_on;
                    pictureBox1.Image = image;
                    button2.Text = "Остановить";
                    button1.Text = "Сервер запущен";
                    button2.Enabled = true;
                    button1.Enabled = false;
                    richTextBox1.AppendText("Сервер запущен.\n\n");
                }
                catch(Exception ex)
                {
                    richTextBox1.AppendText("При запуске сервера произошла ошибка:\n" + ex.ToString() + "\n\n");
                    Console.Beep(3000, 80);
                    Console.Beep(1000, 100);
                }
            }
        }

        private void StopServer()
        {
            if (_server != null)
            {
                richTextBox1.AppendText("Остановка сервера...\n");
                SendToClients("ByeAll", 0);
                Thread.Sleep(100);
                _server.Stop();
                _server = null;
                _stopNetwork = true;

                for (int i = 0; i < MAX_CLIENTS; i++)
                {
                    if (clients[i] != null) clients[i].Close();
                }
                System.Drawing.Bitmap image = WindowsFormsApplication5.Properties.Resources.img_off;
                pictureBox1.Image = image;
                button2.Text = "Сервер не запущен";
                button1.Text = "Запустить сервер";
                button2.Enabled = false;
                button1.Enabled = true;
                richTextBox1.AppendText("Сервер остановлен.\n\n");
                _clientCount = 0;
                Console.Beep(3000, 80);
                Console.Beep(1000, 100);
            }
        }

        void AcceptClients()
        {
            while (true)
            {
                try
                {
                    this.clients[_clientCount] = _server.AcceptTcpClient();
                    Thread readThread = new Thread(ReceiveRun);
                    readThread.Start(_clientCount);
                    _clientCount++;
                    label3.Text = "Подключено: " + _clientCount;

                    if (richTextBox1.InvokeRequired) richTextBox1.Invoke(new Add((s) => richTextBox1.Text = richTextBox1.Text + s), "Запрос на подключение...\nКлиент подключен.\n");
                    else richTextBox1.AppendText("Запрос на подключение...\nКлиент подключен.\n");

                    Console.Beep(3000, 80);
                    Console.Beep(1000, 100);
                }
                catch
                {
                    //MessageBox.Show(ex.ToString());
                    //if (richTextBox1.InvokeRequired) richTextBox1.Invoke(new Add((s) => richTextBox1.Text = richTextBox1.Text + s), "При подключении клиента произошла ошибка.\n");
                    //else richTextBox1.AppendText("При подключении клиента произошла ошибка.\n" + ex.ToString());
                }


                if (_clientCount == MAX_CLIENTS || _stopNetwork == true)
                {
                    break;
                }
                
            }
        }

        void SendToClients(string text, int skipindex)
        {
            for (int i = 0; i < MAX_CLIENTS; i++)
            {
                if (clients[i] != null)
                {
                    //if (i == skipindex) continue;
                    NetworkStream ns = clients[i].GetStream();
                    byte[] myReadBuffer = Encoding.Unicode.GetBytes(text);
                    ns.BeginWrite(myReadBuffer, 0, myReadBuffer.Length,
                    new AsyncCallback(AsyncSendCompleted), ns);
                }
            }
        }

        public void AsyncSendCompleted(IAsyncResult ar)
        {
            NetworkStream ns = (NetworkStream)ar.AsyncState;
            ns.EndWrite(ar);
        }

        void ReceiveRun(object num)
        {
            while (true)
            {
                try
                {
                    string s = null;
                    StringBuilder nm = new StringBuilder(null);
                    StringBuilder mes = new StringBuilder(null);
                    NetworkStream ns = clients[(int)num].GetStream();
                    //clients[(int)num].ReceiveBufferSize = 2;
                    while (ns.DataAvailable == true)
                    {
                        byte[] buffer = new byte[clients[(int)num].Available];

                        ns.Read(buffer, 0, buffer.Length);
                        s += Encoding.Unicode.GetString(buffer);
                    }

                    if (s != null)
                    {
                        if (s.Equals("Bye"))
                        {
                            //try
                            //{
                               // clients[(int)num].Close();
                            //}
                            //catch { }
                            _clientCount--;
                            label3.Text = "Подключено: " + _clientCount;
                        }
                        else
                        {
                            Invoke(new UpdateReceiveDisplayDelegate(UpdateReceiveDisplay), new object[] { (int)num, s });
                            s = "[" + ((int)num).ToString() + "] " + s;
                            SendToClients(s, (int)num);
                            s = String.Empty;
                        }
                    }
                    Thread.Sleep(100);
                }
                catch(Exception ex)
                {
                    clients[(int)num].Close();
                    if (richTextBox1.InvokeRequired) richTextBox1.Invoke(new Add((s) => richTextBox1.Text = richTextBox1.Text + s), "При подключении клиента произошла ошибка.\n" + ex.ToString());
                    else richTextBox1.AppendText("При подключении клиента произошла ошибка.\n" + ex.ToString());
                }


                if (_stopNetwork == true) break;

            }
        }

        public void UpdateReceiveDisplay(int clientnum, string message)
        {
            richTextBox1.AppendText("[" + clientnum.ToString() + "] " + message + "\n");
        }

        protected delegate void UpdateReceiveDisplayDelegate(int clientcount, string message);

        delegate void Add(string text);

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            StopServer();
            label3.Text = "Подключено: 0";
        }

        private void textBox1_Enter(object sender, EventArgs e)
        {

        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            StopServer();
        }
    }
}
