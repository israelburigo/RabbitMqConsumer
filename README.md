# RabbitMqConsumer

            RabbitMq.Instance
                .AddQueue<QueueConsumer, ObjectDto, DeadQueueConsumer>()
                .Connect(new RabbitMqConfig
                {
                    Host = "host",
                    User = "user",
                    Password = "pass",
                    VirtualHost = "vHost"
                })
                .Wait();
