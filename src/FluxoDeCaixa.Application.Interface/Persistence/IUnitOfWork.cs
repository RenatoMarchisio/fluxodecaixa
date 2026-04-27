namespace FluxoDeCaixa.Application.Interface.Persistence
{
    public interface IUnitOfWorkFluxoDeCaixa : IDisposable
    {
        IFluxoDeCaixaCreditoRepository FluxoDeCaixaCredito { get; }
        IFluxoDeCaixaDebitoRepository FluxoDeCaixaDebito { get; }
        IFluxoDeCaixaRelatorioRepository FluxoDeCaixaRelatorio { get; }
        IFluxoDeCaixaConsolidadoRepository FluxoDeCaixaConsolidado { get; }
    }
}
