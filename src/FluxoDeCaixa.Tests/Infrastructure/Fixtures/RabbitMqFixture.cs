using RabbitMQ.Client;
using Testcontainers.RabbitMq;

namespace FluxoDeCaixa.Tests.Infrastructure.Fixtures
{
    /// <summary>
    /// Sobe um container RabbitMQ para testes de integração do publisher.
    /// </summary>
    public sealed class RabbitMqFixture : IAsyncLifetime
    {
        private readonly RabbitMqContainer _container = new RabbitMqBuilder()
            .WithImage("rabbitmq:3.13-management")
            .Build();

        public string ConnectionString { get; private set; } = default!;

        public async Task InitializeAsync()
        {
            await _container.StartAsync();
            ConnectionString = _container.GetConnectionString();
        }

        public async Task DisposeAsync() => await _container.DisposeAsync();

        /// <summary>
        /// Cria uma conexão real ao broker para verificar mensagens nos testes.
        /// </summary>
        public IConnection CriarConexao()
        {
            var factory = new ConnectionFactory
            {
                Uri = new Uri(ConnectionString),
                DispatchConsumersAsync = true
            };
            return factory.CreateConnection();
        }

        /// <summary>
        /// Cria as opções tipadas que o <see cref="RabbitMqPublisher"/> espera receber.
        /// </summary>
        public IOptions<RabbitMqSettings> CriarOptions(string queueName = "test.queue", string dlqName = "test.queue.dlq")
        {
            var settings = new RabbitMqSettings
            {
                ConnectionString   = ConnectionString,
                QueueName          = queueName,
                DeadLetterQueueName = dlqName,
                ExchangeName       = "",
                MaxRetries         = 3
            };
            return Microsoft.Extensions.Options.Options.Create(settings);
        }
    }
}
