using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Useall.MicroCore.RabbitMQ.Base.Rabbit
{
    public class RabbitMqDeadQueue<TDto> : RabbitMqQueue<TDto>
        where TDto : RabbitDTO
    {
        public RabbitMqDeadQueue(string name, RabbitMqQueueConfig config) 
            : base(name + RabbitMqConsts.DL, config)
        {
            IsDeadLetter = true;
        }
    }

    public class RabbitMqDeadQueue : RabbitMqQueue
    {
        public RabbitMqDeadQueue(string name, RabbitMqQueueConfig config)
            : base(name + RabbitMqConsts.DL, config)
        {
            IsDeadLetter = true;
        }
    }
}
