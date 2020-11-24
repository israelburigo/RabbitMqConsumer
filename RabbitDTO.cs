using System.Collections.Generic;
using System.Security.Claims;

namespace Useall.MicroCore.RabbitMQ.Base.Rabbit
{
    public abstract class RabbitDTO
    {
        public abstract string QueueName();
        public Dictionary<string, string> Claims { get; set; }

        public virtual void Auth()
        {
            Claims = new Dictionary<string, string>();
        }
    }
}
