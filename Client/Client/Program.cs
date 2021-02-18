using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Client
{
    public class Programm
    {
        const int WaitTime = 2300;

        static string[] randomRequests = new[] { "Запрашиваю число!", "Где мое число!?", "Запрос на число от 8 до 8.5!" };
        static int Port;
        static int RequestNumber = 1;
        static TcpClient Client;
		static IPAddress ServerIp;
		static NetworkStream Stream;
        static Random random = new Random();
		static void Main(string[] args)
		{
			Console.WriteLine("Введите ip сервера");
			ServerIp = IPAddress.Parse(Console.ReadLine());
			Console.WriteLine("Введите порт сервера");
			Port = int.Parse(Console.ReadLine());
			Console.Clear();
			try
			{
				ClientObject Client = new ClientObject();
				Client.Connect(ServerIp, Port);
				Client.Start();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}

		}
		public class ClientObject
        {
            public void Connect(IPAddress serverip, int port)
            {
                Client = new TcpClient(Convert.ToString(serverip), port);
                Stream = Client.GetStream();
            }
            public void Start()
            {

                Thread SendThread = new Thread(new ThreadStart(SendMessage));
                SendThread.Start();
                Thread GetThread = new Thread(new ThreadStart(GetMessageFromServer));
                GetThread.Start();
            }
            public void SendMessage()
            {
                while (true)
                {
                    var message = randomRequests[random.Next(2)];
                    byte[] data = Encoding.Unicode.GetBytes(message);
                    Stream.Write(data);
                    Console.WriteLine("Клиент: " + message);
                    Thread.Sleep(WaitTime);
                }
            }
            public void GetMessageFromServer()
            {
                StringBuilder builder = new StringBuilder();
                byte[] data = new byte[256];
                do
                {
                    builder.Clear();
                    var bytes = Stream.Read(data, 0, data.Length);
                    builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                    Console.WriteLine("Сервер: " + builder.ToString());
                }
                while (true);
            }
        }
    }
}
