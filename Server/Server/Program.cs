using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    class Program
    {
        static int port;
        static IPAddress serverip;
        static Random RandomNumber = new Random();
        static ServerObject server;
        static Thread listenThread;
        public static List<ClientObject> clients = new List<ClientObject>();
        public static List<Tuple<string, string>> clientmessage = new List<Tuple<string, string>>();
        public static List<Tuple<string, string>> Dupletmessage = new List<Tuple<string, string>>();
        static void Main(string[] args)
        {
            Console.WriteLine("Введите адресс ip");
            serverip = IPAddress.Parse(Console.ReadLine());
            Console.WriteLine("Введите номер порта");
            port = int.Parse(Console.ReadLine());
            Console.Clear();
            try
            {
                server = new ServerObject();
                listenThread = new Thread(new ThreadStart(server.Listen));
                listenThread.Start();
            }
            catch (Exception ex)
            {
                server.Disconnect();
                Console.WriteLine(ex.Message);
            }

        }
        public class ClientObject
        {
            public string Id { get; private set; }

            public NetworkStream Stream { get; private set; }

			private readonly TcpClient client;
			private readonly ServerObject server;

            public ClientObject(TcpClient tcpClient, ServerObject serverObject)
            {
                Id = Guid.NewGuid().ToString();
                client = tcpClient;
                server = serverObject;
                serverObject.AddConnection(this);
            }
            protected internal int GetClientNumber(string id)
            {
                ClientObject client = clients.FirstOrDefault(c => c.Id == id);
                if (client != null)
                    return clients.IndexOf(client);
                return 0;
            }
            protected internal NetworkStream GetClientStream(string id)
            {
                ClientObject client = clients.FirstOrDefault(c => c.Id == id);
                if (client != null)
                    return client.Stream;
                return null;
            }

            public void Process()
            {
                try
                {
                    Stream = client.GetStream();

                    while (true)
                    {
                        try
                        {
                            if (GetClientNumber(Id) <= 2)
                            {
                                var message = GetMessage();
                                server.AddMessage(Id, message);
                                if (!server.DupletCheck(Id, message))
                                {
                                    if (server.IsServerBusy(Id, message))
                                    {
                                        var builder = "Сервер занят!";
                                        SendMessage(builder);
										server.RemoveMessage(Id, message);
									}
                                    else
                                    {
                                        var processingMessage = message;
                                        ProcessingMessageAsync(processingMessage);
                                    }
                                }
                                else
                                {
                                    var builder = "Задача уже обрабатывается!";
                                    SendMessage(builder);
                                    server.AddDuplet(Id, message);
                                }
                            }
                            else
                            {
                                string message = GetMessage();
                                string builder = "Превышен лимит потоков, попробуйте позже";
                                SendMessage(builder);
                            }
                        }
                        catch
                        {
                            string message = String.Format($"Клиент с номером потока {GetClientNumber(Id)} - отключился");
                            Console.WriteLine(message);
                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                finally
                {
                    server.RemoveConnection(this.Id);
                    Close();
                }
            }

            private async void ProcessingMessageAsync(string message)
			{
                await Task.Run(() =>
                {
                    Thread.Sleep(5000);
                    Console.WriteLine($"Номер потока {GetClientNumber(Id)}, " + DateTime.Now.ToShortTimeString() + ": " + message.ToString());
                    var builder = message.ToString() + ": " + Math.Round(RandomNumber.Next(80, 85) / 10.0, 1);
                    SendMessage(builder);
                    server.RemoveMessage(Id, message);
                    server.RemoveDuplets(message);
                });
            }

            private string GetMessage()
            {
                byte[] data = new byte[256];
                StringBuilder builder = new StringBuilder();
                int bytes = 0;
                do
                {
                    bytes = Stream.Read(data, 0, data.Length);
                    builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                }
                while (Stream.DataAvailable);

                return builder.ToString();
            }
            private void SendMessage(string builder)
            {
                byte[] data = new byte[256];
                data = Encoding.Unicode.GetBytes(builder);
                Stream.Write(data, 0, data.Length);
            }
            private void SendMessage(string builder, string message)
            {
                byte[] data = new byte[256];
                data = Encoding.Unicode.GetBytes(builder);
                Stream.Write(data, 0, data.Length);
                foreach ((string elementID, string elementMESS) in Dupletmessage)
                {
                    if (elementMESS == message)
                    {
                        GetClientStream(elementID).Write(data, 0, data.Length);
                    }
                }
            }

            protected internal void Close()
            {
                if (Stream != null)
                    Stream.Close();
                if (client != null)
                    client.Close();
            }
        }

        public class ServerObject
        {
            static TcpListener tcpListener;

            protected internal void AddConnection(ClientObject clientObject)
            {
                clients.Add(clientObject);
            }
            protected internal void RemoveConnection(string id)
            {
                ClientObject client = clients.FirstOrDefault(c => c.Id == id);
                if (client != null)
                    clients.Remove(client);
            }
            protected internal void AddMessage(string Id, string message)
            {
                clientmessage.Add(Tuple.Create(Id, message));
            }
            protected internal void RemoveMessage(string Id, string message)
            {
                clientmessage.Clear();
            }
            protected internal void AddDuplet(string Id, string message)
            {
                Dupletmessage.Add(Tuple.Create(Id, message));
            }
            protected internal void RemoveDuplets(string message)
            {
                Dupletmessage.Clear();
            }
            protected internal bool DupletCheck(string Id, string message)
            {
                int i = 0;
                foreach ((string elementID, string elementMESS) in clientmessage)
                {
                    if (elementID == Id && elementMESS == message)
                    {
                        i++;
                    }
                }
                if (i > 1)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            protected internal bool IsServerBusy(string Id, string message)
            {
                int i = 0;
                foreach ((string elementID, string elementMESS) in clientmessage)
                {
                    if (elementID == Id && elementMESS != message)
                    {
                        i++;
                    }
                }
                if (i >= 1)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            protected internal void Listen()
            {
                try
                {
                    tcpListener = new TcpListener(serverip, port);
                    tcpListener.Start();
                    Console.WriteLine("Сервер создан, ожидаются запросы");
                    while (true)
                    {
                        TcpClient tcpClient = tcpListener.AcceptTcpClient();
                        ClientObject clientObject = new ClientObject(tcpClient, this);
                        Thread clientThread = new Thread(new ThreadStart(clientObject.Process));
                        clientThread.Start();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Disconnect();
                }
            }

            protected internal void Disconnect()
            {
                tcpListener.Stop();

                for (int i = 0; i < clients.Count; i++)
                {
                    clients[i].Close();
                }
                Environment.Exit(0);
            }
        }
    }
}