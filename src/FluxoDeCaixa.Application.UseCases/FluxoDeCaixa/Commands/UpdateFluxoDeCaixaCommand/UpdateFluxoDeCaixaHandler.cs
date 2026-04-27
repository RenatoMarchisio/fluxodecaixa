using AutoMapper;
using FluxoDeCaixa.Application.Interface.Persistence;
using FluxoDeCaixa.Application.UseCases.Commons.Bases;
using MediatR;

namespace FluxoDeCaixa.Application.UseCases.FluxoDeCaixa.Commands.UpdateFluxoDeCaixaCommand
{
    public class UpdateFluxoDeCaixaHandler : IRequestHandler<UpdateFluxoDeCaixaCommand, BaseResponse<bool>>
    {
        private readonly IUnitOfWorkFluxoDeCaixa _unitOfWork;
        private readonly IMapper _mapper;

        public UpdateFluxoDeCaixaHandler(IUnitOfWorkFluxoDeCaixa unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public Task<BaseResponse<bool>> Handle(UpdateFluxoDeCaixaCommand request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
