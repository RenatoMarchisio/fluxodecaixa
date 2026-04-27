using FluxoDeCaixa.Application.Interface.Persistence;
using FluxoDeCaixa.Persistence.Contexts;
using FluxoDeCaixa.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace FluxoDeCaixa.Persistence
{
    public static class ConfigureServices
    {
        public static IServiceCollection AddInjectionPersistence(this IServiceCollection services)
        {
            services.AddSingleton<DapperContextFC>(); // Connection para o BD

            services.AddScoped<IFluxoDeCaixaCreditoRepository, FluxoDeCaixaCreditoRepository>();
            services.AddScoped<IFluxoDeCaixaDebitoRepository, FluxoDeCaixaDebitoRepository>();
            services.AddScoped<IFluxoDeCaixaRelatorioRepository, FluxoDeCaixaRelatorioRepository>();
            services.AddScoped<IUnitOfWorkFluxoDeCaixa, UnitOfWorkFluxoDeCaixa>();
            services.AddScoped<IFluxoDeCaixaConsolidadoRepository, FluxoDeCaixaConsolidadoRepository>();

            return services;
        }
    }
}
