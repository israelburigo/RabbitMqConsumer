using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Polly;

namespace Useall.MicroCore.RabbitMQ.Base.Rabbit
{
    public class RabbitMq
    {
        private readonly List<RabbitMqQueue> _queues;
        public IConnection Connection { get; private set; }
        public RabbitMqConfig Config { get; private set; }
        private readonly object _lockObject = new object();
        public bool Connected { get { return Connection != null && Connection.IsOpen; } }

        private static RabbitMq _instance;
        public static RabbitMq Instance { get { return _instance ?? (_instance = new RabbitMq()); } }

        private RabbitMq()
        {
            _queues = new List<RabbitMqQueue>();
        }

        public RabbitMq Wait()
        {
            while (!Connected) { }
            return this;
        }

        public RabbitMq Connect(RabbitMqConfig config)
        {
            new Thread(th =>
            {
                Connect(config.Host, config.User, config.Password, config.VirtualHost);
            }).Start();

            return this;
        }

        private void Connect(string host, string user, string pass, string vHost)
        {
            lock (_lockObject)
            {
                var factory = new ConnectionFactory
                {
                    HostName = host,
                    UserName = user,
                    Password = pass,
                    VirtualHost = vHost
                };

                Policy.Handle<SocketException>()
                    .Or<BrokerUnreachableException>()
                    .RetryForever()
                    .Execute(() =>
                    {
                        Connection = factory.CreateConnection();
                        _queues.ForEach(p => p.CreateChannel(Connection));
                    });
            }
        }

        public RabbitMq AddQueue<TDto>() 
            where TDto : RabbitDTO, new()
        {
            AddQueue<TDto>(new RabbitMqQueueConfig());
            return this;
        }

        public RabbitMq AddQueue<TDto>(RabbitMqQueueConfig config) 
            where TDto : RabbitDTO, new()
        {
            var name = new TDto().QueueName();

            if (_queues.Any(p => p.Name == name))
                return this;

            _queues.Add(new RabbitMqQueue(name, config));
            return this;
        }

        public RabbitMq AddQueue<TCons, TDto>()
            where TCons : RabbitConsumer<TDto>, new()
            where TDto : RabbitDTO, new()
        {
            AddQueue<TCons, TDto>(new RabbitMqQueueConfig());
            return this;
        }

        public RabbitMq AddQueue<TCons, TDto>(RabbitMqQueueConfig config)
            where TCons : RabbitConsumer<TDto>, new()
            where TDto : RabbitDTO, new()
        {
            var name = new TDto().QueueName();

            if (_queues.Any(p => p.Name == name))
                return this;

            _queues.Add(new RabbitMqQueue<TDto>(name, config).AddConsumer<TCons>());

            return this;
        }

        public void Push<T>(T obj) where T : RabbitDTO
        {
            var queue = _queues.FirstOrDefault(p => p.Name == obj.QueueName());
            if (queue == null)
                return;

            var json = JsonConvert.SerializeObject(obj);
            var body = Encoding.UTF8.GetBytes(json);

            queue.Channel.BasicPublish("", queue.Name, null, body);
        }
    }
}
