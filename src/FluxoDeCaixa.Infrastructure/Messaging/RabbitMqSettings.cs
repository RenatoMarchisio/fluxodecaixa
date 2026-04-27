namespace FluxoDeCaixa.Infrastructure.Messaging
{
    public class RabbitMqSettings
    {
        public string ConnectionString { get; set; } =
            "amqps://jxsdbhlb:WO9hwZCzEj8MoQm40gyxbzPVjQnnmGUu@moose.rmq.cloudamqp.com/jxsdbhlb";

        public string QueueName { get; set; } = "fluxodecaixa.queue";
        public string DeadLetterQueueName { get; set; } = "fluxodecaixa.queue.dlq";
        public string ExchangeName { get; set; } = "fluxodecaixa.exchange";
        public int MaxRetries { get; set; } = 3;
    }
}
