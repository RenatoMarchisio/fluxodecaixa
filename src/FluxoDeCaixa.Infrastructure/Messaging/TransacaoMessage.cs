namespace FluxoDeCaixa.Infrastructure.Messaging
{
    public record TransacaoMessage(
        DateOnly DataFc,
        string Descricao,
        decimal? Credito,
        decimal? Debito,
        string TipoOperacao,   // "CREDITO" ou "DEBITO"
        Guid CorrelationId,
        DateTime CriadoEm
    );
}
