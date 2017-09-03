using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing;
using RabbitMQ.Client.MessagePatterns;
using RabbitRx.Advanced.Subscription;
using RabbitRx.Core.Message;
using RabbitRx.Core.Subscription;
using RabbitRx.Json.Subscription;
using System.IO;

namespace DataManager
{
    class Program
    {
        /// <summary>
        /// Connecting to a Broker
        /// </summary>
        static readonly ConnectionFactory Factory = new ConnectionFactory { HostName = "66.128.60.46", UserName = "dev", Password = "dev", VirtualHost = "/" };
        static readonly IConnection Connection = Factory.CreateConnection();

        static string exchangeName = "deviceTopic";
        static string dataManagerQueue = "dataManager";

        static void Main(string[] args)
        {
            Start();
        }

        private static CancellationTokenSource _tokenSource;
        private static StreamWriter csv;
        private static string csvName;

        /// <summary>
        /// Title: RabbitRx
        /// Author: Ben Johnson
        /// Date: Jan 27, 2015
        /// Availability: https://github.com/bensmind/RabbitRx
        /// </summary>
        private static void Start()
        {
            _tokenSource = new CancellationTokenSource();
            Console.WriteLine("Enter the full path:");
            csvName = Console.ReadLine();
            Console.WriteLine("Data Manager Service: Press Enter to Start");
            Console.ReadLine();
            if (!string.IsNullOrEmpty(csvName))
                csv = new StreamWriter(csvName);
            Task.Run(() => ConsumeThrottle());
            Console.WriteLine("Press Any Key to Stop");
            Console.ReadLine();
            _tokenSource.Cancel();
            Start();
        }
        static readonly Random Rand = new Random();
        static void ConsumeThrottle()
        {
            var channel = Connection.CreateModel();

            channel.BasicQos(0, 50, false);

            var settings = new BasicProperties()
            {
                ContentType = "application/json",
                DeliveryMode = 1
            };

            var consumer = new JsonObservableSubscription<object>(channel, dataManagerQueue, true);

            var throttlingConsumer = new ThrottlingConsumer<RabbitMessage<object>>(consumer, 10);

            throttlingConsumer.Subscribe(message =>
            {
                var session = JsonConvert.DeserializeObject<Model.Data>(message.Payload.ToString());
                //TODO: Token valiadtion
                //if (IsValidToken())
                //{
                    Console.WriteLine("Received (Thread {1}): {0}", message.Payload, Thread.CurrentThread.GetHashCode());
                    if (!string.IsNullOrEmpty(csvName))
                    {
                        var newLine = string.Format("{0},{1},{2}", session.Id, session.IdTransaction, DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"));
                        csv.WriteLine(newLine);
                        csv.Flush();
                    }

                    //TODO: Send data to datastorage/API interface
                //}
                consumer.Ack(message);

            }, _tokenSource.Token);

            var start = throttlingConsumer.Start(_tokenSource.Token, TimeSpan.FromSeconds(5));

            start.ContinueWith(t =>
            {
                consumer.Close();
                channel.Dispose();
            });
        }
    }
}
