/*
 * Fluxo de Caixa
 */
using Dapper;
using FluxoDeCaixa.Application.UseCases;
using FluxoDeCaixa.Infrastructure.Dapper;
using FluxoDeCaixa.Infrastructure.Messaging;
using FluxoDeCaixa.Persistence;
using FluxoDeCaixa.WebApi.Endpoints;
using FluxoDeCaixa.WebApi.Extensions.Middleware;
using FluxoDeCaixa.WebApi.Messaging;

// Registra o manipulador globalmente no Dapper
// Utlizado para mappear DateOnly para SqlServer DATE Type
SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Persistence + Application
builder.Services.AddInjectionPersistence();
builder.Services.AddInjectionApplication();
builder.Services.AddAuthorization();

// RabbitMQ — settings, publisher (Singleton) e consumer da fila principal (HostedService)
builder.Services.Configure<RabbitMqSettings>(
    builder.Configuration.GetSection("RabbitMQ"));
builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();
builder.Services.AddHostedService<FluxoDeCaixaMainConsumer>();

// Utlizado para evitar erros de bloqueios no gateway api
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Fluxo de Caixa V.1");
        c.RoutePrefix = string.Empty;
    });
//}

// Utlizado para evitar erros de bloqueios no gateway api
app.UseCors();

//app.UseHttpsRedirection();
app.UseAuthorization();
app.AddMiddleware(); // Validation
app.MapFluxoDeCaixaEndpoints();
app.Run();
