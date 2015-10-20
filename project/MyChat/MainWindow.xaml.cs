using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace MyChat
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //Параметры соединения 

        private string serverTcpIP;
        private int serverTcpPort;

        //Сокеты

        private TcpClient tcpClient;

        //Потоки

        private Thread usersThread;
        private Thread messagesThread;
        private DispatcherTimer timer;

        //Вспомогательные

        private char separator = '|';
        private string userLogin;
        private bool isCrash;

        //Коллекции

        private List<Message> messages = new List<Message>(1000);
        private List<string> users = new List<string>(100);

        public MainWindow()
        {
            InitializeComponent();

            this.timer = new DispatcherTimer();
            this.timer.Tick += timer_Tick;
            this.timer.Interval = TimeSpan.FromSeconds(2);
            
            this.Loaded += MainWindow_Loaded;
            
            this.lbUsers.ItemsSource = this.users;
            this.lbMessages.ItemsSource = this.messages;
        }

        #region GUI_Events

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                this.serverTcpIP = ConfigurationManager.AppSettings["tcpIP"];
                this.serverTcpPort = Int32.Parse(ConfigurationManager.AppSettings["tcpPort"]);

                this.Login();
            }
            catch (ConfigurationErrorsException cexc)
            {
                MessageBox.Show(cexc.Message, "Файл конфигурации", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
            }
            catch (FormatException fexc)
            {
                MessageBox.Show(fexc.Message, "Неверные параметры конфигурации", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message, "Всё сломалось", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
            }
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.SendMessage();
            }
            catch
            {
                MessageBox.Show("Невозможно отправить сообщение", "Проблемы с соединением", MessageBoxButton.OK, MessageBoxImage.Warning);
                this.Reconnect();
            }
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            this.Reconnect();
        }

        #endregion

        #region Threads

        private void UpdateUsers()
        {
            Console.WriteLine("Поток обновления пользователей запущен");

            while (true)
            {
                try
                {
                    this.GetUsers();
                    this.Dispatcher.Invoke(new Action(() => this.lbUsers.Items.Refresh()));
                    Thread.Sleep(3000);
                }
                catch
                {
                    this.isCrash = true;
                    Console.WriteLine("Поток обновления пользователей завершён");
                    break;
                }
            }
        }

        private void UpdateMessages()
        {
            Console.WriteLine("Поток обновления сообщений запущен");
            while (true)
            {
                try
                {
                    this.GetMessages();
                    this.Dispatcher.Invoke(new Action(() => this.lbMessages.Items.Refresh()));
                    Thread.Sleep(1500);
                }
                catch
                {
                    this.isCrash = true;
                    Console.WriteLine("Поток обновления сообщений завершён");
                    break;
                }
            }
        }

        private void GetUsers()
        {
            string request = "GUS" + this.separator;
            string responce = "";
            lock (this.tcpClient)
            {
                this.SendRequestToServer(request);
                responce = this.GetResponseFromServer();
            }
            string[] respArr = responce.Split(this.separator);
            switch (respArr[0])
            {
                case "OK":
                    this.users.Clear();
                    for (int i = 1; i < respArr.Length; i++)
                    {
                        this.users.Add(respArr[i]);
                    }
                    break;
                case "ERR":
                    MessageBox.Show(respArr[1], "Получение списка пользователей", MessageBoxButton.OK, MessageBoxImage.Warning);
                    break;
                default:
                    break;
            }
        }

        private void GetMessages()
        {
            string request = "GMS" + this.separator;
            string responce = "";
            lock (this.tcpClient)
            {
                this.SendRequestToServer(request);
                responce = this.GetResponseFromServer();
            }
            string[] respArr = responce.Split(this.separator);
            switch (respArr[0])
            {
                case "OK":
                    if (this.messages.Count >= 1000)
                    {
                        this.messages.RemoveRange(0, 100);
                    }
                    Message msg = null;
                    for (int i = 1; i < respArr.Length; i += 3)
                    {
                        msg = new Message(respArr[i], respArr[i + 1] + ":", respArr[i + 2]);
                        this.messages.Add(msg);
                    }
                    break;
                case "ERR":
                    MessageBox.Show(respArr[1], "Получение списка сообщений", MessageBoxButton.OK, MessageBoxImage.Warning);
                    break;
                default:
                    break;
            }
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            Console.WriteLine("Проверка соединения: {0}", this.isCrash);
            if (this.isCrash == true)
            {
                MessageBox.Show("Проблемы с соединением", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                this.Reconnect();
            }
        }

        #endregion

        #region Connection

        private void Login()
        {
            while (true)
            {
                try
                {
                    string userLogin = null;
                    if (this.GetUserLogin(ref userLogin) == false)
                    {
                        Environment.Exit(0);
                    }

                    //Соединение

                    this.tcpClient = new TcpClient(this.serverTcpIP, this.serverTcpPort);

                    //Логин

                    string response = null;
                    lock (this.tcpClient)
                    {
                        this.SendRequestToServer(String.Format("LGN" + this.separator + "{0}", userLogin));
                        response = this.GetResponseFromServer();
                    }

                    string[] respArr = response.Split(this.separator);
                    switch (respArr[0])
                    {
                        case "OK":
                            this.isCrash = false;
                            this.userLogin = userLogin;
                            this.Title = "Chat: Приветствуем - " + userLogin;

                            this.usersThread = new Thread(UpdateUsers);
                            this.usersThread.IsBackground = true;
                            this.usersThread.Start();

                            this.messagesThread = new Thread(UpdateMessages);
                            this.messagesThread.IsBackground = true;
                            this.messagesThread.Start();

                            this.timer.Start();

                            return;
                        case "ERR":
                            MessageBox.Show(respArr[1], "Авторизация на сервере", MessageBoxButton.OK, MessageBoxImage.Information);
                            break;
                        default:
                            break;
                    }
                }
                catch (SocketException sexc)
                {
                    MessageBox.Show(sexc.Message, "Соединение с сервером", MessageBoxButton.OK, MessageBoxImage.Warning);
                    if (this.tcpClient != null) { this.tcpClient.Close(); }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "Всё сломалось", MessageBoxButton.OK, MessageBoxImage.Warning);
                    if (this.tcpClient != null) { this.tcpClient.Close(); }
                }
            }
        }

        private void Logout()
        {
            try
            {
                this.messages.Clear();
                this.users.Clear();
                this.lbUsers.Items.Refresh();
                this.lbMessages.Items.Refresh();
                this.tbxMessage.Text = "";
                this.Title = "Chat";

                if (this.usersThread != null) { this.usersThread.Abort(); }
                if (this.messagesThread != null) { this.messagesThread.Abort(); }

                if (this.timer != null && this.timer.IsEnabled) { this.timer.Stop(); }

                string request = String.Format("LGT" + this.separator + "{0}", this.userLogin);
                string responce = "";
                lock (this.tcpClient)
                {
                    this.SendRequestToServer(request);
                    responce = this.GetResponseFromServer();
                }
            }
            catch { }
            finally
            {
                if (this.tcpClient != null) { this.tcpClient.Close(); }
            }
        }

        private void Reconnect()
        {
            this.Logout();
            this.Login();
        }

        #endregion

        #region Helpers

        private bool GetUserLogin(ref string login)
        {
            Login loginWnd = new Login();
            loginWnd.Owner = this;
            bool? dialogResult = loginWnd.ShowDialog();
            if (dialogResult.HasValue && dialogResult.Value)
            {
                login = loginWnd.userLogin;
                return true;
            }
            return false;
        }

        private void SendRequestToServer(string request)
        {
            NetworkStream netStream = this.tcpClient.GetStream();
            BinaryWriter BW = new BinaryWriter(netStream);
            int size = Encoding.UTF8.GetByteCount(request);
            BW.Write(size);
            byte[] buffer = Encoding.UTF8.GetBytes(request);
            netStream.Write(buffer, 0, size);
        }

        private string GetResponseFromServer()
        {
            int size = this.GetResponseLength();
            MemoryStream MS = new MemoryStream(size);
            byte[] buffer = new byte[16384];
            int bytes = 0;
            NetworkStream netStream = this.tcpClient.GetStream();
            do
            {
                int cnt = netStream.Read(buffer, 0, buffer.Length);
                if (cnt == 0) { throw new ApplicationException("Ошибка передачи данных. Получено 0 байт"); }
                bytes += cnt;
                MS.Write(buffer, 0, cnt);
            } while (size > bytes);

            return Encoding.UTF8.GetString(MS.GetBuffer(), 0, bytes);
        }

        private int GetResponseLength()
        {
            NetworkStream netStream = this.tcpClient.GetStream();
            byte[] buffer = new byte[4];
            int cnt = netStream.Read(buffer, 0, 4);
            if (cnt != 4) { throw new ApplicationException("Ошибка передачи данных. Неверный размер заголовка запроса"); }
            return BitConverter.ToInt32(buffer, 0);
        }

        private void SendMessage()
        {
            string message = this.tbxMessage.Text.Trim();
            if (message.Length == 0) { return; }

            message = message.Replace(this.separator, '☻');
            int mSize = Encoding.UTF8.GetByteCount(message);

            if (mSize > 1024)
            {
                MessageBox.Show("Слишком большое сообщение", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string request = "SMS" + this.separator + message;
            string responce = "";
            
            lock (this.tcpClient)
            {
                this.SendRequestToServer(request);
                responce = this.GetResponseFromServer();
            }

            string[] respArr = responce.Split(this.separator);
            switch (respArr[0])
            {
                case "OK":
                    this.tbxMessage.Text = "";
                    break;
                case "ERR":
                    MessageBox.Show(respArr[1], "Отправка сообщения", MessageBoxButton.OK, MessageBoxImage.Warning);
                    break;
                default:
                    break;
            }
        }

        #endregion
    }
}