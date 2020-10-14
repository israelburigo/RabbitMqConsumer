using System;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Useall.MicroCore.RabbitMQ.Base.Rabbit
{
    public class RabbitMqQueue<TDto> : RabbitMqQueue
        where TDto : RabbitDTO
    {
        public Func<string, TDto> JsonResolver { get; private set; }
        public RabbitConsumer<TDto> RabbitConsumer { get; private set; }

        public RabbitMqQueue(string name, RabbitMqQueueConfig config)
            : base(name, config)
        {
        }

        public RabbitMqQueue<TDto> AddConsumer<T>()
            where T : RabbitConsumer<TDto>, new()
        {
            RabbitConsumer = new T();
            JsonResolver = json => JsonConvert.DeserializeObject<TDto>(json);
            return this;
        }

        public override void CreateChannel(IConnection conn)
        {
            base.CreateChannel(conn);
            CreateConsumer();
        }

        private void CreateConsumer()
        {
            var consumer = new EventingBasicConsumer(Channel);

            consumer.Received += (sender, e) =>
            {
                try
                {   
                    var json = Encoding.UTF8.GetString(e.Body.ToArray());
                    var dto = JsonResolver.Invoke(json);

                    GenerateAuth(dto);

                    RabbitConsumer.Execute(dto, e);
                    Channel.BasicAck(e.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    RabbitConsumer.Except(ex, e);
                    Channel.BasicNack(e.DeliveryTag, false, RabbitConsumer.Requeue);
                }
            };

            Channel.BasicConsume(Name, false, consumer);
        }

        private void GenerateAuth(RabbitDTO dto)
        {
            if (dto.Claims() != null && dto.Claims().Any())
            {
                var identity = new ClaimsIdentity(dto.Claims(), "Useall");
                Thread.CurrentPrincipal = new ClaimsPrincipal(identity);
            }
        }
    }

    public class RabbitMqQueue
    {
        public IModel Channel { get; private set; }
        public string Name { get; private set; }
        public RabbitMqQueueConfig Config { get; private set; }

        public RabbitMqQueue(string name, RabbitMqQueueConfig config)
        {
            Name = name;
            Config = config;
        }

        public virtual void CreateChannel(IConnection conn)
        {
            Channel = conn.CreateModel();
            Channel.BasicQos((uint)Config.PrefetchSize, (ushort)Config.PrefetchCount, Config.Global);
            Channel.QueueDeclare(Name, true, false, false, null);
        }
    }
}
