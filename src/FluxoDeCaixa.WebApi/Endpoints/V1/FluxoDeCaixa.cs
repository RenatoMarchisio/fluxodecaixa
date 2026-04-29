using FluxoDeCaixa.Application.UseCases.FluxoDeCaixa.Commands.CreateFluxoDeCaixaCommand;
using FluxoDeCaixa.Application.UseCases.Commons.Bases;
using FluxoDeCaixa.Infrastructure.Messaging;
using FluentValidation;

namespace FluxoDeCaixa.WebApi.Endpoints
{
    public static class FluxoDeCaixaEndpoints
    {
        public static IEndpointRouteBuilder MapFluxoDeCaixaEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/FluxoDeCaixa").WithTags("FluxoDeCaixa").WithOpenApi();

            group.MapPost("/InsertCredito", async (
                IRabbitMqPublisher publisher,
                IValidator<CreateFluxoDeCaixaCreditoCommand> validator,
                CreateFluxoDeCaixaCreditoCommand command,
                CancellationToken ct) =>
            {
                if (command is null) return Results.BadRequest();

                var validacao = await validator.ValidateAsync(command, ct);
                if (!validacao.IsValid)
                {
                    var erros = validacao.Errors.Select(e => new BaseError { PropertyMessage = e.PropertyName, ErrorMessage = e.ErrorMessage });
                    return Results.BadRequest(new BaseResponse<bool> { Message = "Dados inválidos", Errors = erros });
                }

                var mensagem = new TransacaoMessage(
                    DataFc: command.dataFC,
                    Descricao: command.descricao,
                    Credito: command.credito,
                    Debito: null,
                    TipoOperacao: "CREDITO",
                    CorrelationId: Guid.NewGuid(),
                    CriadoEm: DateTime.UtcNow);

                await publisher.PublicarAsync(mensagem, ct);

                return Results.Ok(new BaseResponse<bool>
                {
                    Data = true,
                    succcess = true,
                    Message = "Credito realizado com sucesso."
                });

            }).WithName("InsertCredito");

            group.MapPost("/InsertDebito", async (
                IRabbitMqPublisher publisher,
                IValidator<CreateFluxoDeCaixaDebitoCommand> validator,
                CreateFluxoDeCaixaDebitoCommand command,
                CancellationToken ct) =>
            {
                if (command is null) return Results.BadRequest();

                var validacao = await validator.ValidateAsync(command, ct);
                if (!validacao.IsValid)
                {
                    var erros = validacao.Errors.Select(e => new BaseError { PropertyMessage = e.PropertyName, ErrorMessage = e.ErrorMessage });
                    return Results.BadRequest(new BaseResponse<bool> { Message = "Dados inválidos", Errors = erros });
                }

                var mensagem = new TransacaoMessage(
                    DataFc: command.dataFC,
                    Descricao: command.descricao,
                    Credito: null,
                    Debito: command.debito,
                    TipoOperacao: "DEBITO",
                    CorrelationId: Guid.NewGuid(),
                    CriadoEm: DateTime.UtcNow);

                await publisher.PublicarAsync(mensagem, ct);

                return Results.Ok(new BaseResponse<bool>
                {
                    Data = true,
                    succcess = true,
                    Message = "Débito realizado com sucesso."
                });

            }).WithName("InsertDebito");

            return app;
        }
    }
}
