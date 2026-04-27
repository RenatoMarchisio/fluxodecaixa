using FluentValidation;

namespace FluxoDeCaixa.Application.UseCases.FluxoDeCaixaRelatorio.Queries.GetRelatorioQuery
{
    public class GetFluxoDeCaixaRelatorioByInicioFimValidator: AbstractValidator<GetRelatorioByDateFCInicioFimQuery>
    {
        public GetFluxoDeCaixaRelatorioByInicioFimValidator()
        {
            RuleFor(x => x.Inicio).NotEmpty().WithMessage("parametro data de Inicio do relatório é obrigatório");
            RuleFor(x => x.Fim).
                NotEmpty().WithMessage("parametro data de fim do relatório é obrigatório").
                GreaterThan(x => x.Inicio).WithMessage("A data de fim deve ser maior que a data de início");
        }
    }
}
