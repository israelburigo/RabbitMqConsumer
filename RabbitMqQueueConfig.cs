namespace Useall.MicroCore.RabbitMQ.Base.Rabbit
{
    public class RabbitMqQueueConfig
    {
        public int PrefetchSize { get; set; }
        public int PrefetchCount { get; set; }
        public bool Global { get; set; }        
        public int RequeueCount { get; set; }

        public RabbitMqQueueConfig()
        {
            PrefetchSize = 0;
            PrefetchCount = 1;
            Global = true;
            RequeueCount = 1;
        }
    }
}
