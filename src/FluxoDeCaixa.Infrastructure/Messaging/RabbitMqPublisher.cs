using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace FluxoDeCaixa.Infrastructure.Messaging
{
    public interface IRabbitMqPublisher
    {
        Task PublicarAsync(TransacaoMessage mensagem, CancellationToken ct = default);
    }

    public sealed class RabbitMqPublisher : IRabbitMqPublisher, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly RabbitMqSettings _settings;

        public RabbitMqPublisher(IOptions<RabbitMqSettings> options)
        {
            _settings = options.Value;

            var factory = new ConnectionFactory
            {
                Uri = new Uri(_settings.ConnectionString),
                DispatchConsumersAsync = true
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Declara a DLQ primeiro (deve existir antes da fila principal referenciá-la)
            _channel.QueueDeclare(
                queue: _settings.DeadLetterQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            // Declara a fila principal com Dead Letter vinculado
            var args = new Dictionary<string, object>
            {
                { "x-dead-letter-exchange",    "" },                               // default exchange
                { "x-dead-letter-routing-key", _settings.DeadLetterQueueName },   // rota para DLQ
                { "x-message-ttl",             86_400_000 }                        // 24 h em ms
            };

            _channel.QueueDeclare(
                queue: _settings.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: args);
        }

        public Task PublicarAsync(TransacaoMessage mensagem, CancellationToken ct = default)
        {
            var json = JsonSerializer.Serialize(mensagem);
            var body = Encoding.UTF8.GetBytes(json);

            var props = _channel.CreateBasicProperties();
            props.Persistent   = true;                  // sobrevive ao restart do broker
            props.ContentType  = "application/json";
            props.CorrelationId = mensagem.CorrelationId.ToString();

            _channel.BasicPublish(
                exchange:   "",
                routingKey: _settings.QueueName,
                basicProperties: props,
                body: body);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
        }
    }
}
