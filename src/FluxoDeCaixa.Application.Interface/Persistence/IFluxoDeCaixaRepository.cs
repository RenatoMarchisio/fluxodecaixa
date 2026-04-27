
using FluxoDeCaixa.Domain.Entities;

namespace FluxoDeCaixa.Application.Interface.Persistence
{
    public interface IFluxoDeCaixaCreditoRepository : IGenericRepository<FluxoDeCaixaCredito>
    {
        new Task<bool> InsertAsync(FluxoDeCaixaCredito fluxoDeCaixaCredito);
        new Task<bool> UpdateAsync(FluxoDeCaixaCredito fluxoDeCaixaCredito);
    }
    public interface IFluxoDeCaixaDebitoRepository : IGenericRepository<FluxoDeCaixaDebito>
    {
        new Task<bool> InsertAsync(FluxoDeCaixaDebito fluxoDeCaixaDebito);
        new Task<bool> UpdateAsync(FluxoDeCaixaDebito fluxoDeCaixaDebito);
    }

}