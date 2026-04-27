using FluxoDeCaixa.Application.UseCases.Commons.Bases;
using MediatR;
using Medo;

namespace FluxoDeCaixa.Application.UseCases.FluxoDeCaixa.Commands.UpdateFluxoDeCaixaCommand
{
    public class UpdateFluxoDeCaixaCommand: IRequest<BaseResponse<bool>>
    {
        // RFC 9562 
        public required Guid ID { get; set; } = Uuid7.NewGuid();
        public DateOnly dataFC { get; set; }
        public string descricao { get; set; }
        public DateTime criadoEm { get; set; }
        public decimal credito { get; set; }
        public decimal debito { get; set; }
    }
}
