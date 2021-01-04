using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Generic;

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
            RabbitConsumer = new T()
            {
                Queue = this
            };
            JsonResolver = json => JsonConvert.DeserializeObject<TDto>(json);
            return this;
        }

        public override void CreateChannel(IConnection conn)
        {
            base.CreateChannel(conn);
            CreateConsumer();
        }

        private string GetFullError(Exception e)
        {            
            if (e == null)
                return null;

            if (e is AggregateException && e.InnerException != null)
                e = e.InnerException;

            var msgExp = e.Message;

            if (e.InnerException != null)
                msgExp = $"{msgExp}\n{GetFullError(e.InnerException)}";

            return msgExp;
        }

        private void CreateConsumer()
        {
            var consumer = new EventingBasicConsumer(Channel);

            consumer.Received += (sender, e) =>
            {
                try
                {
                    var json = Encoding.UTF8.GetString(e.Body.ToArray());
                    var dto = JsonResolver?.Invoke(json);
                    RabbitConsumer.Execute(dto, e);
                    Channel.BasicAck(e.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    try
                    {
                        RabbitConsumer.Except(ex, e);

                        //if (RabbitConsumer.ExceptionAck)
                        //    Channel.BasicAck(e.DeliveryTag, false);
                        //else
                        //    Channel.BasicNack(e.DeliveryTag, false, RabbitConsumer.Requeue);

                        //Gambiarra para mostrar o erro no Rabbit, o correto não é fazer isso o correto é logar o erro pela função Except(ex, e) chamada acima
                        // feito para integração do loja
                        Channel.BasicAck(e.DeliveryTag, false);
                        if (e.BasicProperties.Headers == null)
                            e.BasicProperties.Headers = new Dictionary<string, object>();
                        e.BasicProperties.Headers.Add("x-error", GetFullError(ex));
                        Channel.BasicPublish("", e.RoutingKey + RabbitMqConsts.DL, e.BasicProperties, e.Body);                        
                    }
                    catch
                    {
                        Channel.BasicNack(e.DeliveryTag, false, false);
                    }
                }
            };

            Channel.BasicConsume(Name, false, consumer);
        }
    }

   

    public class RabbitMqQueue
    {
        public IModel Channel { get; private set; }
        public string Name { get; private set; }
        public RabbitMqQueueConfig Config { get; private set; }
        public bool IsDeadLetter { get; protected set; }

        public RabbitMqQueue(string name, RabbitMqQueueConfig config)
        {
            Name = name;
            Config = config;
        }

        public List<TDto> GetMessages<TDto>()
        {
            var ret = new List<TDto>();
            var c = Channel.MessageCount(Name);
            while (c-- > 0)
            {
                var data = Channel.BasicGet(Name, false);
                var json = Encoding.UTF8.GetString(data.Body.ToArray());
                ret.Add(JsonConvert.DeserializeObject<TDto>(json));
            }
            //Channel.Abort();
            return ret;
        }

        public virtual void CreateChannel(IConnection conn)
        {
            Channel = conn.CreateModel();
            var args = new Dictionary<string, object>();

            if (!IsDeadLetter)
            {
                var dl = Name + RabbitMqConsts.DL;
                var dlx = Name + RabbitMqConsts.DLX;
                Channel.ExchangeDeclare(dlx, ExchangeType.Fanout);
                Channel.QueueDeclare(dl, true, false, false, null);
                Channel.QueueBind(dl, dlx, "");
                args.Add("x-dead-letter-exchange", dlx);
            }

            Channel.QueueDeclare(Name, true, false, false, args);

            Channel.BasicQos((uint)Config.PrefetchSize, (ushort)Config.PrefetchCount, Config.Global);
        }
    }
}
