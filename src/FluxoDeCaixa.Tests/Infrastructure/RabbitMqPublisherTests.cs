using FluxoDeCaixa.Tests.Infrastructure.Fixtures;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace FluxoDeCaixa.Tests.Infrastructure
{
    [Collection(RabbitMqCollection.Name)]
    public sealed class RabbitMqPublisherTests : IDisposable
    {
        private const string Queue = "test.fluxo.queue";
        private const string DlqQueue = "test.fluxo.queue.dlq";

        private readonly RabbitMqFixture _fixture;
        private readonly RabbitMqPublisher _sut;
        private readonly IConnection _verificacaoConn;
        private readonly IModel _verificacaoChannel;

        public RabbitMqPublisherTests(RabbitMqFixture fixture)
        {
            _fixture = fixture;
            _sut = new RabbitMqPublisher(_fixture.CriarOptions(Queue, DlqQueue));

            _verificacaoConn = _fixture.CriarConexao();
            _verificacaoChannel = _verificacaoConn.CreateModel();
        }

        public void Dispose()
        {
            _verificacaoChannel?.Close();
            _verificacaoConn?.Close();
            _sut?.Dispose();
        }

        // ── Publicar mensagem ────────────────────────────────────────────

        [Fact]
        public async Task PublicarAsync_MensagemCredito_ChegaNaFila()
        {
            var mensagem = CriarMensagem("CREDITO", credito: 1500m);

            await _sut.PublicarAsync(mensagem);

            // Aguarda entrega ao broker (sincrono — não há consumer, então BasicGet)
            await Task.Delay(200);
            var result = _verificacaoChannel.BasicGet(Queue, autoAck: true);

            result.Should().NotBeNull("a mensagem deve ter chegado na fila");

            var json = Encoding.UTF8.GetString(result!.Body.Span);
            var desserializado = JsonSerializer.Deserialize<TransacaoMessage>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            desserializado.Should().NotBeNull();
            desserializado!.TipoOperacao.Should().Be("CREDITO");
            desserializado.Credito.Should().Be(1500m);
        }

        [Fact]
        public async Task PublicarAsync_MensagemDebito_ChegaNaFila()
        {
            var mensagem = CriarMensagem("DEBITO", debito: 300m);

            await _sut.PublicarAsync(mensagem);

            await Task.Delay(200);
            var result = _verificacaoChannel.BasicGet(Queue, autoAck: true);

            result.Should().NotBeNull();

            var json = Encoding.UTF8.GetString(result!.Body.Span);
            var desserializado = JsonSerializer.Deserialize<TransacaoMessage>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            desserializado!.TipoOperacao.Should().Be("DEBITO");
            desserializado.Debito.Should().Be(300m);
        }

        [Fact]
        public async Task PublicarAsync_CorrelationId_ChegaNasPropriedades()
        {
            var correlationId = Guid.NewGuid();
            var mensagem = CriarMensagem("CREDITO", credito: 100m, correlationId: correlationId);

            await _sut.PublicarAsync(mensagem);

            await Task.Delay(200);
            var result = _verificacaoChannel.BasicGet(Queue, autoAck: true);

            result.Should().NotBeNull();
            result!.BasicProperties.CorrelationId.Should().Be(correlationId.ToString());
        }

        [Fact]
        public async Task PublicarAsync_PropriedadesPersistent_EContentType()
        {
            var mensagem = CriarMensagem("CREDITO", credito: 50m);

            await _sut.PublicarAsync(mensagem);

            await Task.Delay(200);
            var result = _verificacaoChannel.BasicGet(Queue, autoAck: true);

            result.Should().NotBeNull();
            result!.BasicProperties.Persistent.Should().BeTrue();
            result.BasicProperties.ContentType.Should().Be("application/json");
        }

        [Fact]
        public async Task PublicarAsync_VariosMensagens_TodasChegamNaFila()
        {
            var quantidade = 5;
            for (var i = 0; i < quantidade; i++)
                await _sut.PublicarAsync(CriarMensagem("CREDITO", credito: i * 10m + 1m));

            await Task.Delay(400);

            var recebidas = 0;
            while (_verificacaoChannel.BasicGet(Queue, autoAck: true) is not null)
                recebidas++;

            recebidas.Should().Be(quantidade);
        }

        // ── Helper ───────────────────────────────────────────────────────

        private static TransacaoMessage CriarMensagem(
            string tipo,
            decimal? credito      = null,
            decimal? debito       = null,
            Guid? correlationId   = null) => new(
            DataFc        : new DateOnly(2026, 4, 15),
            Descricao     : "Teste integração",
            Credito       : credito,
            Debito        : debito,
            TipoOperacao  : tipo,
            CorrelationId : correlationId ?? Guid.NewGuid(),
            CriadoEm      : DateTime.UtcNow
        );
    }
}
