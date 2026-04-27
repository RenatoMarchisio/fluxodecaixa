using AutoMapper;
using FluxoDeCaixa.Application.Interface.Persistence;
using FluxoDeCaixa.Application.UseCases.Commons.Bases;
using FluxoDeCaixa.Domain.Entities;
using MediatR;

namespace FluxoDeCaixa.Application.UseCases.FluxoDeCaixa.Commands.CreateFluxoDeCaixaCommand
{
    public class CreateFluxoDeCaixaCreditoHandler : IRequestHandler<CreateFluxoDeCaixaCreditoCommand, BaseResponse<bool>>
    {
        private readonly IUnitOfWorkFluxoDeCaixa _unitOfWork;
        private readonly IMapper _mapper;

        public CreateFluxoDeCaixaCreditoHandler(IUnitOfWorkFluxoDeCaixa unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public async Task<BaseResponse<bool>> Handle(CreateFluxoDeCaixaCreditoCommand command, CancellationToken cancellationToken)
        {
            var response = new BaseResponse<bool>();
            try
            {
                var fluxoDeCaixaCredito = _mapper.Map<FluxoDeCaixaCredito>(command);
                response.Data = await _unitOfWork.FluxoDeCaixaCredito.InsertAsync(fluxoDeCaixaCredito);
                if (response.Data)
                {
                    response.succcess = true;
                    response.Message = "Criado com sucesso!";
                }
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
            }
            return response;
        }
    }

    public class CreateFluxoDeCaixaDebitoHandler : IRequestHandler<CreateFluxoDeCaixaDebitoCommand, BaseResponse<bool>>
    {
        private readonly IUnitOfWorkFluxoDeCaixa _unitOfWork;
        private readonly IMapper _mapper;

        public CreateFluxoDeCaixaDebitoHandler(IUnitOfWorkFluxoDeCaixa unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public async Task<BaseResponse<bool>> Handle(CreateFluxoDeCaixaDebitoCommand command, CancellationToken cancellationToken)
        {
            var response = new BaseResponse<bool>();
            try
            {
                var fluxoDeCaixaDebito = _mapper.Map<FluxoDeCaixaDebito>(command);
                response.Data = await _unitOfWork.FluxoDeCaixaDebito.InsertAsync(fluxoDeCaixaDebito);
                if (response.Data)
                {
                    response.succcess = true;
                    response.Message = "Criado com sucesso!";
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

