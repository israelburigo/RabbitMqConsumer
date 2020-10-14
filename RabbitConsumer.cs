using System;
using RabbitMQ.Client.Events;

namespace Useall.MicroCore.RabbitMQ.Base.Rabbit
{
    public abstract class RabbitConsumer<T>
        where T : RabbitDTO
    {
        public bool Requeue { get; set; }

        public abstract void Execute(T obj, BasicDeliverEventArgs e);
        public abstract void Except(Exception ex, BasicDeliverEventArgs e);
        
    }
}
