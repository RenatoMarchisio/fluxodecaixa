using FluxoDeCaixa.Application.Dto;
using FluxoDeCaixa.Application.UseCases.Commons.Bases;
using FluxoDeCaixa.Application.UseCases.FluxoDeCaixaRelatorio.Queries.GetRelatorioQuery;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace FluxoDeCaixaRelatorio.WebApi.Endpoints;

public static class FluxoDeCaixaRelatorioEndpoints
{
    public static IEndpointRouteBuilder MapFluxoDeCaixaRelatorioEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/FluxoDeCaixaRelatorio")
                       .WithTags("FluxoDeCaixaRelatorio")
                       .WithOpenApi();

        group.MapGet("/Relatorio", async (
            IMediator mediator,
            IDistributedCache cache,
            [FromQuery] DateTime inicio,
            [FromQuery] DateTime fim,
            CancellationToken ct) =>
        {
            var cacheKey = $"relatorio:{inicio:yyyy-MM-dd}:{fim:yyyy-MM-dd}";

            // Cache-on-First-Hit: verifica Redis antes de bater no SQL
            var cachedJson = await cache.GetStringAsync(cacheKey, ct);
            if (cachedJson is not null)
            {
                var cachedData = JsonSerializer.Deserialize<BaseResponse<IEnumerable<FluxoDeCaixaRelatorioDto>>>(cachedJson);
                return cachedData is not null
                    ? Results.Ok(cachedData)
                    : Results.Ok(new BaseResponse<IEnumerable<FluxoDeCaixaRelatorioDto>>
                    {
                        succcess = false,
                        Message = "Erro ao desserializar cache."
                    });
            }

            // Cache MISS → consulta SQL via CQRS
            var response = await mediator.Send(
                new GetRelatorioByDateFCInicioFimQuery { Inicio = inicio, Fim = fim }, ct);

            if (response.succcess)
            {
                // Calcula TTL inteligente:
                //  - fim no passado → TTL longo (365 dias) — dado imutável
                //  - fim = hoje ou futuro → TTL até meia-noite de hoje — pode mudar
                var agora = DateTime.UtcNow;
                var fimUtc = fim.Date.ToUniversalTime();

                DistributedCacheEntryOptions cacheOptions;
                if (fimUtc < agora.Date)
                {
                    cacheOptions = new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(365)
                    };
                }
                else
                {
                    var meianoite = agora.Date.AddDays(1).ToUniversalTime();
                    cacheOptions = new DistributedCacheEntryOptions
                    {
                        AbsoluteExpiration = meianoite
                    };
                }

                var json = JsonSerializer.Serialize(response);
                await cache.SetStringAsync(cacheKey, json, cacheOptions, ct);
            }

            return response.succcess ? Results.Ok(response) : Results.BadRequest(response);
        })
        .WithName("GetRelatorioConsolidado");

        return app;
    }
}
