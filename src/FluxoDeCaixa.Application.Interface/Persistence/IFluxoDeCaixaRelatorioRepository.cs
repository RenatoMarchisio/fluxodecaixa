using FluxoDeCaixa.Application.Dto;
using FluxoDeCaixa.Domain.Entities;

namespace FluxoDeCaixa.Application.Interface.Persistence
{

    public interface IFluxoDeCaixaRelatorioRepository : IGenericRepository<FluxoDeCaixaRelatorio>
    {
        Task<IEnumerable<FluxoDeCaixaRelatorioDto>> GetFluxoDeCaixaRelatorioAsync(DateTime inicio, DateTime fim);
    }
    /// <summary>
    /// Repositório da tabela FluxoDeCaixaConsolidado (read model pré-agregado por dia).
    /// Operações: UPSERT incremental (credito/debito) por dataFC.
    /// </summary>
    public interface IFluxoDeCaixaConsolidadoRepository
    {
        /// <summary>
        /// Atualiza o consolidado do dia:
        /// - Se o dia já existe → soma credito / debito ao acumulado.
        /// - Se o dia não existe → cria a linha com os valores iniciais.
        /// </summary>
        Task UpsertAsync(DateOnly dataFc, decimal credito, decimal debito);
    }
}


