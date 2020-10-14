using System.Security.Claims;

namespace Useall.MicroCore.RabbitMQ.Base.Rabbit
{
    public abstract class RabbitDTO
    {
        public abstract string QueueName();
        public virtual Claim[] Claims()
        {
            return null;
        }
    }
}
