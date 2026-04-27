using FluxoDeCaixa.Application.Interface.Persistence;
using FluxoDeCaixa.Domain.Entities;
using FluxoDeCaixa.Infrastructure.Messaging;
using Microsoft.Extensions.Options;
using Medo;

namespace FluxoDeCaixa.DLQ.Messaging
{
    /// <summary>
    /// Consumer Pattern Dead Letter Queue (fluxodecaixa.queue.dlq).
    /// Tenta persistir o lançamento que falhou na fila principal.
    /// NÃO faz requeue em caso de erro — evita loop infinito.
    /// </summary>
    public sealed class FluxoDeCaixaDlqConsumer : RabbitMqConsumerBase
    {
        protected override string GetQueueName() => "fluxodecaixa.queue.dlq";

        // false = sem requeue — mensagem da DLQ não volta para nenhuma fila
        protected override bool RequeueOnError => false;
        public FluxoDeCaixaDlqConsumer(IOptions<RabbitMqSettings> options,ILogger<FluxoDeCaixaDlqConsumer> logger,
                                       IServiceScopeFactory scopeFactory): base(options, logger, scopeFactory) 
        { 
        }

        protected override async Task ProcessarAsync(TransacaoMessage mensagem,IServiceScope scope,CancellationToken ct)
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWorkFluxoDeCaixa>();

            if (mensagem.TipoOperacao == "CREDITO")
            {
                var entidade = new FluxoDeCaixaCredito
                {
                    ID        = Uuid7.NewGuid(),
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
                    ID        = Uuid7.NewGuid(),
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
                    $"[DLQ] TipoOperacao desconhecido: '{mensagem.TipoOperacao}'.");
            }
        }
    }
}
