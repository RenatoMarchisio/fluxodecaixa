using FluxoDeCaixa.Application.Interface.Persistence;


namespace FluxoDeCaixa.Persistence.Repositories
{

    internal class UnitOfWorkFluxoDeCaixa : IUnitOfWorkFluxoDeCaixa
    {

        public IFluxoDeCaixaCreditoRepository FluxoDeCaixaCredito { get; }
        public IFluxoDeCaixaDebitoRepository FluxoDeCaixaDebito { get; }
        public IFluxoDeCaixaRelatorioRepository FluxoDeCaixaRelatorio { get; }
        public IFluxoDeCaixaConsolidadoRepository FluxoDeCaixaConsolidado { get; }

        public UnitOfWorkFluxoDeCaixa(IFluxoDeCaixaCreditoRepository fluxoDeCaixaCredito, 
                                      IFluxoDeCaixaDebitoRepository fluxoDeCaixaDebito,
                                      IFluxoDeCaixaRelatorioRepository fluxoDeCaixaRelatorio,
                                      IFluxoDeCaixaConsolidadoRepository fluxoDeCaixaConsolidado)
        {
            FluxoDeCaixaCredito = fluxoDeCaixaCredito ?? throw new ArgumentNullException(nameof(fluxoDeCaixaCredito));
            FluxoDeCaixaDebito = fluxoDeCaixaDebito ?? throw new ArgumentNullException(nameof(fluxoDeCaixaDebito));
            FluxoDeCaixaRelatorio = fluxoDeCaixaRelatorio ?? throw new ArgumentNullException(nameof(fluxoDeCaixaRelatorio));
            FluxoDeCaixaConsolidado = fluxoDeCaixaConsolidado ?? throw new ArgumentNullException(nameof(fluxoDeCaixaConsolidado));
        }

        public void Dispose()
        {
            System.GC.SuppressFinalize(this);
        }
    }
}
