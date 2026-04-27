using FluxoDeCaixa.Application.Interface.Persistence;
using FluxoDeCaixa.Domain.Entities;
using FluxoDeCaixa.Infrastructure.Messaging;
using Microsoft.Extensions.Options;

namespace FluxoDeCaixa.WebApi.Messaging
{
    /// <summary>
    /// Consumer da fila principal (fluxodecaixa.queue).
    /// Persiste o lançamento no SQL Server e atualiza o consolidado via UPSERT.
    /// Em caso de falha após MaxRetries, a mensagem é rejeitada e vai automaticamente para a DLQ.
    /// </summary>
    public sealed class FluxoDeCaixaMainConsumer : RabbitMqConsumerBase
    {
        public FluxoDeCaixaMainConsumer(IOptions<RabbitMqSettings> options,ILogger<FluxoDeCaixaMainConsumer> logger,
                                        IServiceScopeFactory scopeFactory) : base(options, logger, scopeFactory) 
        { 
        }

        protected override string GetQueueName() => "fluxodecaixa.queue";

        // true = aplica retry + rejeita para DLQ após MaxRetries
        protected override bool RequeueOnError => true;

        protected override async Task ProcessarAsync(
            TransacaoMessage mensagem,
            IServiceScope scope,
            CancellationToken ct)
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWorkFluxoDeCaixa>();

            if (mensagem.TipoOperacao == "CREDITO")
            {
                var entidade = new FluxoDeCaixaCredito
                {
                    ID        = Guid.NewGuid(),
                    dataFC    = mensagem.DataFc,
                    descricao = mensagem.Descricao,
                    credito   = mensagem.Credito ?? 0m
                };

                await uow.FluxoDeCaixaCredito.InsertAsync(entidade);
                await uow.FluxoDeCaixaConsolidado.UpsertAsync(mensagem.DataFc, mensagem.Credito ?? 0m, 0m);
            }
            else if (mensagem.TipoOperacao == "DEBITO")
            {
                var entidade = new FluxoDeCaixaDebito
                {
                    ID        = Guid.NewGuid(),
                    dataFC    = mensagem.DataFc,
                    descricao = mensagem.Descricao,
                    debito    = mensagem.Debito ?? 0m
                };

                await uow.FluxoDeCaixaDebito.InsertAsync(entidade);
                await uow.FluxoDeCaixaConsolidado.UpsertAsync(mensagem.DataFc, 0m, mensagem.Debito ?? 0m);
            }
            else
            {
                throw new InvalidOperationException(
                    $"TipoOperacao desconhecido: '{mensagem.TipoOperacao}'. Esperado: CREDITO ou DEBITO.");
            }
        }
    }
}
