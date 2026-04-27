using FluentValidation;

namespace FluxoDeCaixa.Application.UseCases.FluxoDeCaixa.Commands.CreateFluxoDeCaixaCommand
{
    public class CreateFluxoDeCaixaBaseValidator : AbstractValidator<CreateFluxoDeCaixaBaseCommand>
    {
        public CreateFluxoDeCaixaBaseValidator()
        {
            RuleFor(x => x.dataFC)
                .NotEmpty().WithMessage("Data do Fluxo de Caixa é obrigatória")
                .InclusiveBetween(new DateOnly(2020, 1, 1), new DateOnly(2030, 12, 31))
                .WithMessage("A data deve estar entre 2020 e 2030");

            RuleFor(x => x.descricao)
                .NotEmpty().WithMessage("Descrição do Fluxo de Caixa é obrigatória")
                .Length(1, 255).WithMessage("Descrição deve ter entre 1 e 255 caracteres");
        }
    }

    public class CreateFluxoDeCaixaCreditoValidator : AbstractValidator<CreateFluxoDeCaixaCreditoCommand>
    {
        public CreateFluxoDeCaixaCreditoValidator()
        {
            Include(new CreateFluxoDeCaixaBaseValidator());

            RuleFor(x => x.credito)
                .InclusiveBetween(1, 9999999999m)
                .WithMessage("Campo Crédito aceita valores entre R$ 1,00 e R$ 9.999.999.999,99");
        }
    }
    public class CreateFluxoDeCaixaDebitoValidator : AbstractValidator<CreateFluxoDeCaixaDebitoCommand>
    {
        public CreateFluxoDeCaixaDebitoValidator()
        {
            Include(new CreateFluxoDeCaixaBaseValidator());

            RuleFor(x => x.debito)
                .InclusiveBetween(1, 9999999999m)
                .WithMessage("Campo Crédito aceita valores entre R$ 1,00 e R$ 9.999.999.999,99");
        }
    }
}
