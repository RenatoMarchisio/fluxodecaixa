using AutoMapper;
using FluxoDeCaixa.Application.Dto;
using FluxoDeCaixa.Application.UseCases.FluxoDeCaixa.Commands.CreateFluxoDeCaixaCommand;
using FluxoDeCaixa.Domain.Entities;

namespace FluxoDeCaixa.Application.UseCases.Commons.Mappings
{
    public class FluxoDeCaixaMapper : Profile
    {
        public FluxoDeCaixaMapper()
        {
            CreateMap<FluxoDeCaixaCredito, FluxoDeCaixaCreditoDTO>().ReverseMap();
            CreateMap<FluxoDeCaixaDebito, FluxoDeCaixaDebitoDTO>().ReverseMap();

            CreateMap<FluxoDeCaixaCredito, CreateFluxoDeCaixaCreditoCommand>().ReverseMap();
            CreateMap<FluxoDeCaixaDebito, CreateFluxoDeCaixaDebitoCommand>().ReverseMap();
        }
    }
}



