/*
 * Relatorio consolidado de Fluxo de Caixa
 */
using Dapper;
using FluxoDeCaixa.Application.UseCases;
using FluxoDeCaixa.Infrastructure.Dapper;
using FluxoDeCaixa.Persistence;
using FluxoDeCaixa.WebApi.Extensions.Middleware;
using FluxoDeCaixaRelatorio.WebApi.Endpoints;

// Registra o manipulador globalmente no Dapper
// Utlizado para mappear DateOnly para SqlServer DATE Type
SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//Add methods Extensions
builder.Services.AddInjectionPersistence();
builder.Services.AddInjectionApplication();
builder.Services.AddAuthorization();

// Redis — Cache-on-First-Hit para o endpoint de Relatório
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:ConnectionString"];
    options.InstanceName = "fluxodecaixa:";
});

// Utlizado para evitar erros de bloqueios no gateway api
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Relatório de Fluxo de Caixa V.1");
        c.RoutePrefix = string.Empty;
    });
//}

// Utlizado para evitar erros de bloqueios no gateway api
app.UseCors();
//app.UseHttpsRedirection();
app.UseAuthorization();
app.AddMiddleware();
app.MapFluxoDeCaixaRelatorioEndpoints();
app.Run();
