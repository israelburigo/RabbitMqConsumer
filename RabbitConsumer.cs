using System;
using System.Linq;
using System.Collections;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Generic;
using System.Text;

namespace Useall.MicroCore.RabbitMQ.Base.Rabbit
{
    public abstract class RabbitConsumer<T>
        where T : RabbitDTO
    {
        public RabbitMqQueue<T> Queue { get; set; }
        public bool Requeue { get; set; }
        public bool ExceptionAck { get; set; }
        public abstract void Execute(T obj, BasicDeliverEventArgs e);
        public abstract void Except(Exception ex, BasicDeliverEventArgs e);

        public void RePublish(BasicDeliverEventArgs e)
        {
            Queue.Channel.BasicPublish("", e.RoutingKey, e.BasicProperties, e.Body);
        }
              

        public int GetDeathCount(BasicDeliverEventArgs e)
        {
            if (e.BasicProperties.Headers == null)
                return 0;

            if (!e.BasicProperties.Headers.ContainsKey("x-death"))
                return 0;

            var xDeath = (e.BasicProperties.Headers["x-death"] as IEnumerable).Cast<Dictionary<string, object>>().First();

            return int.Parse(xDeath["count"].ToString());
        }
        
    }
}
