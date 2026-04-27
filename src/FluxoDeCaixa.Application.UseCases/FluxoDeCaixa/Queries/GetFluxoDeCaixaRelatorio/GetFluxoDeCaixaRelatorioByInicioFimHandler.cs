using AutoMapper;
using FluxoDeCaixa.Application.Dto;
using FluxoDeCaixa.Application.Interface.Persistence;
using FluxoDeCaixa.Application.UseCases.Commons.Bases;
using MediatR;

namespace FluxoDeCaixa.Application.UseCases.FluxoDeCaixaRelatorio.Queries.GetRelatorioQuery
{
    public class GetRelatorioByDateFCInicioFimQueryHandler : IRequestHandler<GetRelatorioByDateFCInicioFimQuery, BaseResponse<IEnumerable<FluxoDeCaixaRelatorioDto>>>
    {
    
        private readonly IUnitOfWorkFluxoDeCaixa _unitOfWork;
        private readonly IMapper _mapper;

        public GetRelatorioByDateFCInicioFimQueryHandler(IUnitOfWorkFluxoDeCaixa unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public async Task<BaseResponse<IEnumerable<FluxoDeCaixaRelatorioDto>>> Handle(GetRelatorioByDateFCInicioFimQuery request, CancellationToken cancellationToken)
        {
            var response = new BaseResponse<IEnumerable<FluxoDeCaixaRelatorioDto>>();
            try
            {
                var relatorio = await _unitOfWork.FluxoDeCaixaRelatorio.GetFluxoDeCaixaRelatorioAsync(request.Inicio, request.Fim);
                if(relatorio is not null)
                {
                    //response.Data = _mapper.Map<FluxoDeCaixaRelatorioDto>(relatorio);
                    response.Data = relatorio;
                    response.succcess = true;
                    response.Message = "Query executada!";
                }
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
            }
            return response;
        }
    }
}
