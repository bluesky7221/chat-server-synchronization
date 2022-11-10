using System.Net.Sockets;
using System.Net;
using System.Collections;
using System.Text;
using System.Dynamic;

namespace chat
{
    //���� ��� ä�� ���� ���α׷��Դϴ�
    //������ ��ٸ��� ����Ʈ ������ 1, ������ Ŭ���̾�Ʈ�� while ���� �����ϸ� ��� ����ϴ� N ������� �����Ǿ� �ֽ��ϴ�
    public partial class ChatServerForm : Form
    {
        delegate void SetTextDelegate(string s);
        public ChatServerForm()
        {
            InitializeComponent();
            ServerStatus.Tag = "Stop";
        }

        TcpListener cServer = new TcpListener(IPAddress.Parse("127.0.0.1"), 2022);
        public static ArrayList clientSocketArray = new ArrayList();

        private void ServerOnOffBtn_Click(object sender, EventArgs e)
        {
            try
            {
                if (ServerStatus.Tag.ToString() == "Stop")
                {
                    cServer.Start();

                    //Ŭ���̾�Ʈ ��û�� ��ٸ��� ������
                    Thread waitThread = new Thread(new ThreadStart(AcceptClient));
                    waitThread.Start();

                    ServerStatus.Text = "Server on";
                    ServerStatus.Tag = "Start";
                    ServerOnOffBtn.Text = "Server Stop";
                }
                else
                {
                    cServer.Stop();
                    foreach (Socket soket in ChatServerForm.clientSocketArray)
                    {
                        soket.Close();
                    }
                    clientSocketArray.Clear();

                    ServerStatus.Text = "Server off";
                    ServerStatus.Tag = "Stop";
                    ServerOnOffBtn.Text = "Server Start";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Server On error : " + ex.Message);
            }
        }
        
        //Ŭ���̾�Ʈ ������ ��ٸ��鼭 �����ߴٸ� ������ ������ְ� �� Ŭ���̾�Ʈ ���� ���� �������� ������ش�
        private void AcceptClient()
        {
            Socket socketClient = null;
            while (true)
            {
                try
                {
                    //���ο� Ŭ���̾�Ʈ�� �����Ҷ����� ������ �ش� �������� ����˴ϴ�.
                    socketClient = cServer.AcceptSocket();

                    ClientHandler clientHandler = new ClientHandler();
                    clientHandler.ClientHandler_Setup(this, socketClient, this.ChatBox);
                    Thread thd_ChatProcess = new Thread(new ThreadStart(clientHandler.Chat_Process));
                    thd_ChatProcess.Start();
                }
                catch(System.Exception)
                {
                    ChatServerForm.clientSocketArray.Remove(socketClient); break;
                }
            }
        }

        //�ؽ�Ʈ �ڽ��� ��ȭ ����
        public void SetText(string text)
        {
            //�ؽ�Ʈ �ڽ��� ���� ���� �� UI �����尡 �ϴ� ���ε� �׷��� ó���� ������Ѵ�

            //work �����峪 �񵿱� ��ƾ �ȿ��� UI�� �ٷ� ���� �ÿ� UI Thread Crash �߻�
            //�ٸ� �����忡�� UI ���� �ÿ��� InvokeRequired�� ����� ���� ������ �����尡 UI Thread���� üũ �� ��
            //UI ó���� �ؾ� ������ ����
            if (this.ChatBox.InvokeRequired)
            {
                SetTextDelegate d = new SetTextDelegate(SetText);
                this.Invoke(d, new object[] { text });
                //��������Ʈ�� ���� UI�����尡 SetText ȣ���ϵ���


                //���ο� �޸�
                //Delegate : �븮��, �޼ҵ� ������ �����ϴ� ����
                //delegate ��ȯ�� ��������Ʈ��(�Ű�����..);
                //delegate int PDelegate(int a, int b);

                //�� ó�� �Լ��� ȣ���ϴ� ��쵵 ������ �ѹ� ���� ���� �����Լ��� �������ؼ��� ���
                //Pdelegate pd2 = delegate(int a, int b) { return a / b; };

                //Delegate.Combine(new Pdelegate(Plus),...); ��������Ʈ�� ���� �޼ҵ� �Ѳ����� ȣ�� ����

                //event ó���� ���� ��������Ʈ
                //event : Ư�� ����� �������� �˸��� �޼���
                //public delegate void MyEventHandler(string message);
                //������ event ��������Ʈ �̸�;
                //public event MyEventHandler Active;
                //public delegate void MyEventHandler(string message);

                //class Publisher
                //{
                //    public event MyEventHandler Active;

                //    public void DoActive(int number)
                //    {
                //        if (number % 10 == 0)
                //            Active("Active!" + number); ->�̺�Ʈ ����
                //        else
                //            Console.WriteLine(number);
                //    }
                //}

                //class Subscriber
                //{
                //    static public void MyHandler(string message)
                //    {
                //        Console.WriteLine(message); -> �̺�Ʈ �߻�
                //    }

                //    static void Main(string[] args)
                //    {
                //        Publisher publisher = new Publisher();
                //        publisher.Active += new MyEventHandler(MyHandler);

                //        for (int i = 1; i < 50; i++)
                //            publisher.DoActive(i);
                //    }
                //}

                //Invoke�� �����ϰ��� �ϴ� �����쿡�� �Ļ��� �����尡 �ƴ� �ٸ� �����忡�� �� ������
                //�� ������ �õ��� �� ������ �߻���Ű�µ�, �� ������ ���ְ��� ���
                //�κ�ũ ���� textBox�� ���� Thread���� SetText�� ȣ��ȴ�
    }
            else
            {
                //UI �����尡 ȣ��Ǹ鼭 ����Ǵ� ����
                this.ChatBox.AppendText(text);
            }
        }

        private void ChatServerForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
            cServer.Stop();
        }
    }
    public class ClientHandler
    {
        private TextBox ChatBox;
        private Socket socketClient;
        private NetworkStream netStream;
        private StreamReader strReader;
        private ChatServerForm form;

        public void ClientHandler_Setup(ChatServerForm form, Socket socketClient, TextBox ChatBox)
        {
            //chat ��� box
            this.ChatBox = ChatBox;
            //Ŭ���̾�Ʈ ���� ���� -> ��Ʈ���� ����� ä���Ѵ�
            this.socketClient = socketClient;
            this.netStream = new NetworkStream(socketClient);
            //Ŭ���̾�Ʈ ���� ������ ����Ʈ�� Add
            ChatServerForm.clientSocketArray.Add(socketClient);
            this.strReader = new StreamReader(netStream);
            this.form = form;
        }

        //�Ѹ��� Ŭ���̾�Ʈ�� ���� ������ ���� ó�� �Լ�
        public void Chat_Process()
        {
            while (true)
            {
                try
                {
                    string msg = strReader.ReadLine();
                    if (msg != null && msg != "")
                    {
                        form.SetText(msg + "\r\n");
                        byte[] data = Encoding.UTF8.GetBytes(msg + "\r\n");
                        //���� �����忡�� ChatServerForm.clientSocketArray�� ����ȭ�� ��Ű�� ���� lock
                        lock (ChatServerForm.clientSocketArray)
                        {
                            foreach (Socket soket in ChatServerForm.clientSocketArray)
                            {
                                NetworkStream stream = new NetworkStream(soket);
                                stream.Write(data, 0, data.Length);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    //MessageBox.Show("chat error : " + ex.ToString());
                    ChatServerForm.clientSocketArray.Remove(socketClient);
                    break;
                }
            }
        }
    }
}