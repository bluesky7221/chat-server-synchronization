using System.Net.Sockets;
using System.Net;
using System.Collections;
using System.Text;
using System.Dynamic;

namespace chat
{
    //동기 방식 채팅 서버 프로그램입니다
    //접속을 기다리는 웨이트 스레드 1, 각각의 클라이언트를 while 무한 루프하며 출력 대기하는 N 스레드로 구성되어 있습니다
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

                    //클라이언트 요청을 기다리는 쓰레드
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
        
        //클라이언트 접속을 기다리면서 접속했다면 소켓을 만들어주고 각 클라이언트 마다 전담 쓰레드을 만들어준다
        private void AcceptClient()
        {
            Socket socketClient = null;
            while (true)
            {
                try
                {
                    //새로운 클라이언트가 접속할때까지 서버는 해당 구문에서 블락됩니다.
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

        //텍스트 박스에 대화 쓰기
        public void SetText(string text)
        {
            //텍스트 박스에 글을 쓰는 건 UI 쓰레드가 하는 일인데 그래서 처리를 해줘야한다

            //work 스레드나 비동기 루틴 안에서 UI에 바로 접근 시에 UI Thread Crash 발생
            //다른 스레드에서 UI 접근 시에는 InvokeRequired를 사용해 현재 진입한 스레드가 UI Thread인지 체크 한 후
            //UI 처리를 해야 문제가 없다
            if (this.ChatBox.InvokeRequired)
            {
                SetTextDelegate d = new SetTextDelegate(SetText);
                this.Invoke(d, new object[] { text });
                //델리게이트를 통해 UI쓰레드가 SetText 호출하도록


                //공부용 메모
                //Delegate : 대리자, 메소드 참조를 포함하는 영역
                //delegate 반환형 델리게이트명(매개변수..);
                //delegate int PDelegate(int a, int b);

                //위 처럼 함수를 호출하는 경우도 있지만 한번 쓰고 버릴 무명함수로 쓰기위해서도 사용
                //Pdelegate pd2 = delegate(int a, int b) { return a / b; };

                //Delegate.Combine(new Pdelegate(Plus),...); 델리게이트를 통해 메소드 한꺼번에 호출 가능

                //event 처리를 위한 델리게이트
                //event : 특정 사건이 벌어지면 알리는 메세지
                //public delegate void MyEventHandler(string message);
                //한정자 event 델리게이트 이름;
                //public event MyEventHandler Active;
                //public delegate void MyEventHandler(string message);

                //class Publisher
                //{
                //    public event MyEventHandler Active;

                //    public void DoActive(int number)
                //    {
                //        if (number % 10 == 0)
                //            Active("Active!" + number); ->이벤트 조건
                //        else
                //            Console.WriteLine(number);
                //    }
                //}

                //class Subscriber
                //{
                //    static public void MyHandler(string message)
                //    {
                //        Console.WriteLine(message); -> 이벤트 발생
                //    }

                //    static void Main(string[] args)
                //    {
                //        Publisher publisher = new Publisher();
                //        publisher.Active += new MyEventHandler(MyHandler);

                //        for (int i = 1; i < 50; i++)
                //            publisher.DoActive(i);
                //    }
                //}

                //Invoke는 접근하고자 하는 윈도우에서 파생된 쓰레드가 아닌 다른 쓰레드에서 이 윈도우
                //에 접근을 시도할 때 에러를 발생시키는데, 이 에러를 없애고자 사용
                //인보크 사용시 textBox를 만든 Thread에서 SetText가 호출된다
    }
            else
            {
                //UI 쓰레드가 호출되면서 실행되는 구문
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
            //chat 출력 box
            this.ChatBox = ChatBox;
            //클라이언트 접속 소켓 -> 스트림을 만들어 채팅한다
            this.socketClient = socketClient;
            this.netStream = new NetworkStream(socketClient);
            //클라이언트 접속 소켓을 리스트에 Add
            ChatServerForm.clientSocketArray.Add(socketClient);
            this.strReader = new StreamReader(netStream);
            this.form = form;
        }

        //한명의 클라이언트가 글을 보냈을 때의 처리 함수
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
                        //여러 스레드에서 ChatServerForm.clientSocketArray를 동기화를 시키기 위한 lock
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