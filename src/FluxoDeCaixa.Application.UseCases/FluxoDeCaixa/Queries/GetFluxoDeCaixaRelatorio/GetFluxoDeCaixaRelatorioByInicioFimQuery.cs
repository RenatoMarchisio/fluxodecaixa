using FluxoDeCaixa.Application.Dto;
using FluxoDeCaixa.Application.UseCases.Commons.Bases;
using MediatR;

namespace FluxoDeCaixa.Application.UseCases.FluxoDeCaixaRelatorio.Queries.GetRelatorioQuery
{
    public class GetRelatorioByDateFCInicioFimQuery : IRequest<BaseResponse<IEnumerable<FluxoDeCaixaRelatorioDto>>>
    {
        public DateTime Inicio { get; set; }
        public DateTime Fim { get; set; }
    }
}
