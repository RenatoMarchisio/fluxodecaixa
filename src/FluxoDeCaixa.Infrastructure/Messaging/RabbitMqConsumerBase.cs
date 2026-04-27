using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace FluxoDeCaixa.Infrastructure.Messaging
{
    /// <summary>
    /// BackgroundService base para consumers RabbitMQ.
    /// Herdar e implementar <see cref="ProcessarAsync"/> com a lógica de negócio.
    /// <para>
    ///   - <see cref="GetQueueName"/>: retorna o nome da fila a consumir.
    ///   - <see cref="RequeueOnError"/>: true = requeue + DLQ após MaxRetries (fila principal);
    ///     false = ack / log apenas (fila DLQ — evita loop infinito).
    /// </para>
    /// </summary>
    public abstract class RabbitMqConsumerBase : BackgroundService
    {
        private readonly RabbitMqSettings _settings;
        private readonly ILogger _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private IConnection? _connection;
        private IModel? _channel;

        protected RabbitMqConsumerBase(
            IOptions<RabbitMqSettings> options,
            ILogger logger,
            IServiceScopeFactory scopeFactory)
        {
            _settings    = options.Value;
            _logger      = logger;
            _scopeFactory = scopeFactory;
        }

        /// <summary>Nome da fila que este consumer escuta.</summary>
        protected abstract string GetQueueName();

        /// <summary>
        /// Se true: em caso de erro aplica Nack+requeue até MaxRetries, depois Reject (→ DLQ).
        /// Se false: em caso de erro apenas loga e faz Ack (sem loop).
        /// </summary>
        protected virtual bool RequeueOnError => true;

        /// <summary>Lógica de negócio — implementar no consumer concreto.</summary>
        protected abstract Task ProcessarAsync(
            TransacaoMessage mensagem,
            IServiceScope scope,
            CancellationToken ct);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory
            {
                Uri = new Uri(_settings.ConnectionString),
                DispatchConsumersAsync = true
            };

            _connection = factory.CreateConnection();
            _channel    = _connection.CreateModel();

            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += OnMensagemRecebidaAsync;

            _channel.BasicConsume(
                queue:    GetQueueName(),
                autoAck:  false,
                consumer: consumer);

            _logger.LogInformation(
                "[{Consumer}] Aguardando mensagens na fila: {Queue}",
                GetType().Name, GetQueueName());

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private async Task OnMensagemRecebidaAsync(object sender, BasicDeliverEventArgs ea)
        {
            var retryCount = ObterContadorRetry(ea.BasicProperties);

            try
            {
                var json     = Encoding.UTF8.GetString(ea.Body.ToArray());
                var mensagem = JsonSerializer.Deserialize<TransacaoMessage>(json);

                if (mensagem is null)
                    throw new InvalidOperationException("Mensagem inválida (null).");

                _logger.LogInformation(
                    "[{Consumer}] Processando {Tipo} | DataFc: {Data} | CorrelationId: {Id}",
                    GetType().Name, mensagem.TipoOperacao, mensagem.DataFc, mensagem.CorrelationId);

                using var scope = _scopeFactory.CreateScope();
                await ProcessarAsync(mensagem, scope, CancellationToken.None);

                _channel!.BasicAck(ea.DeliveryTag, multiple: false);

                _logger.LogInformation(
                    "[{Consumer}] Mensagem processada com sucesso. CorrelationId: {Id}",
                    GetType().Name, mensagem.CorrelationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[{Consumer}] Erro ao processar mensagem. Tentativa {Retry}/{Max}",
                    GetType().Name, retryCount + 1, _settings.MaxRetries);

                if (!RequeueOnError)
                {
                    // Fila DLQ: não recoloca (evita loop). Loga e confirma para remover da fila.
                    _logger.LogCritical(
                        "[{Consumer}] Falha na DLQ — mensagem descartada após erro. CorrelationId inspecionar logs.",
                        GetType().Name);
                    _channel!.BasicAck(ea.DeliveryTag, multiple: false);
                }
                else if (retryCount >= _settings.MaxRetries)
                {
                    _logger.LogWarning(
                        "[{Consumer}] MaxRetries atingido. Rejeitando → DLQ: {DLQ}",
                        GetType().Name, _settings.DeadLetterQueueName);
                    _channel!.BasicReject(ea.DeliveryTag, requeue: false);
                }
                else
                {
                    _channel!.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
                }
            }
        }

        private static int ObterContadorRetry(IBasicProperties props)
        {
            if (props.Headers is null) return 0;

            if (props.Headers.TryGetValue("x-death", out var xDeath) &&
                xDeath is List<object> deaths && deaths.Count > 0 &&
                deaths[0] is Dictionary<string, object> firstDeath &&
                firstDeath.TryGetValue("count", out var count))
            {
                return Convert.ToInt32(count);
            }

            return 0;
        }

        public override void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
            base.Dispose();
        }
    }
}
