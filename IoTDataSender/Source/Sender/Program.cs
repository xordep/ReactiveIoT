using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing;
using RabbitRx.Advanced.Subscription;
using RabbitRx.Core.Message;
using RabbitRx.Core.Subscription;
using RabbitRx.Json.Subscription;
using System.IO;

namespace Sender
{
    class Program
    {
        /// <summary>
        /// Connecting to a Broker
        /// </summary>
        static readonly ConnectionFactory factory = new ConnectionFactory { HostName = "66.128.60.46", UserName = "dev", Password = "dev", VirtualHost = "/" };
        static readonly IConnection connection = factory.CreateConnection();
        static readonly IModel channel = connection.CreateModel();
        static string exchangeName = "deviceTopic";
        static string syntacticAnalyzerQueue = "syntacticAnalyzer";
        static string discoveryResponseQueue = "discoveryResponse";
        static string authenticationResponseQueue = "authenticationResponse";

        static List<Model.DataTime> listTime = new List<Model.DataTime>();

        static void Main(string[] args)
        {
            channel.ExchangeDeclare(exchangeName, "topic");
            //Queue to send data
            channel.QueueDeclare(syntacticAnalyzerQueue, false, false, false, null);
            channel.QueueBind(syntacticAnalyzerQueue, exchangeName, syntacticAnalyzerQueue);

            Start();
        }

        private static CancellationTokenSource tokenSource;
        private static CancellationTokenSource tokenSourceDiscovery;
        private static CancellationTokenSource tokenSourceAuthentication;
        private static CancellationTokenSource tokenSourceProducer;
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
            tokenSource = new CancellationTokenSource();
            tokenSourceDiscovery = new CancellationTokenSource();
            tokenSourceAuthentication = new CancellationTokenSource();
            tokenSourceProducer = new CancellationTokenSource();

            Console.WriteLine("Enter the number of devices:");
            var devicesCount = Console.ReadLine();
            Console.WriteLine("\nEnter Lambda:");
            var lambda = Console.ReadLine();
            Console.WriteLine("\nEnter the full path:");
            csvName = Console.ReadLine();
            Console.WriteLine("\nSender: Press Enter to Start");
            Console.ReadLine();
            if(!string.IsNullOrEmpty(csvName))
                csv = new StreamWriter(csvName);
            Task.Run(() => Produce(int.Parse(devicesCount), double.Parse(lambda)));
            Task.Run(() => ConsumeThrottleDiscovery());
            Task.Run(() => ConsumeThrottleAuthentication(double.Parse(lambda)));
            Console.WriteLine("Press Any Key to Stop");
            Console.ReadLine();
            tokenSource.Cancel();
            tokenSourceDiscovery.Cancel();
            tokenSourceAuthentication.Cancel();
            tokenSourceProducer.Cancel();
            Start();
        }

        private static void Produce(int devicesCount, double lambda)
        {
            var deviceCounter = 0;
            var rand = new Random();
            var settings = new BasicProperties()
            {
                ContentType = "application/json",
                DeliveryMode = 1 //1)not durable, 2)durable
            };

            var ob = Observable.Generate(Guid.NewGuid(), i => !tokenSource.IsCancellationRequested, i => Guid.NewGuid(), i => i, x => TimeSpan.FromMilliseconds(5)/*TimeSpan.FromMilliseconds(nextTime(1 / lambda))*/);

            ob.Subscribe(id =>
            {
                deviceCounter++;

                //Send Data
                var device = new Model.Device()
                {
                    Id = id,
                    Name = "Device #" + deviceCounter,
                    Type = Enum.DeviceType.Temperature
                };
                var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(device));
                channel.BasicPublish(exchangeName, syntacticAnalyzerQueue, settings, bytes);
                Console.WriteLine("Published Discovery: {0}", device.Name);

                //Stop to device registration
                if (deviceCounter == devicesCount)
                {
                    tokenSource.Cancel();
                }

            }, tokenSource.Token);

        }

        static void ConsumeThrottleDiscovery()
        {
            var channelConsumer = connection.CreateModel();

            channelConsumer.BasicQos(0, 50, false);
            channelConsumer.ExchangeDeclare(exchangeName, "topic");
            //Declare the Discovery Response Queue
            channelConsumer.QueueDeclare(discoveryResponseQueue, false, false, false, null);
            channelConsumer.QueueBind(discoveryResponseQueue, exchangeName, discoveryResponseQueue);
            //Queue to send data to Syntactic AnalyzerQueue
            channelConsumer.QueueDeclare(syntacticAnalyzerQueue, false, false, false, null);
            channelConsumer.QueueBind(syntacticAnalyzerQueue, exchangeName, syntacticAnalyzerQueue);

            var settings = new BasicProperties()
            {
                ContentType = "application/json",
                DeliveryMode = 1 //1)not durable, 2)durable
            };

            //Consumer Discovery
            var consumer = new JsonObservableSubscription<object>(channelConsumer, discoveryResponseQueue, true);

            var throttlingConsumer = new ThrottlingConsumer<RabbitMessage<object>>(consumer, 4);

            throttlingConsumer.Subscribe(message =>
            {
                var discoveryResponse = JsonConvert.DeserializeObject<Model.DiscoveryResponse>(message.Payload.ToString());
                var ramdomString = Model.Common.RandomString(10);
                var password = Model.Common.Encrypt(ramdomString + discoveryResponse.Salt);

                var authentication = new Model.Authentication()
                {
                    Id = discoveryResponse.Id,
                    BaseString = ramdomString,
                    Password = password
                };
                var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(authentication));
                channelConsumer.BasicPublish(exchangeName, syntacticAnalyzerQueue, settings, bytes);
                Console.WriteLine("Received (Thread {1}): {0}", message.Payload, Thread.CurrentThread.GetHashCode());

            }, tokenSourceDiscovery.Token);

            var start = throttlingConsumer.Start(tokenSourceDiscovery.Token, TimeSpan.FromSeconds(10));

            start.ContinueWith(t =>
            {
                consumer.Close();
                channelConsumer.Dispose();
            });
        }

        static void ConsumeThrottleAuthentication(double lambda)
        {
            var channelConsumer = connection.CreateModel();

            channelConsumer.BasicQos(0, 50, false);
            channelConsumer.ExchangeDeclare(exchangeName, "topic");
            //Declare the Authentication Response Queue
            channelConsumer.QueueDeclare(authenticationResponseQueue, false, false, false, null);
            channelConsumer.QueueBind(authenticationResponseQueue, exchangeName, authenticationResponseQueue);
            //Queue to send data to Syntactic AnalyzerQueue
            channelConsumer.QueueDeclare(syntacticAnalyzerQueue, false, false, false, null);
            channelConsumer.QueueBind(syntacticAnalyzerQueue, exchangeName, syntacticAnalyzerQueue);

            var settings = new BasicProperties()
            {
                ContentType = "application/json",
                DeliveryMode = 1
            };

            //Consumer Authentication
            var consumer = new JsonObservableSubscription<object>(channelConsumer, authenticationResponseQueue, true);

            var throttlingConsumer = new ThrottlingConsumer<RabbitMessage<object>>(consumer, 4);

            throttlingConsumer.Subscribe(message =>
            {
                var authenticationResponse = JsonConvert.DeserializeObject<Model.AuthenticationResponse>(message.Payload.ToString());

                Task.Run(() => ProduceData(authenticationResponse, lambda));
                Console.WriteLine("Received (Thread {1}): {0}", message.Payload, Thread.CurrentThread.GetHashCode());

            }, tokenSourceAuthentication.Token);

            var start = throttlingConsumer.Start(tokenSourceAuthentication.Token, TimeSpan.FromSeconds(10));

            start.ContinueWith(t =>
            {
                consumer.Close();
                channelConsumer.Dispose();
            });
        }

        static void ProduceData(Model.AuthenticationResponse auth, double lambda)
        {
            var packageCounter = 0;
            var rand = new Random();
            var settings = new BasicProperties()
            {
                ContentType = "application/json",
                DeliveryMode = 1
            };

            var ob = Observable.Generate(rand.Next(), i => !tokenSourceProducer.IsCancellationRequested, i => rand.Next(), i => i, x => TimeSpan.FromMilliseconds(nextTime(1 / lambda)));

            ob.Subscribe(id =>
            {
                var date = DateTime.Now;
                if (!activeBackPressure(auth.Id, date))
                {
                    packageCounter++;

                    //Send Data
                    var data = new Model.Data()
                    {
                        Id = auth.Id,
                        IdTransaction = packageCounter,
                        Token = auth.Token,
                        Value = rand.Next()
                    };
                    var jsonData = JsonConvert.SerializeObject(data);
                    var bytes = Encoding.UTF8.GetBytes(jsonData);
                    channel.BasicPublish(exchangeName,"dataManager", settings, bytes);
                    Console.WriteLine("Published Data: {0}", jsonData);
                    if (!string.IsNullOrEmpty(csvName))
                    {
                        var newLine = string.Format("{0},{1},{2}", auth.Id, packageCounter, date.ToString("MM/dd/yyyy hh:mm:ss.fff tt"));
                        csv.WriteLine(newLine);
                        csv.Flush();
                    }
                }

            }, tokenSourceProducer.Token);
        }

        private static bool activeBackPressure(Guid idDevice, DateTime date)
        {
            if (listTime.Count == 0)
            {
                listTime.Add(new Model.DataTime() { date = date, idDevice = idDevice });
                return false;
            }
            else
            {
                foreach (var dataTime in listTime)
                {
                    if(dataTime.idDevice == idDevice)
                    {
                        if(dataTime.date.Hour == date.Hour && dataTime.date.Minute == date.Minute && dataTime.date.Second == date.Second)
                        {
                            return true;
                        }
                        else
                        {
                            dataTime.date = date;
                            return false;
                        }
                    }
                }
            }
            listTime.Add(new Model.DataTime() { date = date, idDevice = idDevice });
            return false;
        }

        private static double nextTime(double rateParameter)
        {
            Random random = new Random();
            return -Math.Log(1.0 - random.NextDouble()) / rateParameter;
        }
    }
}
