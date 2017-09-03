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

namespace ContextProcessor
{
    class Program
    {
        /// <summary>
        /// Connecting to a Broker
        /// </summary>
        static readonly ConnectionFactory Factory = new ConnectionFactory { HostName = "66.128.60.46", UserName = "dev", Password = "dev", VirtualHost = "/" };
        static readonly IConnection Connection = Factory.CreateConnection();

        static string exchangeName = "deviceTopic";
        static string contextProcessorQueue = "contextProcessor";
        static string discoveryQueue = "discovery";
        static string authenticationQueue = "authentication";
        static string dataManagerQueue = "dataManager";

        static List<Model.Device> deviceList = new List<Model.Device>();

        static void Main(string[] args)
        {
            Start();
        }

        private static CancellationTokenSource _tokenSource;

        /// <summary>
        /// Title: RabbitRx
        /// Author: Ben Johnson
        /// Date: Jan 27, 2015
        /// Availability: https://github.com/bensmind/RabbitRx
        /// </summary>
        private static void Start()
        {
            _tokenSource = new CancellationTokenSource();

            Console.WriteLine("Context Processor Service: Press Enter to Start");
            Console.ReadLine();
            Task.Run(() => ConsumeThrottle());
            Console.WriteLine("Press Any Key to Stop");
            Console.ReadLine();
            _tokenSource.Cancel();
            Start();
        }

        static void ConsumeThrottle()
        {
            var channel = Connection.CreateModel();

            channel.BasicQos(0, 50, false);
            channel.ExchangeDeclare(exchangeName, "topic");
            //Queue to send data to Discovery
            channel.QueueDeclare(discoveryQueue, false, false, false, null);
            channel.QueueBind(discoveryQueue, exchangeName, discoveryQueue);
            //Queue to send data to Authentication
            channel.QueueDeclare(authenticationQueue, false, false, false, null);
            channel.QueueBind(authenticationQueue, exchangeName, authenticationQueue);
            //Queue to send data to Data Manager
            channel.QueueDeclare(dataManagerQueue, false, false, false, null);
            channel.QueueBind(dataManagerQueue, exchangeName, dataManagerQueue);

            var settings = new BasicProperties()
            {
                ContentType = "application/json",
                DeliveryMode = 1 
            };

            var consumer = new JsonObservableSubscription<object>(channel, contextProcessorQueue, true);

            var throttlingConsumer = new ThrottlingConsumer<RabbitMessage<object>>(consumer, 4);

            throttlingConsumer.Subscribe(message =>
            {
                var discoveryPayload = JsonConvert.DeserializeObject<Model.Device>(message.Payload.ToString());
                var authenticationPayload = JsonConvert.DeserializeObject<Model.Authentication>(message.Payload.ToString());

                if (discoveryPayload.Id != Guid.Empty && !string.IsNullOrEmpty(discoveryPayload.Name) && discoveryPayload.Type != 0)
                {
                    var device = deviceList.Where(d => d.Id == discoveryPayload.Id).SingleOrDefault();
                    if (device == null)
                    {
                        var Salt = Model.Common.GenerateSalt();
                        discoveryPayload.Salt = Salt;
                        deviceList.Add(discoveryPayload);
                    }

                    var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(discoveryPayload));
                    channel.BasicPublish(exchangeName, discoveryQueue, settings, bytes);
                    Console.WriteLine("Received:\n");
                    Console.WriteLine("Device: {0}\n", discoveryPayload.Id);
                    Console.WriteLine("Thread: {0}\n\n", Thread.CurrentThread.GetHashCode());
                }
                else if (authenticationPayload.Id != Guid.Empty && !string.IsNullOrEmpty(authenticationPayload.BaseString) && !string.IsNullOrEmpty(authenticationPayload.Password))
                {
                    var device = deviceList.Where(d => d.Id == authenticationPayload.Id).SingleOrDefault();
                    authenticationPayload.Salt = device.Salt;
                    if(device.Token == null)
                    {
                        device.Token = Model.Common.GenerateToken();
                    }

                    authenticationPayload.Token = device.Token;
           
                    var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(authenticationPayload));
                    channel.BasicPublish(exchangeName, authenticationQueue, settings, bytes);
                    Console.WriteLine("Received:\n");
                    Console.WriteLine("Device: {0}\n", discoveryPayload.Id);
                    Console.WriteLine("Base String: {0}\n", authenticationPayload.BaseString);
                    Console.WriteLine("Share Secret: {0}\n", authenticationPayload.Password);
                    Console.WriteLine("Thread: {0}\n\n", Thread.CurrentThread.GetHashCode());
                }
                else
                {
                    Console.WriteLine("Received (Thread {1}): {0}\n", "INVALID PAYLOAD", Thread.CurrentThread.GetHashCode());
                }

            }, _tokenSource.Token);

            var start = throttlingConsumer.Start(_tokenSource.Token, TimeSpan.FromSeconds(1));

            start.ContinueWith(t =>
            {
                consumer.Close();
                channel.Dispose();
            });
        }
    }
}
