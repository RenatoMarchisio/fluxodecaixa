using FluxoDeCaixa.Application.UseCases.Commons.Bases;
using MediatR;
using Medo;
using System.Text.Json.Serialization;

namespace FluxoDeCaixa.Application.UseCases.FluxoDeCaixa.Commands.CreateFluxoDeCaixaCommand
{

    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public abstract class CreateFluxoDeCaixaBaseCommand : IRequest<BaseResponse<bool>>
    {
        public Guid ID {
            get
            {
                return ID;
            }
            internal set 
            {
                // Especificação RFC 9562 - UUIDv7 é baseado em timestamp e é ordenável, o que pode ser benéfico para bancos de dados e sistemas de registro.
                var uuid = Uuid7.NewUuid7();
                ID = uuid.ToGuid();
            } 
        } 

        public DateOnly dataFC { get; set; }

        public string descricao { get; set; }
    }

    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public class CreateFluxoDeCaixaCreditoCommand : CreateFluxoDeCaixaBaseCommand
    {
        public decimal credito { get; set; }
    }

    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public class CreateFluxoDeCaixaDebitoCommand : CreateFluxoDeCaixaBaseCommand
    {
        public decimal debito { get; set; }
    }

}
